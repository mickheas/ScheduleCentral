using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleGridEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleGrids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "ScheduleMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleGridId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    SlotStart = table.Column<int>(type: "int", nullable: false),
                    SlotLength = table.Column<int>(type: "int", nullable: false)
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
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "dd48d037-9d60-4c81-b009-b1c4697e1037");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "334a9ee0-6bc0-4827-aa3f-d822ed2df9d6");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "ab1170d9-3362-4726-95a9-605c3a636d51");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "567f7638-5a7d-426f-94f5-1350833f15d3");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "39d9a482-494f-4b55-849d-ff10e72169a1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "ff9c7534-8e40-4917-96b4-2fce4a613ed3");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c8a67553-4df4-4303-a5b2-24d6de144a84", "AQAAAAIAAYagAAAAEF/PWhRc9rr5grZrbAVhlIfaS3Pant3Y2xza3rFrMR3VOiaZdI0m2NCfaF71hTJbVA==", "6bd18916-ab97-4ce7-8b36-8932309a04e8" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleMeetings");

            migrationBuilder.DropTable(
                name: "ScheduleGrids");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "fd2280c4-99e0-46af-8976-adb1665aa644");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "438cdd23-2bc0-4e1c-9f70-738bca0535ae");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "05bb2817-f617-4a7f-bc4c-6d83ccebaff1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "2b966733-b755-453c-873c-ac3b05bbeb6c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "aadf5c75-aa04-4f76-86c3-90e626f05ea8");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "12c3134f-62ca-412d-aba5-7dab6ea75092");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3e92934a-6122-4ff5-ba29-a4723f9b7326", "AQAAAAIAAYagAAAAEBL25rx7BaPL+OCOkOsCLAjajbtX5jCnoCLtzOX4/g94nY4kuhEy8JbEKPR+vNO0uw==", "d38b3374-91d8-4bc4-ae5c-cae73e9b07b1" });
        }
    }
}
