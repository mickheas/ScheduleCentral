using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.OrTools.Sat;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;

namespace ScheduleCentral.Services
{
    public sealed class ScheduleSolverService
    {
        private readonly ApplicationDbContext _context;

        // Fixed timeline per USER assumptions (do not change)
        // Regular: Mon–Fri, 8 slots/day  ->  0..39
        // Extension Night: Mon–Fri, 2 slots/day -> 40..49
        // Extension Weekend: Sat–Sun, 8 slots/day -> 50..65
        private const int RegularDays = 5;
        private const int RegularSlotsPerDay = 8;

        private const int ExtNightStart = 40;
        private const int ExtNightDays = 5;
        private const int ExtNightSlotsPerDay = 2;

        private const int ExtWeekendStart = 50;
        private const int ExtWeekendDays = 2;
        private const int ExtWeekendSlotsPerDay = 8;

        private const int TimelineMin = 0;
        private const int TimelineMax = 65;

        public ScheduleSolverService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ScheduleRunResult> GenerateAndSaveForActiveTermAsync(string? generatedByUserId = null, CancellationToken cancellationToken = default)
        {
            var (academicYear, semester) = await GetActiveAcademicYearSemesterAsync(cancellationToken);

            var termAssignments = await _context.SemesterAssignments
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.Section)
                    .ThenInclude(s => s.Batch)
                        .ThenInclude(b => b.Department)
                .Where(a => a.AcademicYear == academicYear && a.Semester == semester)
                .ToListAsync(cancellationToken);

            var offeringIdsInTerm = new HashSet<int>();
            foreach (var a in termAssignments)
            {
                if (a.Section?.Batch == null) continue;
                if (!TryParseOfferingBatchInfo(a.Section.Batch.Name, out var parsed)) continue;
                offeringIdsInTerm.Add(parsed.OfferingId);
            }

            var approvedOfferingIds = new HashSet<int>();
            if (offeringIdsInTerm.Count > 0)
            {
                var approvedIds = await _context.CourseOfferings
                    .AsNoTracking()
                    .Where(o => offeringIdsInTerm.Contains(o.Id) && o.Status == OfferingStatus.Approved)
                    .Select(o => o.Id)
                    .ToListAsync(cancellationToken);
                approvedOfferingIds = approvedIds.ToHashSet();
            }

            var assignments = termAssignments
                .Where(a => a.Section?.Batch != null
                            && TryParseOfferingBatchInfo(a.Section.Batch.Name, out var parsed)
                            && approvedOfferingIds.Contains(parsed.OfferingId))
                .ToList();

            if (assignments.Count == 0)
            {
                var existingTerms = await _context.SemesterAssignments
                    .AsNoTracking()
                    .GroupBy(a => new { a.AcademicYear, a.Semester })
                    .Select(g => new { g.Key.AcademicYear, g.Key.Semester, Count = g.Count() })
                    .OrderByDescending(x => x.AcademicYear)
                    .ThenBy(x => x.Semester)
                    .Take(8)
                    .ToListAsync(cancellationToken);

                var termSummary = existingTerms.Count == 0
                    ? "(none)"
                    : string.Join("; ", existingTerms.Select(x => $"{x.AcademicYear} {x.Semester} ({x.Count})"));

                return new ScheduleRunResult
                {
                    AcademicYear = academicYear,
                    Semester = semester,
                    Success = false,
                    Error = $"No SemesterAssignments found for approved offerings in active term ({academicYear} {semester}). "
                            + $"Assignments in term: {termAssignments.Count}. Offerings referenced in term: {offeringIdsInTerm.Count}. Approved offerings referenced: {approvedOfferingIds.Count}. "
                            + $"Existing terms in SemesterAssignments: {termSummary}."
                };
            }

            var rooms = await _context.Rooms
                .AsNoTracking()
                .Select(r => new RoomInfo { RoomId = r.Id, RoomTypeId = r.RoomTypeId })
                .ToListAsync(cancellationToken);

            var sectionBatchNames = assignments
                .Where(a => a.Section != null)
                .Select(a => new { a.SectionId, a.Section!.IsExtension, BatchName = a.Section.Batch.Name })
                .Distinct()
                .ToList();

            var offeringBatchKeys = new List<OfferingBatchKey>();
            foreach (var s in sectionBatchNames)
            {
                if (TryParseOfferingBatchInfo(s.BatchName, out var key))
                    offeringBatchKeys.Add(new OfferingBatchKey(key.OfferingId, key.YearLevel, key.BatchName, s.IsExtension));
            }

