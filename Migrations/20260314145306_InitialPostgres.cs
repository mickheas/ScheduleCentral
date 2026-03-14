using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSelfRegistered = table.Column<bool>(type: "boolean", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    AvailableHours = table.Column<int>(type: "integer", nullable: false),
                    AvailabilitySlots = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Department = table.Column<string>(type: "text", nullable: false),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "text", nullable: true),
                    PublicationId = table.Column<int>(type: "integer", nullable: false),
                    ScheduleMeetingId = table.Column<int>(type: "integer", nullable: false),
                    ScheduleGridId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: true),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    InstructorId = table.Column<string>(type: "text", nullable: true),
                    OldDayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    OldSlotStart = table.Column<int>(type: "integer", nullable: false),
                    NewDayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    NewSlotStart = table.Column<int>(type: "integer", nullable: false),
                    ChangeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEmailSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UnsubscribeToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEmailSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulePublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "text", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedByUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    HeadId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_HeadId",
                        column: x => x.HeadId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferingYearLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseOfferingId = table.Column<int>(type: "integer", nullable: false),
                    YearLevel = table.Column<int>(type: "integer", nullable: false),
                    ProgramType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingYearLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingYearLevels_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Batches_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreditHours = table.Column<int>(type: "integer", nullable: false),
                    LectureHours = table.Column<int>(type: "integer", nullable: false),
                    LabHours = table.Column<int>(type: "integer", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "YearLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YearLevels_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferingBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseOfferingId = table.Column<int>(type: "integer", nullable: false),
                    YearLevel = table.Column<int>(type: "integer", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    IsExtension = table.Column<bool>(type: "boolean", nullable: false),
                    SectionCount = table.Column<int>(type: "integer", nullable: false),
                    BatchName = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CourseOfferingYearLevelId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingBatches_CourseOfferingYearLevels_CourseOfferi~",
                        column: x => x.CourseOfferingYearLevelId,
                        principalTable: "CourseOfferingYearLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseOfferingBatches_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NumberOfStudents = table.Column<int>(type: "integer", nullable: false),
                    IsExtension = table.Column<bool>(type: "boolean", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sections_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstructorQualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstructorId = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorQualifications_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstructorQualifications_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YearBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    YearLevelId = table.Column<int>(type: "integer", nullable: false),
                    CourseOfferingId = table.Column<int>(type: "integer", nullable: true),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    ProgramType = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YearBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YearBatches_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_YearBatches_YearLevels_YearLevelId",
                        column: x => x.YearLevelId,
                        principalTable: "YearLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferingSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseOfferingId = table.Column<int>(type: "integer", nullable: true),
                    OfferingBatchId = table.Column<int>(type: "integer", nullable: true),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    YearLevel = table.Column<string>(type: "text", nullable: false),
                    SectionName = table.Column<string>(type: "text", nullable: false),
                    ProgramType = table.Column<string>(type: "text", nullable: false),
                    InstructorId = table.Column<string>(type: "text", nullable: true),
                    AssignedContactHours = table.Column<int>(type: "integer", nullable: false),
                    IsFullDay = table.Column<bool>(type: "boolean", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    Period = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSections_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseOfferingSections_CourseOfferingBatches_OfferingBatchId",
                        column: x => x.OfferingBatchId,
                        principalTable: "CourseOfferingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSections_CourseOfferings_CourseOfferingId",
                        column: x => x.CourseOfferingId,
                        principalTable: "CourseOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSections_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSections_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleGrids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleGrids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleGrids_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SemesterAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<string>(type: "text", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SemesterAssignments_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemesterAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SemesterAssignments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SemesterAssignments_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferingSectionInstructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseOfferingSectionId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferingSectionInstructors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSectionInstructors_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseOfferingSectionInstructors_CourseOfferingSections_Cou~",
                        column: x => x.CourseOfferingSectionId,
                        principalTable: "CourseOfferingSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionRoomRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseOfferingSectionId = table.Column<int>(type: "integer", nullable: false),
                    RoomTypeId = table.Column<int>(type: "integer", nullable: false),
                    HoursPerWeek = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionRoomRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionRoomRequirements_CourseOfferingSections_CourseOfferi~",
                        column: x => x.CourseOfferingSectionId,
                        principalTable: "CourseOfferingSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectionRoomRequirements_RoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "RoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SchedulePublicationId = table.Column<int>(type: "integer", nullable: true),
                    ScheduleGridId = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<string>(type: "text", nullable: true),
                    RoomId = table.Column<int>(type: "integer", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    SlotStart = table.Column<int>(type: "integer", nullable: false),
                    SlotLength = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleMeetings_AspNetUsers_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduleMeetings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleMeetings_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduleMeetings_ScheduleGrids_ScheduleGridId",
                        column: x => x.ScheduleGridId,
                        principalTable: "ScheduleGrids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleMeetings_SchedulePublications_SchedulePublicationId",
                        column: x => x.SchedulePublicationId,
                        principalTable: "SchedulePublications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleMeetingId = table.Column<int>(type: "integer", nullable: false),
                    RequesterInstructorId = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    PeerInstructorId = table.Column<string>(type: "text", nullable: true),
                    PeerScheduleMeetingId = table.Column<int>(type: "integer", nullable: true),
                    TargetDayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    TargetSlotStart = table.Column<int>(type: "integer", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InitialReviewerUserId = table.Column<string>(type: "text", nullable: true),
                    InitialReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeerRespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeerDecision = table.Column<int>(type: "integer", nullable: false),
                    FinalReviewerUserId = table.Column<string>(type: "text", nullable: true),
                    FinalReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerUserId = table.Column<string>(type: "text", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    AppliedPublicationId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_AspNetUsers_PeerInstructorId",
                        column: x => x.PeerInstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_AspNetUsers_RequesterInstructorId",
                        column: x => x.RequesterInstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_ScheduleMeetings_PeerScheduleMeetingId",
                        column: x => x.PeerScheduleMeetingId,
                        principalTable: "ScheduleMeetings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_ScheduleMeetings_ScheduleMeetingId",
                        column: x => x.ScheduleMeetingId,
                        principalTable: "ScheduleMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c", "e3c277ce-ae45-4ffd-96ad-aeb4b45008d7", "Department", "DEPARTMENT" },
                    { "34f669a9-3c3b-4c0d-a320-c1143f295621", "a0807117-46e5-4872-90f8-4ee1306d9d00", "Admin", "ADMIN" },
                    { "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5", "0f93355b-2032-46da-9e36-8182aa140ced", "ProgramOfficer", "PROGRAMOFFICER" },
                    { "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e", "6bcaa411-8c6a-4f92-8415-82c031aba9c8", "Student", "STUDENT" },
                    { "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53", "6db258ba-6d18-4e4a-9a46-b8e094c7f506", "Instructor", "INSTRUCTOR" },
                    { "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9", "af1c8d22-6ebb-4dde-96f8-93fd1ccac727", "TopManagement", "TOPMANAGEMENT" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "AvailabilitySlots", "AvailableHours", "ConcurrencyStamp", "Department", "Email", "EmailConfirmed", "FirstName", "IsApproved", "IsSelfRegistered", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "RegisteredAtUtc", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0", 0, null, 12, "1c13d21a-236c-4ef1-8a0a-967f548bf108", null, "mikyasabebe76@gmail.com", true, "Mikyas", true, null, "Abebe", false, null, "MIKYASABEBE76@GMAIL.COM", "MIKYASABEBE76@GMAIL.COM", "AQAAAAIAAYagAAAAENDQIPRgKERCYsXCSBvEG/UVCfekTDCIJZ/cgTPO0vcSCD6dXtednNFJcfZLzLCbxw==", null, false, null, "11b221bf-aa5e-4b55-afc6-7979af1b88bc", true, "mikyasabebe76@gmail.com" });

            migrationBuilder.InsertData(
                table: "RoomTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Additional seating", "Lecture Hall" },
                    { 2, "Workstations provided", "Computer Lab" },
                    { 3, "Drafting tables and open space", "Design Studio" },
                    { 4, "Standard tier seating", "Lecture Room" },
                    { 5, "Standard tier seating room with projector", "Projector Room" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "34f669a9-3c3b-4c0d-a320-c1143f295621", "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batches_DepartmentId",
                table: "Batches",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingId_YearLevel_BatchName",
                table: "CourseOfferingBatches",
                columns: new[] { "CourseOfferingId", "YearLevel", "BatchName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingYearLevelId",
                table: "CourseOfferingBatches",
                column: "CourseOfferingYearLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSectionInstructors_CourseOfferingSectionId_In~",
                table: "CourseOfferingSectionInstructors",
                columns: new[] { "CourseOfferingSectionId", "InstructorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSectionInstructors_InstructorId",
                table: "CourseOfferingSectionInstructors",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSections_CourseId",
                table: "CourseOfferingSections",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSections_CourseOfferingId",
                table: "CourseOfferingSections",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSections_InstructorId",
                table: "CourseOfferingSections",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSections_OfferingBatchId",
                table: "CourseOfferingSections",
                column: "OfferingBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSections_RoomId",
                table: "CourseOfferingSections",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingYearLevels_CourseOfferingId",
                table: "CourseOfferingYearLevels",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HeadId",
                table: "Departments",
                column: "HeadId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorQualifications_CourseId",
                table: "InstructorQualifications",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorQualifications_InstructorId_CourseId",
                table: "InstructorQualifications",
                columns: new[] { "InstructorId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeId",
                table: "Rooms",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGrids_SectionId",
                table: "ScheduleGrids",
                column: "SectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_CourseId",
                table: "ScheduleMeetings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_InstructorId",
                table: "ScheduleMeetings",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_RoomId",
                table: "ScheduleMeetings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_ScheduleGridId",
                table: "ScheduleMeetings",
                column: "ScheduleGridId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_SchedulePublicationId",
                table: "ScheduleMeetings",
                column: "SchedulePublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_PeerInstructorId",
                table: "ScheduleSwapRequests",
                column: "PeerInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_PeerScheduleMeetingId",
                table: "ScheduleSwapRequests",
                column: "PeerScheduleMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_RequesterInstructorId",
                table: "ScheduleSwapRequests",
                column: "RequesterInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_ScheduleMeetingId",
                table: "ScheduleSwapRequests",
                column: "ScheduleMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionRoomRequirements_CourseOfferingSectionId",
                table: "SectionRoomRequirements",
                column: "CourseOfferingSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionRoomRequirements_RoomTypeId",
                table: "SectionRoomRequirements",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_BatchId",
                table: "Sections",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterAssignments_CourseId",
                table: "SemesterAssignments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterAssignments_InstructorId",
                table: "SemesterAssignments",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterAssignments_RoomId",
                table: "SemesterAssignments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterAssignments_SectionId",
                table: "SemesterAssignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_YearBatches_CourseOfferingId",
                table: "YearBatches",
                column: "CourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_YearBatches_YearLevelId",
                table: "YearBatches",
                column: "YearLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_YearLevels_DepartmentId",
                table: "YearLevels",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicPeriods");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CourseOfferingSectionInstructors");

            migrationBuilder.DropTable(
                name: "InstructorQualifications");

            migrationBuilder.DropTable(
                name: "ScheduleChangeLogs");

            migrationBuilder.DropTable(
                name: "ScheduleEmailSubscriptions");

            migrationBuilder.DropTable(
                name: "ScheduleSwapRequests");

            migrationBuilder.DropTable(
                name: "SectionRoomRequirements");

            migrationBuilder.DropTable(
                name: "SemesterAssignments");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "YearBatches");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ScheduleMeetings");

            migrationBuilder.DropTable(
                name: "CourseOfferingSections");

            migrationBuilder.DropTable(
                name: "YearLevels");

            migrationBuilder.DropTable(
                name: "ScheduleGrids");

            migrationBuilder.DropTable(
                name: "SchedulePublications");

            migrationBuilder.DropTable(
                name: "CourseOfferingBatches");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Sections");

            migrationBuilder.DropTable(
                name: "CourseOfferingYearLevels");

            migrationBuilder.DropTable(
                name: "RoomTypes");

            migrationBuilder.DropTable(
                name: "Batches");

            migrationBuilder.DropTable(
                name: "CourseOfferings");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
