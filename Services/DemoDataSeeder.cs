using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Data;
using ScheduleCentral.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleCentral.Services
{
    public static class DemoDataSeeder
    {
        /// <summary>
        /// Seeds the demo in-memory SQLite database with fully realistic data that mirrors
        /// what the real workflow (Department → PO approval → Sections/Assignments generated) would produce.
        /// Batch names, section names, and semester assignments all follow the exact format the
        /// ScheduleSolverService's TryParseOfferingBatchInfo expects.
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext context, IServiceProvider serviceProvider)
        {
            // ===== 1. ROLES (already seeded by EnsureCreated / OnModelCreating) =====
            var adminRole = await context.Roles.FirstAsync(r => r.NormalizedName == "ADMIN");
            var poRole = await context.Roles.FirstAsync(r => r.NormalizedName == "PROGRAMOFFICER");
            var deptRole = await context.Roles.FirstAsync(r => r.NormalizedName == "DEPARTMENT");
            var instructorRole = await context.Roles.FirstAsync(r => r.NormalizedName == "INSTRUCTOR");
            var execRole = await context.Roles.FirstAsync(r => r.NormalizedName == "TOPMANAGEMENT");
            var studentRole = await context.Roles.FirstAsync(r => r.NormalizedName == "STUDENT");

            // ===== 2. USERS =====
            var passwordHasher = new PasswordHasher<ApplicationUser>();

            var users = new List<(ApplicationUser User, string RoleId)>
            {
                (new ApplicationUser { Id = "demo_admin_id",       UserName = "admin.demo@demo.edu",       NormalizedUserName = "ADMIN.DEMO@DEMO.EDU",       Email = "admin.demo@demo.edu",       NormalizedEmail = "ADMIN.DEMO@DEMO.EDU",       EmailConfirmed = true, FirstName = "Demo",    LastName = "Admin",          IsApproved = true,  SecurityStamp = Guid.NewGuid().ToString() }, adminRole.Id),
                (new ApplicationUser { Id = "demo_po_id",          UserName = "po.demo@demo.edu",          NormalizedUserName = "PO.DEMO@DEMO.EDU",          Email = "po.demo@demo.edu",          NormalizedEmail = "PO.DEMO@DEMO.EDU",          EmailConfirmed = true, FirstName = "Demo",    LastName = "Program Officer", IsApproved = true,  SecurityStamp = Guid.NewGuid().ToString() }, poRole.Id),
                (new ApplicationUser { Id = "demo_dept_id",        UserName = "dept.demo@demo.edu",        NormalizedUserName = "DEPT.DEMO@DEMO.EDU",        Email = "dept.demo@demo.edu",        NormalizedEmail = "DEPT.DEMO@DEMO.EDU",        EmailConfirmed = true, FirstName = "Demo",    LastName = "Department Head", IsApproved = true,  Department = "Computer Science", SecurityStamp = Guid.NewGuid().ToString() }, deptRole.Id),
                (new ApplicationUser { Id = "demo_instructor_id",  UserName = "instructor.demo@demo.edu",  NormalizedUserName = "INSTRUCTOR.DEMO@DEMO.EDU",  Email = "instructor.demo@demo.edu",  NormalizedEmail = "INSTRUCTOR.DEMO@DEMO.EDU",  EmailConfirmed = true, FirstName = "Alice",   LastName = "Smith",          IsApproved = true,  Department = "Computer Science", AvailableHours = 12, AvailabilitySlots = "1,2,3,4,11,12,13,14,21,22,23,24", SecurityStamp = Guid.NewGuid().ToString() }, instructorRole.Id),
                (new ApplicationUser { Id = "demo_instructor2_id", UserName = "instructor2.demo@demo.edu", NormalizedUserName = "INSTRUCTOR2.DEMO@DEMO.EDU", Email = "instructor2.demo@demo.edu", NormalizedEmail = "INSTRUCTOR2.DEMO@DEMO.EDU", EmailConfirmed = true, FirstName = "Helen",   LastName = "Carter",         IsApproved = true,  Department = "Architecture",     AvailableHours = 12, AvailabilitySlots = "0,1,2,3,4,5,6,7",          SecurityStamp = Guid.NewGuid().ToString() }, instructorRole.Id),
                (new ApplicationUser { Id = "demo_exec_id",        UserName = "exec.demo@demo.edu",        NormalizedUserName = "EXEC.DEMO@DEMO.EDU",        Email = "exec.demo@demo.edu",        NormalizedEmail = "EXEC.DEMO@DEMO.EDU",        EmailConfirmed = true, FirstName = "Demo",    LastName = "Top Management", IsApproved = true,  SecurityStamp = Guid.NewGuid().ToString() }, execRole.Id),
                (new ApplicationUser { Id = "demo_student_id",     UserName = "student.demo@demo.edu",     NormalizedUserName = "STUDENT.DEMO@DEMO.EDU",     Email = "student.demo@demo.edu",     NormalizedEmail = "STUDENT.DEMO@DEMO.EDU",     EmailConfirmed = true, FirstName = "Demo",    LastName = "Student",        IsApproved = true,  SecurityStamp = Guid.NewGuid().ToString() }, studentRole.Id),
                (new ApplicationUser { Id = "demo_pending_id",     UserName = "pending.demo@demo.edu",     NormalizedUserName = "PENDING.DEMO@DEMO.EDU",     Email = "pending.demo@demo.edu",     NormalizedEmail = "PENDING.DEMO@DEMO.EDU",     EmailConfirmed = true, FirstName = "Pending", LastName = "Student",        IsApproved = false, SecurityStamp = Guid.NewGuid().ToString() }, studentRole.Id)
            };

            foreach (var (user, roleId) in users)
            {
                if (!await context.Users.AnyAsync(x => x.Id == user.Id))
                {
                    user.PasswordHash = passwordHasher.HashPassword(user, "Demo!123");
                    await context.Users.AddAsync(user);
                    await context.UserRoles.AddAsync(new IdentityUserRole<string> { UserId = user.Id, RoleId = roleId });
                }
            }
            await context.SaveChangesAsync();

            // ===== 3. ROOM TYPES =====
            var roomTypes = new List<RoomType>
            {
                new RoomType { Id = 1, Name = "Lecture Hall",  Description = "Large lecture hall for major classes" },
                new RoomType { Id = 2, Name = "Computer Lab",  Description = "Fully equipped workstation lab" },
                new RoomType { Id = 3, Name = "Design Studio", Description = "Drafting studio workspace" },
                new RoomType { Id = 4, Name = "Lecture Room",  Description = "Standard classrooms" }
            };
            foreach (var rt in roomTypes)
                if (!await context.RoomTypes.AnyAsync(x => x.Id == rt.Id))
                    await context.RoomTypes.AddAsync(rt);
            await context.SaveChangesAsync();

            // ===== 4. ROOMS =====
            var rooms = new List<Room>
            {
                new Room { Id = 101, Name = "Room 101", Capacity = 100, RoomTypeId = 1 },
                new Room { Id = 102, Name = "Room 102", Capacity = 40,  RoomTypeId = 4 },
                new Room { Id = 201, Name = "Lab 201",  Capacity = 30,  RoomTypeId = 2 },
                new Room { Id = 301, Name = "Studio A", Capacity = 40,  RoomTypeId = 3 }
            };
            foreach (var rm in rooms)
                if (!await context.Rooms.AnyAsync(x => x.Id == rm.Id))
                    await context.Rooms.AddAsync(rm);
            await context.SaveChangesAsync();

            // ===== 5. DEPARTMENTS =====
            if (!await context.Departments.AnyAsync(x => x.Id == 1))
                await context.Departments.AddAsync(new Department { Id = 1, Name = "Computer Science", Code = "CS",   HeadId = "demo_dept_id" });
            if (!await context.Departments.AnyAsync(x => x.Id == 2))
                await context.Departments.AddAsync(new Department { Id = 2, Name = "Architecture",     Code = "ARCH", HeadId = null });
            await context.SaveChangesAsync();

            // ===== 6. COURSES =====
            var courses = new List<Course>
            {
                new Course { Id = 1, Code = "CS-101",   Name = "Intro to Programming", CreditHours = 3, LectureHours = 2, LabHours = 2, Department = "Computer Science" },
                new Course { Id = 2, Code = "CS-202",   Name = "Data Structures",       CreditHours = 4, LectureHours = 3, LabHours = 1, Department = "Computer Science" },
                new Course { Id = 3, Code = "ARCH-101", Name = "Design Studio I",       CreditHours = 4, LectureHours = 2, LabHours = 4, Department = "Architecture" },
                new Course { Id = 4, Code = "MATH-101", Name = "Calculus I",            CreditHours = 3, LectureHours = 3, LabHours = 0, Department = "Computer Science" }
            };
            foreach (var c in courses)
                if (!await context.Courses.AnyAsync(x => x.Id == c.Id))
                    await context.Courses.AddAsync(c);
            await context.SaveChangesAsync();

            // ===== 7. ACADEMIC PERIOD =====
            // StartDate year / EndDate year → "2024/2025"
            // Name contains "II"? → "II". Contains "summer"? → "Summer". Else → "I"
            if (!await context.AcademicPeriods.AnyAsync(x => x.Id == 1))
                await context.AcademicPeriods.AddAsync(new AcademicPeriod
                {
                    Id = 1,
                    Name = "2024/2025 Semester I",   // ← "I" because it doesn't contain "ii" or "summer"
                    StartDate = new DateTime(2024, 9, 1,  0, 0, 0, DateTimeKind.Utc),
                    EndDate   = new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    IsActive  = true
                });
            await context.SaveChangesAsync();

            // ===== 8. INSTRUCTOR QUALIFICATIONS =====
            var qualifications = new List<InstructorCourse>
            {
                new InstructorCourse { Id = 1, InstructorId = "demo_instructor_id",  CourseId = 1 },
                new InstructorCourse { Id = 2, InstructorId = "demo_instructor_id",  CourseId = 2 },
                new InstructorCourse { Id = 3, InstructorId = "demo_instructor_id",  CourseId = 4 },
                new InstructorCourse { Id = 4, InstructorId = "demo_instructor2_id", CourseId = 3 }
            };
            foreach (var q in qualifications)
                if (!await context.InstructorQualifications.AnyAsync(x => x.Id == q.Id))
                    await context.InstructorQualifications.AddAsync(q);
            await context.SaveChangesAsync();

            // ===== 9. COURSE OFFERING =====
            // The offering is in Approved state (from Department side).
            // Academic year and semester must match what GetActiveAcademicYearSemesterAsync() returns:
            //   → AcademicYear: "2024/2025"  (StartDate.Year / EndDate.Year)
            //   → Semester:     "I"           (Name does not contain "ii" or "summer")
            const string academicYear = "2024/2025";
            const string semester     = "I";
            const int offeringId      = 1;

            if (!await context.CourseOfferings.AnyAsync(x => x.Id == offeringId))
            {
                await context.CourseOfferings.AddAsync(new CourseOffering
                {
                    Id           = offeringId,
                    Department   = "Computer Science",
                    AcademicYear = academicYear,
                    Semester     = semester,
                    Status       = OfferingStatus.Approved
                });
                await context.SaveChangesAsync();
            }

            // ===== 10. OFFERING BATCHES =====
            // These drive section count and are read by ProgramOfficerController when generating sections.
            var offeringBatches = new List<CourseOfferingBatch>
            {
                new CourseOfferingBatch { Id = 1, CourseOfferingId = offeringId, YearLevel = 1, Semester = semester, BatchName = "Batch I", SectionCount = 1, IsExtension = false },
                new CourseOfferingBatch { Id = 2, CourseOfferingId = offeringId, YearLevel = 2, Semester = semester, BatchName = "Batch I", SectionCount = 1, IsExtension = false }
            };
            foreach (var ob in offeringBatches)
                if (!await context.CourseOfferingBatches.AnyAsync(x => x.Id == ob.Id))
                    await context.CourseOfferingBatches.AddAsync(ob);
            await context.SaveChangesAsync();

            // ===== 11. SYSTEM BATCHES + SECTIONS + GRIDS + SEMESTER ASSIGNMENTS =====
            // CRITICAL: Batch names MUST follow the format the solver's TryParseOfferingBatchInfo parses:
            //   "{deptCode} Year {yearLevel} - {batchName} ({academicYear} Sem {semester} Off {offeringId})"
            // Parser requires:
            //   - "Year " substring (case-insensitive)
            //   - A "-" after the year number
            //   - A "(" bracket containing "Off {id}"
            // Department code = "CS" (dept.Code)
            // offeringToken = "(2024/2025 Sem I Off 1)"
            var offeringToken = $"({academicYear} Sem {semester} Off {offeringId})";

            // Batch 1 → Year 1, "Batch I"
            var sysBatch1Name = $"CS Year 1 - Batch I {offeringToken}";
            var sysBatch1 = await context.Batches.Include(b => b.Sections)
                .FirstOrDefaultAsync(b => b.DepartmentId == 1 && b.Name == sysBatch1Name);
            if (sysBatch1 == null)
            {
                sysBatch1 = new Batch { DepartmentId = 1, Name = sysBatch1Name };
                context.Batches.Add(sysBatch1);
                await context.SaveChangesAsync();
            }

            // Batch 2 → Year 2, "Batch I"
            var sysBatch2Name = $"CS Year 2 - Batch I {offeringToken}";
            var sysBatch2 = await context.Batches.Include(b => b.Sections)
                .FirstOrDefaultAsync(b => b.DepartmentId == 1 && b.Name == sysBatch2Name);
            if (sysBatch2 == null)
            {
                sysBatch2 = new Batch { DepartmentId = 1, Name = sysBatch2Name };
                context.Batches.Add(sysBatch2);
                await context.SaveChangesAsync();
            }

            // Section for Batch 1 (Year 1, Section 1)
            // Section name format from real system: "{deptCode}{yearLevel}{batchToken}Sec{i}"
            // ExtractBatchToken("Batch I") → strips non-alphanumeric → "BatchI"
            const string sec1Name = "CS1BatchISec1";
            var sec1 = await context.Sections.FirstOrDefaultAsync(s => s.BatchId == sysBatch1.Id && s.Name == sec1Name);
            if (sec1 == null)
            {
                sec1 = new Section { BatchId = sysBatch1.Id, Name = sec1Name, IsExtension = false, NumberOfStudents = 35 };
                context.Sections.Add(sec1);
                await context.SaveChangesAsync();
            }
            if (!await context.ScheduleGrids.AnyAsync(g => g.SectionId == sec1.Id))
            {
                context.ScheduleGrids.Add(new ScheduleGrid { SectionId = sec1.Id });
                await context.SaveChangesAsync();
            }

            // Section for Batch 2 (Year 2, Section 1)
            const string sec2Name = "CS2BatchISec1";
            var sec2 = await context.Sections.FirstOrDefaultAsync(s => s.BatchId == sysBatch2.Id && s.Name == sec2Name);
            if (sec2 == null)
            {
                sec2 = new Section { BatchId = sysBatch2.Id, Name = sec2Name, IsExtension = false, NumberOfStudents = 30 };
                context.Sections.Add(sec2);
                await context.SaveChangesAsync();
            }
            if (!await context.ScheduleGrids.AnyAsync(g => g.SectionId == sec2.Id))
            {
                context.ScheduleGrids.Add(new ScheduleGrid { SectionId = sec2.Id });
                await context.SaveChangesAsync();
            }

            // ===== 12. OFFERING SECTIONS (links offering batches → courses → instructors) =====
            var offeringSections = new List<CourseOfferingSection>
            {
                new CourseOfferingSection { Id = 1, CourseOfferingId = offeringId, OfferingBatchId = 1, CourseId = 1, YearLevel = "1st Year", SectionName = "Sec-A", ProgramType = "Regular", InstructorId = "demo_instructor_id",  AssignedContactHours = 4 },
                new CourseOfferingSection { Id = 2, CourseOfferingId = offeringId, OfferingBatchId = 1, CourseId = 4, YearLevel = "1st Year", SectionName = "Sec-A", ProgramType = "Regular", InstructorId = "demo_instructor_id",  AssignedContactHours = 3 },
                new CourseOfferingSection { Id = 3, CourseOfferingId = offeringId, OfferingBatchId = 2, CourseId = 2, YearLevel = "2nd Year", SectionName = "Sec-A", ProgramType = "Regular", InstructorId = "demo_instructor_id",  AssignedContactHours = 4 }
            };
            foreach (var os in offeringSections)
                if (!await context.CourseOfferingSections.AnyAsync(x => x.Id == os.Id))
                    await context.CourseOfferingSections.AddAsync(os);
            await context.SaveChangesAsync();

            // ===== 13. ROOM REQUIREMENTS =====
            var requirements = new List<SectionRoomRequirement>
            {
                new SectionRoomRequirement { Id = 1, CourseOfferingSectionId = 1, RoomTypeId = 2 },
                new SectionRoomRequirement { Id = 2, CourseOfferingSectionId = 2, RoomTypeId = 4 },
                new SectionRoomRequirement { Id = 3, CourseOfferingSectionId = 3, RoomTypeId = 2 }
            };
            foreach (var rq in requirements)
                if (!await context.SectionRoomRequirements.AnyAsync(x => x.Id == rq.Id))
                    await context.SectionRoomRequirements.AddAsync(rq);
            await context.SaveChangesAsync();

            // ===== 14. SEMESTER ASSIGNMENTS =====
            // These must:
            //   - Match AcademicYear = "2024/2025" and Semester = "I" (exactly what solver queries)
            //   - Point to the SYSTEM sections (sec1, sec2) whose Batch names are parseable by the solver
            //   - Status = "Draft" (how real PO approval generates them)
            var assignments = new List<(int SectionId, int CourseId, string? InstructorId)>
            {
                (sec1.Id, 1, "demo_instructor_id"),   // Year 1 Sec1 → CS-101
                (sec1.Id, 4, "demo_instructor_id"),   // Year 1 Sec1 → MATH-101
                (sec2.Id, 2, "demo_instructor_id"),   // Year 2 Sec1 → CS-202
            };

            int assignId = 100;
            foreach (var (sectionId, courseId, instructorId) in assignments)
            {
                if (!await context.SemesterAssignments.AnyAsync(a =>
                    a.SectionId == sectionId && a.CourseId == courseId &&
                    a.AcademicYear == academicYear && a.Semester == semester))
                {
                    context.SemesterAssignments.Add(new SemesterCourseAssignment
                    {
                        Id           = assignId++,
                        AcademicYear = academicYear,
                        Semester     = semester,
                        SectionId    = sectionId,
                        CourseId     = courseId,
                        InstructorId = instructorId,
                        RoomId       = null,
                        Status       = "Draft"
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