            var offeringBatches = new Dictionary<OfferingBatchKey, CourseOfferingBatch>();
            if (offeringBatchKeys.Count > 0)
            {
                var offeringIds = offeringBatchKeys.Select(k => k.OfferingId).Distinct().ToList();
                var candidates = await _context.CourseOfferingBatches
                    .AsNoTracking()
                    .Where(b => offeringIds.Contains(b.CourseOfferingId))
                    .ToListAsync(cancellationToken);

                foreach (var k in offeringBatchKeys.Distinct())
                {
                    var match = candidates.FirstOrDefault(b =>
                        b.CourseOfferingId == k.OfferingId &&
                        b.YearLevel == k.YearLevel &&
                        string.Equals(b.BatchName, k.BatchName, StringComparison.OrdinalIgnoreCase) &&
                        b.IsExtension == k.IsExtension);

                    if (match != null)
                        offeringBatches[k] = match;
                }
            }

            var offeringBatchIds = offeringBatches.Values.Select(b => b.Id).Distinct().ToList();
            var offeringSections = new List<CourseOfferingSection>();
            if (offeringBatchIds.Count > 0)
            {
                offeringSections = await _context.CourseOfferingSections
                    .AsNoTracking()
                    .Include(s => s.RoomRequirements)
                    .Include(s => s.SectionInstructors)
                    .Where(s => s.OfferingBatchId != null && offeringBatchIds.Contains(s.OfferingBatchId.Value))
                    .ToListAsync(cancellationToken);
            }

