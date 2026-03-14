using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScheduleCentral.Models;
using System.Reflection.Emit;

namespace ScheduleCentral.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -----------------------------------------------------------
            // --- CRITICAL: SEEDING ROLES AND ADMIN USER IN MIGRATION ---
            // -----------------------------------------------------------

            // 1. Seed Roles
            var roles = new List<IdentityRole>
            {
                new IdentityRole { Id = "34f669a9-3c3b-4c0d-a320-c1143f295621", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "a0e1f2b3-c4d5-6e7f-8a9b-0c1d2e3f4a5b" },
                new IdentityRole { Id = "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5", Name = "ProgramOfficer", NormalizedName = "PROGRAMOFFICER", ConcurrencyStamp = "b1f2a3c4-d5e6-7f8a-9b0c-1d2e3f4a5b6c" },
                new IdentityRole { Id = "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53", Name = "Instructor", NormalizedName = "INSTRUCTOR", ConcurrencyStamp = "c2a3b4d5-e6f7-8a9b-0c1d-2e3f4a5b6c7d" },
                new IdentityRole { Id = "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e", Name = "Student", NormalizedName = "STUDENT", ConcurrencyStamp = "d3b4c5e6-f7a8-9b0c-1d2e-3f4a5b6c7d8e" },
                new IdentityRole { Id = "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c", Name = "Department", NormalizedName = "DEPARTMENT", ConcurrencyStamp = "e4c5d6f7-a8b9-0c1d-2e3f-4a5b6c7d8e9f" },
                new IdentityRole { Id = "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9", Name = "TopManagement", NormalizedName = "TOPMANAGEMENT", ConcurrencyStamp = "f5d6e7a8-b9c0-1d2e-3f4a-5b6c7d8e9f0a" }
            };
            

            builder.Entity<IdentityRole>().HasData(roles);

            // 2. Seed Admin User
            var adminUserId = "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0";
            var adminRoleForeignKey = "34f669a9-3c3b-4c0d-a320-c1143f295621"; // Admin Role ID

            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "mikyasabebe76@gmail.com",
                NormalizedUserName = "MIKYASABEBE76@GMAIL.COM",
                Email = "mikyasabebe76@gmail.com",
                NormalizedEmail = "MIKYASABEBE76@GMAIL.COM",
                EmailConfirmed = true,
                FirstName = "Mikyas",
                LastName = "Abebe",
                TwoFactorEnabled = false,
                IsApproved = true,
                SecurityStamp = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            };

            // Create a PasswordHasher instance to generate a hash for "Admin!23"
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            // IMPORTANT: The hash must be generated using the correct class
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin!23");

            builder.Entity<ApplicationUser>().HasData(adminUser);

            // 3. Link Admin User to Admin Role
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = adminRoleForeignKey, // Admin Role ID
                    UserId = adminUserId
                }
            );
            // Ensure unique pairings if needed
            builder.Entity<InstructorCourse>()
            .HasIndex(ic => new { ic.InstructorId, ic.CourseId })
            .IsUnique();

            builder.Entity<CourseOfferingBatch>()
                .HasIndex(b => new { b.CourseOfferingId, b.YearLevel, b.BatchName })
                .IsUnique();

            builder.Entity<Course>()
                .HasIndex(c => c.Code)
                .IsUnique();

            builder.Entity<ScheduleGrid>()
                .HasIndex(g => g.SectionId)
                .IsUnique();

            builder.Entity<CourseOfferingSectionInstructor>()
                .HasIndex(x => new { x.CourseOfferingSectionId, x.InstructorId })
                .IsUnique();

            // -----------------------------------------------------------
            
            // Seed Initial Room Types
            builder.Entity<RoomType>().HasData(
                new RoomType { Id = 1, Name = "Lecture Hall", Description = "Additional seating" },
                new RoomType { Id = 2, Name = "Computer Lab", Description = "Workstations provided" },
                new RoomType { Id = 3, Name = "Design Studio", Description = "Drafting tables and open space" },
                new RoomType { Id = 4, Name = "Lecture Room", Description = "Standard tier seating" },
                new RoomType { Id = 5, Name = "Projector Room", Description = "Standard tier seating room with projector" }
            );

            builder.Entity<CourseOfferingBatch>()
                    .HasOne(b => b.CourseOffering)
                    .WithMany(co => co.Batches) // use the Batches collection on CourseOffering to match CourseOfferingBatch
                    .HasForeignKey(b => b.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Restrict);

                // If CourseOffering has Sections navigation, ensure the relationship is configured:
                builder.Entity<CourseOfferingSection>()
                    .HasOne(s => s.OfferingBatch)
                    .WithMany(b => b.Sections)
                    .HasForeignKey(s => s.OfferingBatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // If you also have CourseOffering.Sections relation, ensure delete behavior won't cause multiple cascade paths.
                // Example: make CourseOffering -> CourseOfferingSection Restrict (safer)
                builder.Entity<CourseOfferingSection>()
                    .HasOne(s => s.CourseOffering)
                    .WithMany(co => co.Sections)
                    .HasForeignKey(s => s.CourseOfferingId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.Entity<CourseOfferingSectionInstructor>()
                    .HasOne(x => x.CourseOfferingSection)
                    .WithMany(s => s.SectionInstructors)
                    .HasForeignKey(x => x.CourseOfferingSectionId)
                    .OnDelete(DeleteBehavior.Cascade);
        }

        // Add DbSet properties for your custom models here (e.g., Department, Course, etc.)
        // public DbSet<ScheduleCentral.Models.Department> Department { get; set; }
        // In Data/ApplicationDbContext.cs

        //public new DbSet<ApplicationUser> Users { get; set; } = null!;
        public DbSet<Department> Departments { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
        public DbSet<CourseOffering> CourseOfferings { get; set; }
        public DbSet<CourseOfferingBatch> CourseOfferingBatches { get; set; }
        public DbSet<CourseOfferingSection> CourseOfferingSections { get; set; }
        public DbSet<CourseOfferingSectionInstructor> CourseOfferingSectionInstructors { get; set; }
        public DbSet<CourseOfferingYearLevel> CourseOfferingYearLevels { get; set; }
        public DbSet<InstructorCourse> InstructorQualifications { get; set; }
        public DbSet<SemesterCourseAssignment> SemesterAssignments { get; set; }
        public DbSet<ScheduleGrid> ScheduleGrids { get; set; }
        public DbSet<SchedulePublication> SchedulePublications { get; set; }
        public DbSet<ScheduleMeeting> ScheduleMeetings { get; set; }
        public DbSet<ScheduleSwapRequest> ScheduleSwapRequests { get; set; }
        public DbSet<ScheduleChangeLog> ScheduleChangeLogs { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<ScheduleEmailSubscription> ScheduleEmailSubscriptions { get; set; }
        public DbSet<SectionRoomRequirement> SectionRoomRequirements { get; set; }
        public DbSet<YearLevel> YearLevels { get; set; }
        public DbSet<YearBatch> YearBatches { get; set; }
    }
}