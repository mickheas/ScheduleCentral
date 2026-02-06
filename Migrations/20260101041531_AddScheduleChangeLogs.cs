using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleChangeLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicationId = table.Column<int>(type: "int", nullable: false),
                    ScheduleMeetingId = table.Column<int>(type: "int", nullable: false),
                    ScheduleGridId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    InstructorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    OldSlotStart = table.Column<int>(type: "int", nullable: false),
                    NewDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    NewSlotStart = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleChangeLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "5566736f-f51d-475f-a29b-99cfa8d6da61");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "1a6b6f20-1503-4328-82ac-a33674fe5a49");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "ec765baf-2eb0-4f49-b2e1-ee0fd15c7e6b");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "97fe18df-47a2-4eb2-97b3-5db8a31c658c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "7245cfd1-4ab7-439e-864c-b844bcf2f47d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "9b908951-8976-4b32-8f18-849817485ca9");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a19c50d6-1d5c-4e10-8a76-c451e3009104", "AQAAAAIAAYagAAAAELvr5ym31COfjWKbUOc9Yq8K16Oqxap77o8L4OW97TpS2Gox/jGFbEW9W6pIWdNSJw==", "dfae6780-429d-4cbd-b49b-517616f6aa37" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleChangeLogs");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "119ff3f0-de3f-4adc-8400-eb7ff4e118f7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "8c282101-499c-47c5-acef-3ca7eec41970");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "e8fad9f0-acd8-420b-a688-773f5c97e8f5");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "74e5ec52-900e-4028-8aaa-9c27ada4e878");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "600546b9-99ce-4cfa-9364-e0cf5329982f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "7063d109-0e78-4d3c-ad0d-709cf909a3f0");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b4c6b4bb-452e-4243-845c-2b0e5bfe3c9f", "AQAAAAIAAYagAAAAEFJENxhGm2Y85kqWrusZaY0FA/8TWigeh/6PSWZq8Epgs/ExiK3zQLySo6ga7p7TFw==", "3b578d98-ae6a-47ab-9991-3b31bdf1d915" });
        }
    }
}