            var offeringSectionLookup = offeringSections
                .Where(s => s.OfferingBatchId != null)
                .GroupBy(s => (OfferingBatchId: s.OfferingBatchId!.Value, s.CourseId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var solverAssignments = new List<SolverAssignment>();
            foreach (var a in assignments)
            {
                if (a.Course == null) continue;
                if (a.Section == null) continue;

                var isArchitecture = string.Equals(a.Course.Department, "Architecture", StringComparison.OrdinalIgnoreCase);

                var fixedRoomId = a.RoomId;
                var requiredRoomTypeIds = Array.Empty<int>();
                var isFullDay = false;
                int? offeringAssignedHours = null;
                var allInstructorIds = new List<string>();
                if (!string.IsNullOrWhiteSpace(a.InstructorId))
                    allInstructorIds.Add(a.InstructorId);

                if (TryParseOfferingBatchInfo(a.Section.Batch.Name, out var parsed))
                {
                    var key = new OfferingBatchKey(parsed.OfferingId, parsed.YearLevel, parsed.BatchName, a.Section.IsExtension);
                    if (offeringBatches.TryGetValue(key, out var ob))
                    {
                        if (offeringSectionLookup.TryGetValue((ob.Id, a.CourseId), out var rows) && rows.Count > 0)
                        {
                            var row = rows[0];
                            isFullDay = row.IsFullDay;
                            if (row.RoomId.HasValue && !fixedRoomId.HasValue)
                                fixedRoomId = row.RoomId;

                            requiredRoomTypeIds = rows
                                .SelectMany(r => r.RoomRequirements)
                                .Select(rr => rr.RoomTypeId)
                                .Distinct()
                                .ToArray();

                            if (row.AssignedContactHours > 0)
                                offeringAssignedHours = row.AssignedContactHours;

                            if (isArchitecture)
                            {
                                allInstructorIds = rows
                                    .SelectMany(r => r.SectionInstructors)
                                    .Select(si => si.InstructorId)
                                    .Where(id => !string.IsNullOrWhiteSpace(id))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                                if (allInstructorIds.Count == 0 && !string.IsNullOrWhiteSpace(a.InstructorId))
                                    allInstructorIds.Add(a.InstructorId);
                            }
                        }
                    }
                }

                if (allInstructorIds.Count > 0 && !string.IsNullOrWhiteSpace(a.InstructorId) &&
                    !allInstructorIds.Contains(a.InstructorId, StringComparer.OrdinalIgnoreCase))
                {
                    allInstructorIds.Insert(0, a.InstructorId);
                }

                var contactHours = offeringAssignedHours ?? (a.Course.LectureHours + a.Course.LabHours);
                if (contactHours <= 0) contactHours = 1;

                solverAssignments.Add(new SolverAssignment
                {
                    AssignmentId = a.Id,
                    SectionId = a.SectionId,
                    CourseId = a.CourseId,
                    InstructorId = a.InstructorId,
                    AllInstructorIds = allInstructorIds,
                    IsExtension = a.Section.IsExtension,
                    IsArchitecture = isArchitecture,
                    IsFullDay = isFullDay,
                    ContactHours = contactHours,
                    FixedRoomId = fixedRoomId,
                    RequiredRoomTypeIds = requiredRoomTypeIds
                });
            }

            var instructorIds = solverAssignments
                .SelectMany(a => a.AllInstructorIds)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var availabilityMap = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (instructorIds.Count > 0)
            {
                var instructorAvailability = await _context.Users
                    .AsNoTracking()
                    .Where(u => instructorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.AvailabilitySlots })
                    .ToListAsync(cancellationToken);

                availabilityMap = instructorAvailability
                    .ToDictionary(x => x.Id, x => x.AvailabilitySlots, StringComparer.OrdinalIgnoreCase);
            }

            var allowedSlotsByInstructor = BuildInstructorAllowedSlots(availabilityMap);

            var solverInput = new ScheduleSolverInput
            {
                Assignments = solverAssignments,
                Rooms = rooms,
                InstructorAllowedSlots = allowedSlotsByInstructor
            };

            List<ScheduledMeetingRow> scheduled;
            try
            {
                scheduled = SolveInternal(solverInput);
            }
            catch (Exception ex)
            {
                return new ScheduleRunResult
                {
                    AcademicYear = academicYear,
                    Semester = semester,
                    Success = false,
                    Error = ex.Message
                };
            }

            // Replace any existing draft/rejected schedule workspace for this term.
            var existingWorkspace = await _context.SchedulePublications
                .Where(p => p.AcademicYear == academicYear
                            && p.Semester == semester
                            && (p.Status == SchedulePublicationStatus.DraftGenerated || p.Status == SchedulePublicationStatus.Rejected))
                .OrderByDescending(p => p.GeneratedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingWorkspace != null)
            {
                var existingWorkspaceMeetings = await _context.ScheduleMeetings
                    .Where(m => m.SchedulePublicationId == existingWorkspace.Id)
                    .ToListAsync(cancellationToken);

                if (existingWorkspaceMeetings.Count > 0)
                    _context.ScheduleMeetings.RemoveRange(existingWorkspaceMeetings);

                _context.SchedulePublications.Remove(existingWorkspace);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var publication = new SchedulePublication
            {
                AcademicYear = academicYear,
                Semester = semester,
                Status = SchedulePublicationStatus.DraftGenerated,
                GeneratedAtUtc = DateTime.UtcNow,
                GeneratedByUserId = generatedByUserId
            };

            _context.SchedulePublications.Add(publication);
            await _context.SaveChangesAsync(cancellationToken);

            var sectionIds = solverAssignments.Select(x => x.SectionId).Distinct().ToList();
            var grids = await _context.ScheduleGrids
                .Where(g => sectionIds.Contains(g.SectionId))
                .ToListAsync(cancellationToken);

            foreach (var sectionId in sectionIds)
            {
                if (grids.Any(g => g.SectionId == sectionId))
                    continue;

                var grid = new ScheduleGrid { SectionId = sectionId };
                _context.ScheduleGrids.Add(grid);
                grids.Add(grid);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var gridIdBySectionId = grids.ToDictionary(g => g.SectionId, g => g.Id);

            foreach (var row in scheduled)
            {
                if (!gridIdBySectionId.TryGetValue(row.SectionId, out var gridId))
                    continue;

                _context.ScheduleMeetings.Add(new ScheduleMeeting
                {
                    SchedulePublicationId = publication.Id,
                    ScheduleGridId = gridId,
                    AcademicYear = academicYear,
                    Semester = semester,
                    CourseId = row.CourseId,
                    InstructorId = row.InstructorId,
                    RoomId = row.RoomId,
                    DayOfWeek = row.DayOfWeek,
                    SlotStart = row.SlotStart,
                    SlotLength = row.SlotLength
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new ScheduleRunResult
            {
                AcademicYear = academicYear,
                Semester = semester,
                Success = true,
                MeetingsCreated = scheduled.Count,
                AssignmentsScheduled = scheduled.Select(s => s.AssignmentId).Distinct().Count(),
                TotalAssignments = solverAssignments.Count,
                PublicationId = publication.Id
            };
        }

        public List<ScheduledMeetingRow> Solve(ScheduleSolverInput input)
        {
            return SolveInternal(input);
        }

        private static List<ScheduledMeetingRow> SolveInternal(ScheduleSolverInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Assignments == null) throw new ArgumentException("Assignments is required.", nameof(input));
            if (input.Rooms == null) throw new ArgumentException("Rooms is required.", nameof(input));

            var model = new CpModel();

            var instructorIntervals = new Dictionary<string, List<IntervalVar>>(StringComparer.OrdinalIgnoreCase);
            var roomIntervals = new Dictionary<int, List<IntervalVar>>();
            var sectionIntervals = new Dictionary<int, List<IntervalVar>>();

            var decisions = new List<SessionDecision>();
            var availabilityViolationLits = new List<ILiteral>();

            foreach (var a in input.Assignments)
            {
                if (a == null) continue;

                var sessionLengths = GetSessionLengths(a);
                var sessionInstructorIds = GetSessionInstructorIds(a, sessionLengths.Count);

                var sessionDayVars = new List<IntVar>();

                for (var s = 0; s < sessionLengths.Count; s++)
                {
                    var sessionInstructorId = s < sessionInstructorIds.Count ? sessionInstructorIds[s] : a.InstructorId;
                    var length = sessionLengths[s];
                    if (length <= 0) throw new InvalidOperationException($"Invalid session length={length} for assignment {a.AssignmentId}.");

                    var candidateStarts = BuildCandidateStarts(a.IsExtension, a.IsArchitecture, length, forbiddenSlots: null);

                    if (candidateStarts.Count == 0)
                        throw new InvalidOperationException(
                            $"No feasible start times for assignment {a.AssignmentId}, session {s}, length {length} (extension={a.IsExtension}).");

                    var startVar = model.NewIntVar(TimelineMin, TimelineMax, $"a{a.AssignmentId}_s{s}_start");
                    var dayVar = model.NewIntVar(1, 7, $"a{a.AssignmentId}_s{s}_day");

                    // Start selection (exactly one candidate start)
                    var startChoices = candidateStarts
                        .Select(v => model.NewBoolVar($"a{a.AssignmentId}_s{s}_start_{v}"))
                        .ToArray();

                    model.Add(LinearExpr.Sum(startChoices) == 1);

                    // startVar == Sum(choice_i * startValue_i)
                    {
                        var terms = new LinearExpr[startChoices.Length];
                        for (var i = 0; i < startChoices.Length; i++)
                            terms[i] = startChoices[i] * candidateStarts[i];
                        model.Add(startVar == LinearExpr.Sum(terms));
                    }

                    // dayVar == Sum(choice_i * dayValue_i)
                    {
                        var terms = new LinearExpr[startChoices.Length];
                        for (var i = 0; i < startChoices.Length; i++)
                        {
                            DecodeFlatStart(candidateStarts[i], out var dayOfWeek, out _);
                            terms[i] = startChoices[i] * dayOfWeek;
                        }
                        model.Add(dayVar == LinearExpr.Sum(terms));
                    }

                    // Availability soft constraint: penalize if any slot of the session is outside allowed blocks.
                    // The allowed set is interpreted as "in campus only in these marked blocks".
                    // We do NOT make it hard; we minimize violations.
                    {
                        var allowed = GetAllowedForInstructor(input.InstructorAllowedSlots, sessionInstructorId);
                        if (allowed != null && allowed.Count > 0)
                        {
                            for (var i = 0; i < candidateStarts.Count; i++)
                            {
                                var start = candidateStarts[i];
                                var ok = IsAllowedRange(start, length, allowed);
                                if (!ok)
                                {
                                    availabilityViolationLits.Add(startChoices[i]);
                                }
                            }
                        }
                    }

                    sessionDayVars.Add(dayVar);

                    var candidateRoomIds = BuildCandidateRoomIds(a, input.Rooms);

                    if (candidateRoomIds.Count == 0)
                        throw new InvalidOperationException($"No candidate rooms for assignment {a.AssignmentId}.");

                    var roomPresence = new Dictionary<int, BoolVar>();
                    var roomOptionalIntervals = new Dictionary<int, IntervalVar>();

                    foreach (var roomId in candidateRoomIds)
                    {
                        var present = model.NewBoolVar($"a{a.AssignmentId}_s{s}_room{roomId}");
                        roomPresence[roomId] = present;

                        var interval = model.NewOptionalFixedSizeIntervalVar(
                            startVar,
                            length,
                            present,
                            $"a{a.AssignmentId}_s{s}_room{roomId}_int");

                        roomOptionalIntervals[roomId] = interval;

                        AddInterval(roomIntervals, roomId, interval);

                        if (a.SectionId > 0)
                            AddInterval(sectionIntervals, a.SectionId, interval);

                        if (!string.IsNullOrWhiteSpace(sessionInstructorId))
                            AddInterval(instructorIntervals, sessionInstructorId, interval);
                    }

                    // Exactly one room selected for this session
                    model.Add(LinearExpr.Sum(roomPresence.Values.ToArray()) == 1);

                    decisions.Add(new SessionDecision(
                        assignment: a,
                        sessionIndex: s,
                        length: length,
                        startVar: startVar,
                        dayVar: dayVar,
                        roomPresence: roomPresence,
                        roomOptionalIntervals: roomOptionalIntervals));
                }

                // Session splitting: sessions must be on different days
                // (applies as given; no extra inference)
                if (!a.IsArchitecture && sessionDayVars.Count == 2)
                {
                    model.Add(sessionDayVars[0] != sessionDayVars[1]);
                }
                if (a.IsArchitecture && sessionDayVars.Count > 1)
                {
                    var maxDistinctDays = a.IsExtension ? 7 : 5;
                    if (sessionDayVars.Count <= maxDistinctDays)
                    {
                        for (var i = 0; i < sessionDayVars.Count; i++)
                        {
                            for (var j = i + 1; j < sessionDayVars.Count; j++)
                                model.Add(sessionDayVars[i] != sessionDayVars[j]);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < sessionDayVars.Count - 1; i++)
                            model.Add(sessionDayVars[i] != sessionDayVars[i + 1]);
                    }
                }
            }

            // No overlap constraints
            foreach (var kvp in sectionIntervals)
                model.AddNoOverlap(kvp.Value);

            foreach (var kvp in roomIntervals)
                model.AddNoOverlap(kvp.Value);

            foreach (var kvp in instructorIntervals)
                model.AddNoOverlap(kvp.Value);

            // Solve
            var solver = new CpSolver();

            // Cap runtime so it won't grind indefinitely when constraints are tight.
            solver.StringParameters = "max_time_in_seconds:10";

            // Objective: minimize number of availability violations.
            if (availabilityViolationLits.Count > 0)
                model.Minimize(LinearExpr.Sum(availabilityViolationLits.Select(l => (LinearExpr)l).ToArray()));

            var status = solver.Solve(model);

            if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                return new List<ScheduledMeetingRow>();

            // Extract scheduled rows
            var result = new List<ScheduledMeetingRow>();

            foreach (var d in decisions)
            {
                var flatStart = (int)solver.Value(d.StartVar);
                DecodeFlatStart(flatStart, out var dayOfWeek, out var slotStart);

                var dSessionLengths = GetSessionLengths(d.Assignment);
                var dSessionInstructorIds = GetSessionInstructorIds(d.Assignment, dSessionLengths.Count);
                var chosenInstructorId = d.SessionIndex < dSessionInstructorIds.Count
                    ? dSessionInstructorIds[d.SessionIndex]
                    : d.Assignment.InstructorId;

                var chosenRoomId = d.RoomPresence
                    .Where(p => solver.BooleanValue(p.Value))
                    .Select(p => p.Key)
                    .FirstOrDefault();

                result.Add(new ScheduledMeetingRow
                {
                    AssignmentId = d.Assignment.AssignmentId,
                    SectionId = d.Assignment.SectionId,
                    CourseId = d.Assignment.CourseId,
                    InstructorId = chosenInstructorId,
                    RoomId = chosenRoomId,
                    SessionIndex = d.SessionIndex,
                    FlatStart = flatStart,
                    DayOfWeek = dayOfWeek,
                    SlotStart = slotStart,
                    SlotLength = d.Length
                });
            }

            return result;
        }

        private static HashSet<int>? GetAllowedUnion(
            IReadOnlyDictionary<string, HashSet<int>> allowedByInstructor,
            IReadOnlyList<string> instructorIds)
        {
            if (instructorIds == null || instructorIds.Count == 0)
                return null;

            HashSet<int>? union = null;
            foreach (var id in instructorIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!allowedByInstructor.TryGetValue(id, out var a)) continue;
                if (a.Count == 0) continue;

                union ??= new HashSet<int>();
                union.UnionWith(a);
            }

            return union;
        }

        private static HashSet<int>? GetAllowedForInstructor(
            IReadOnlyDictionary<string, HashSet<int>> allowedByInstructor,
            string? instructorId)
        {
            if (string.IsNullOrWhiteSpace(instructorId))
                return null;

            if (!allowedByInstructor.TryGetValue(instructorId, out var allowed))
                return null;

            return allowed;
        }

        private static List<string?> GetSessionInstructorIds(SolverAssignment a, int sessionCount)
        {
            var result = new List<string?>(Math.Max(0, sessionCount));
            if (sessionCount <= 0)
                return result;

            if (!a.IsArchitecture)
            {
                for (var i = 0; i < sessionCount; i++)
                    result.Add(a.InstructorId);
                return result;
            }

            var ids = (a.AllInstructorIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0)
            {
                for (var i = 0; i < sessionCount; i++)
                    result.Add(a.InstructorId);
                return result;
            }

            for (var i = 0; i < sessionCount; i++)
                result.Add(ids[i % ids.Count]);

            return result;
        }

        private static bool IsAllowedRange(int start, int length, HashSet<int> allowedSlots)
        {
            for (var t = start; t < start + length; t++)
            {
                if (!allowedSlots.Contains(t))
                    return false;
            }
            return true;
        }

        private async Task<(string AcademicYear, string Semester)> GetActiveAcademicYearSemesterAsync(CancellationToken cancellationToken)
        {
            var active = await _context.AcademicPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive, cancellationToken);
            if (active != null)
            {
                var academicYear = $"{active.StartDate.Year}/{active.EndDate.Year}";
                var name = (active.Name ?? "").ToLowerInvariant();
                var sem = name.Contains("ii") ? "II" : name.Contains("summer") ? "Summer" : "I";
                return (academicYear, sem);
            }

            var now = DateTime.Now;
            return (now.Year + "/" + (now.Year + 1), "I");
        }

        private static Dictionary<string, HashSet<int>> BuildInstructorAllowedSlots(
            IReadOnlyDictionary<string, string?> availabilityByInstructor)
        {
            var result = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in availabilityByInstructor)
            {
                var id = kvp.Key;
                var csv = kvp.Value;

                if (string.IsNullOrWhiteSpace(csv))
                {
                    result[id] = new HashSet<int>();
                    continue;
                }

                var allowed = new HashSet<int>();
                var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var p in parts)
                {
                    if (!TryParseAvailabilityToken(p, out var dayIndex, out var block))
                        continue;

                    AddAllowedSlotsForBlock(allowed, dayIndex, block);
                }

                result[id] = allowed;
            }

            return result;
        }

