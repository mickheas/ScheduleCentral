using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class newPublicationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchedulePublicationId",
                table: "ScheduleMeetings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SchedulePublications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulePublications", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "6e548458-9df5-4720-8132-cd6f11afcb2e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "e2309fac-e0f0-43b0-ac21-0e702120c872");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "2c7e04a7-8644-4ba5-a965-7d33d8e55846");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "766326ae-c221-4c8d-a98d-42d3a98a293a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "9094ef38-5e03-469b-b37c-400f57002613");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "7b12c7a5-44e9-4c34-ae9d-41d0b914fc2d");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cb339cf1-3604-47bc-926d-e9b7f74420d3", "AQAAAAIAAYagAAAAEL9GNNuvZmZTBoBK07m9Y7qJ2r8CdUC+j3nmYMOcW+8AkWg7rsfcQ+gSSSMncL5D3w==", "087f7686-b82a-4036-a407-17b69f10ecc3" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMeetings_SchedulePublicationId",
                table: "ScheduleMeetings",
                column: "SchedulePublicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleMeetings_SchedulePublications_SchedulePublicationId",
                table: "ScheduleMeetings",
                column: "SchedulePublicationId",
                principalTable: "SchedulePublications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleMeetings_SchedulePublications_SchedulePublicationId",
                table: "ScheduleMeetings");

            migrationBuilder.DropTable(
                name: "SchedulePublications");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleMeetings_SchedulePublicationId",
                table: "ScheduleMeetings");

            migrationBuilder.DropColumn(
                name: "SchedulePublicationId",
                table: "ScheduleMeetings");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "141834db-cd73-4e32-bd5d-f9cb7077dc72");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "1df40913-da08-4739-ae2c-b2bc8f0d049c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "289d0fe8-1ced-440a-a5fc-5a6a892ad8ef");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "0ebf9172-1561-4c24-80c6-542246d1c739");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "0367ae5b-493a-4c67-9ad7-ae98e44c6172");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "660699c4-f189-4473-8aff-7db98dc7ee2a");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "02876fd0-47c6-4db0-8789-1451c9da9d40", "AQAAAAIAAYagAAAAEDEzki9aEyvlGRBFLqLFRt19KUOEskY/uqoXNT4SDyYR1OU6khPkb5mbBmq0EhTfyw==", "89e7111c-6846-408a-bfb3-1a73b5504774" });
        }
    }
}