        private static bool TryParseAvailabilityToken(string token, out int dayIndex, out string block)
        {
            dayIndex = -1;
            block = "";
            if (string.IsNullOrWhiteSpace(token)) return false;

            var parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            var day = parts[0].Trim();
            block = parts[1].Trim();

            dayIndex = day.ToLowerInvariant() switch
            {
                "mon" => 0,
                "tue" => 1,
                "wed" => 2,
                "thu" => 3,
                "fri" => 4,
                "sat" => 5,
                "sun" => 6,
                _ => -1
            };

            return dayIndex >= 0 && (block.Equals("AM", StringComparison.OrdinalIgnoreCase)
                                     || block.Equals("PM", StringComparison.OrdinalIgnoreCase)
                                     || block.Equals("Night", StringComparison.OrdinalIgnoreCase));
        }

        private static void AddAllowedSlotsForBlock(HashSet<int> allowed, int dayIndex, string block)
        {
            if (dayIndex is >= 0 and <= 4)
            {
                if (block.Equals("AM", StringComparison.OrdinalIgnoreCase))
                {
                    var start = dayIndex * RegularSlotsPerDay;
                    for (var i = 0; i < 4; i++) allowed.Add(start + i);
                    return;
                }

                if (block.Equals("PM", StringComparison.OrdinalIgnoreCase))
                {
                    var start = dayIndex * RegularSlotsPerDay;
                    for (var i = 4; i < 8; i++) allowed.Add(start + i);
                    return;
                }

                if (block.Equals("Night", StringComparison.OrdinalIgnoreCase))
                {
                    var start = ExtNightStart + dayIndex * ExtNightSlotsPerDay;
                    for (var i = 0; i < ExtNightSlotsPerDay; i++) allowed.Add(start + i);
                }

                return;
            }

            if (dayIndex is 5 or 6)
            {
                if (block.Equals("AM", StringComparison.OrdinalIgnoreCase))
                {
                    var start = ExtWeekendStart + (dayIndex - 5) * ExtWeekendSlotsPerDay;
                    for (var i = 0; i < 4; i++) allowed.Add(start + i);
                    return;
                }

                if (block.Equals("PM", StringComparison.OrdinalIgnoreCase))
                {
                    var start = ExtWeekendStart + (dayIndex - 5) * ExtWeekendSlotsPerDay;
                    for (var i = 4; i < 8; i++) allowed.Add(start + i);
                }
            }
        }

        private readonly record struct ParsedOfferingBatchInfo(int OfferingId, int YearLevel, string BatchName);

        private readonly record struct OfferingBatchKey(int OfferingId, int YearLevel, string BatchName, bool IsExtension);

        private static bool TryParseOfferingBatchInfo(string? systemBatchName, out ParsedOfferingBatchInfo info)
        {
            info = default;
            if (string.IsNullOrWhiteSpace(systemBatchName)) return false;

            var idx = systemBatchName.IndexOf("Year ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var afterYear = systemBatchName[(idx + 5)..];
            var dashIdx = afterYear.IndexOf("-", StringComparison.Ordinal);
            if (dashIdx < 0) return false;

            var yearPart = afterYear[..dashIdx].Trim();
            if (!int.TryParse(new string(yearPart.Where(char.IsDigit).ToArray()), out var yearLevel))
                return false;

            var tokenIdx = systemBatchName.IndexOf("(", StringComparison.Ordinal);
            if (tokenIdx < 0) return false;

            var beforeToken = systemBatchName[..tokenIdx];
            var dash = beforeToken.LastIndexOf("-", StringComparison.Ordinal);
            if (dash < 0) return false;

            var batchName = beforeToken[(dash + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(batchName)) return false;

            var offIdx = systemBatchName.IndexOf("Off ", StringComparison.OrdinalIgnoreCase);
            if (offIdx < 0) return false;
            var afterOff = systemBatchName[(offIdx + 4)..];
            var endIdx = afterOff.IndexOf(")", StringComparison.Ordinal);
            var offIdStr = (endIdx >= 0 ? afterOff[..endIdx] : afterOff).Trim();
            if (!int.TryParse(new string(offIdStr.Where(char.IsDigit).ToArray()), out var offeringId))
                return false;

            info = new ParsedOfferingBatchInfo(offeringId, yearLevel, batchName);
            return true;
        }

        private static void AddInterval<TKey>(Dictionary<TKey, List<IntervalVar>> map, TKey key, IntervalVar interval)
            where TKey : notnull
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<IntervalVar>();
                map[key] = list;
            }
            list.Add(interval);
        }

        private static List<int> GetSessionLengths(SolverAssignment a)
        {
            if (a.PreferredSessionLengths != null && a.PreferredSessionLengths.Count > 0)
                return a.PreferredSessionLengths.Where(x => x > 0).ToList();

            if (a.IsArchitecture)
            {
                var contactHours = Math.Max(1, a.ContactHours);
                var instructorCount = a.AllInstructorIds?.Count ?? 0;

                if (a.IsFullDay && contactHours > 8)
                {
                    var lengths = new List<int>();
                    var remaining = contactHours;
                    while (remaining > 8)
                    {
                        lengths.Add(8);
                        remaining -= 8;
                    }
                    lengths.Add(Math.Max(1, remaining));
                    return lengths;
                }

                if (instructorCount > 1)
                {
                    var sessions = Math.Min(instructorCount, contactHours);
                    return SplitRoughlyEqual(contactHours, sessions);
                }

                return new List<int> { contactHours };
            }

            // Non-Architecture: split into exactly two sessions
            return SplitIntoTwoSessions(Math.Max(1, a.ContactHours));
        }

        private static List<int> SplitRoughlyEqual(int totalSlots, int parts)
        {
            totalSlots = Math.Max(1, totalSlots);
            parts = Math.Max(1, Math.Min(parts, totalSlots));

            var baseLen = totalSlots / parts;
            var remainder = totalSlots % parts;

            var lengths = new List<int>(parts);
            for (var i = 0; i < parts; i++)
                lengths.Add(baseLen + (i < remainder ? 1 : 0));

            return lengths;
        }

        private static List<int> SplitIntoTwoSessions(int totalSlots)
        {
            var first = Math.Max(1, totalSlots / 2);
            var second = Math.Max(1, totalSlots - first);
            return new List<int> { first, second };
        }

        private static List<int> BuildCandidateRoomIds(SolverAssignment a, IReadOnlyList<RoomInfo> rooms)
        {
            if (a.FixedRoomId.HasValue)
                return new List<int> { a.FixedRoomId.Value };

            if (a.RequiredRoomTypeIds == null || a.RequiredRoomTypeIds.Count == 0)
                return rooms.Select(r => r.RoomId).Distinct().ToList();

            var typeSet = new HashSet<int>(a.RequiredRoomTypeIds);
            var candidates = rooms.Where(r => typeSet.Contains(r.RoomTypeId)).Select(r => r.RoomId).Distinct().ToList();

            // Reasonable default if requirements yield no rooms (still hard constraints elsewhere could make it infeasible)
            if (candidates.Count == 0)
                candidates = rooms.Select(r => r.RoomId).Distinct().ToList();

            return candidates;
        }

        private static bool CrossesLunchBreak(int slotStart0Based, int length)
        {
            // Slots are 0-based inside a day. Lunch break is between slot 3 (4th) and slot 4 (5th).
            var end0Based = slotStart0Based + length - 1;
            return slotStart0Based <= 3 && end0Based >= 4;
        }

        private static List<int> BuildCandidateStarts(bool isExtension, bool isArchitecture, int length, HashSet<int>? forbiddenSlots)
        {
            var candidates = new List<int>();

            if (!isExtension)
            {
                // Regular: Mon–Fri, 8 slots/day -> 0..39
                for (var day = 0; day < RegularDays; day++)
                {
                    var dayStart = day * RegularSlotsPerDay;
                    for (var slot = 0; slot < RegularSlotsPerDay; slot++)
                    {
                        var start = dayStart + slot;
                        var end = start + length;
                        if (end > dayStart + RegularSlotsPerDay) continue;

                        if (!isArchitecture && CrossesLunchBreak(slotStart0Based: slot, length: length))
                            continue;

                        if (forbiddenSlots != null && forbiddenSlots.Contains(start)) continue;
                        candidates.Add(start);
                    }
                }

                return candidates;
            }

            // Extension: weekdays night (Mon–Fri, slots 9–10) -> 40..49
            for (var day = 0; day < RegularDays; day++)
            {
                var dayStart = ExtNightStart + day * ExtNightSlotsPerDay;
                for (var slot = 0; slot < ExtNightSlotsPerDay; slot++)
                {
                    var start = dayStart + slot;
                    var end = start + length;
                    if (end > dayStart + ExtNightSlotsPerDay) continue;
                    if (forbiddenSlots != null && forbiddenSlots.Contains(start)) continue;
                    candidates.Add(start);
                }
            }

            // Extension: weekends day (Sat–Sun, slots 1–8) -> 50..65
            for (var day = 0; day < 2; day++)
            {
                var dayStart = ExtWeekendStart + day * ExtWeekendSlotsPerDay;
                for (var slot = 0; slot < ExtWeekendSlotsPerDay; slot++)
                {
                    var start = dayStart + slot;
                    var end = start + length;
                    if (end > dayStart + ExtWeekendSlotsPerDay) continue;

                    if (!isArchitecture && CrossesLunchBreak(slotStart0Based: slot, length: length))
                        continue;

                    if (forbiddenSlots != null && forbiddenSlots.Contains(start)) continue;
                    candidates.Add(start);
                }
            }

            return candidates;
        }

        private static bool IsForbiddenRange(int start, int length, HashSet<int>? forbiddenSlots)
        {
            if (forbiddenSlots == null || forbiddenSlots.Count == 0) return false;

            for (var t = start; t < start + length; t++)
            {
                if (forbiddenSlots.Contains(t))
                    return true;
            }
            return false;
        }

        private static void DecodeFlatStart(int flat, out int dayOfWeek, out int slotStart)
        {
            // dayOfWeek: 1=Mon..7=Sun
            // slotStart: 1..10 (as used by ScheduleMeeting)

            if (flat >= 0 && flat <= 39)
            {
                var day = flat / RegularSlotsPerDay;      // 0..4
                var slot = flat % RegularSlotsPerDay;     // 0..7
                dayOfWeek = day + 1;                      // 1..5
                slotStart = slot + 1;                     // 1..8
                return;
            }

            if (flat >= 40 && flat <= 49)
            {
                var offset = flat - ExtNightStart;        // 0..9
                var day = offset / ExtNightSlotsPerDay;   // 0..4 => Mon..Fri
                var slot = offset % ExtNightSlotsPerDay;  // 0..1
                dayOfWeek = day + 1;                      // 1..5
                slotStart = 9 + slot;                     // 9..10
                return;
            }

            if (flat >= 50 && flat <= 65)
            {
                var offset = flat - ExtWeekendStart;      // 0..15
                var day = offset / ExtWeekendSlotsPerDay; // 0..1 => Sat..Sun
                var slot = offset % ExtWeekendSlotsPerDay;// 0..7
                dayOfWeek = 6 + day;                      // 6..7
                slotStart = 1 + slot;                     // 1..8
                return;
            }

            // Default fallback
            dayOfWeek = 1;
            slotStart = 1;
        }

        private sealed class SessionDecision
        {
            public SessionDecision(
                SolverAssignment assignment,
                int sessionIndex,
                int length,
                IntVar startVar,
                IntVar dayVar,
                Dictionary<int, BoolVar> roomPresence,
                Dictionary<int, IntervalVar> roomOptionalIntervals)
            {
                Assignment = assignment;
                SessionIndex = sessionIndex;
                Length = length;
                StartVar = startVar;
                DayVar = dayVar;
                RoomPresence = roomPresence;
                RoomOptionalIntervals = roomOptionalIntervals;
            }

            public SolverAssignment Assignment { get; }
            public int SessionIndex { get; }
            public int Length { get; }
            public IntVar StartVar { get; }
            public IntVar DayVar { get; }
            public Dictionary<int, BoolVar> RoomPresence { get; }
            public Dictionary<int, IntervalVar> RoomOptionalIntervals { get; }
        }
    }

    public sealed class ScheduleSolverInput
    {
        public IReadOnlyList<SolverAssignment> Assignments { get; init; } = Array.Empty<SolverAssignment>();
        public IReadOnlyList<RoomInfo> Rooms { get; init; } = Array.Empty<RoomInfo>();

        // Allowed slots are a soft preference and must be provided as flat timeline indices (0..65).
        public IReadOnlyDictionary<string, HashSet<int>> InstructorAllowedSlots { get; init; }
            = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class SolverAssignment
    {
        public int AssignmentId { get; init; }
        public int SectionId { get; init; }
        public int CourseId { get; init; }
        public string? InstructorId { get; init; }

        public IReadOnlyList<string> AllInstructorIds { get; init; } = Array.Empty<string>();

        public bool IsExtension { get; init; }

        public bool IsArchitecture { get; init; }
        public bool IsFullDay { get; init; }

        // ContactHours expressed in slot units (no DateTime usage).
        public int ContactHours { get; init; }

        // If provided, the solver uses these session lengths verbatim (1 or more sessions).
        public IReadOnlyList<int>? PreferredSessionLengths { get; init; }

        // If true and PreferredSessionLengths has 2 sessions, enforce different-days.
        public bool ForceDifferentDaysForTwoSessions { get; init; } = false;

        // Room logic
        public int? FixedRoomId { get; init; }
        public IReadOnlyList<int> RequiredRoomTypeIds { get; init; } = Array.Empty<int>();
    }

    public sealed class RoomInfo
    {
        public int RoomId { get; init; }
        public int RoomTypeId { get; init; }
    }

    public sealed class ScheduledMeetingRow
    {
        public int AssignmentId { get; init; }
        public int SectionId { get; init; }
        public int CourseId { get; init; }
        public string? InstructorId { get; init; }
        public int RoomId { get; init; }

        public int SessionIndex { get; init; }

        // Flat timeline output
        public int FlatStart { get; init; }

        // Decoded (for convenience; still no DateTime usage)
        public int DayOfWeek { get; init; }   // 1..7
        public int SlotStart { get; init; }   // 1..10
        public int SlotLength { get; init; }  // consecutive slots
    }

    public sealed class ScheduleRunResult
    {
        public string AcademicYear { get; init; } = "";
        public string Semester { get; init; } = "";
        public bool Success { get; init; }
        public string? Error { get; init; }
        public int TotalAssignments { get; init; }
        public int AssignmentsScheduled { get; init; }
        public int MeetingsCreated { get; init; }
        public int? PublicationId { get; init; }
    }
}