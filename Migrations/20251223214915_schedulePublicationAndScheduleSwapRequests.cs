using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class schedulePublicationAndScheduleSwapRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleMeetingId = table.Column<int>(type: "int", nullable: false),
                    RequesterInstructorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    TargetSlotStart = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedPublicationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_AspNetUsers_RequesterInstructorId",
                        column: x => x.RequesterInstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleSwapRequests_ScheduleMeetings_ScheduleMeetingId",
                        column: x => x.ScheduleMeetingId,
                        principalTable: "ScheduleMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "24d3b517-6d13-4085-bea4-2b750c949102");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "5a91c52a-a5b6-41ab-b4eb-9af9dadae204");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "bac1e58e-2db0-4eef-a65a-2e715ea0930a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "5eff1c30-f472-4c60-8481-cdae8636c06b");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "6234103f-d3b6-4b0f-b653-ade6eb20e222");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "ea08dab1-9bac-4ea9-bd16-ad7e49f9f70f");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d7977ef4-1b98-4c3d-adcf-3065409cc1b8", "AQAAAAIAAYagAAAAEFOFCTIvWFMAgtWfCYsSkRoG0smifKG9+wfvAZb+HBnfT3wf6Sk+ByUw/MBR8qqYdw==", "125fb2af-d66c-467e-bb4d-8e1e806839f7" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_RequesterInstructorId",
                table: "ScheduleSwapRequests",
                column: "RequesterInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_ScheduleMeetingId",
                table: "ScheduleSwapRequests",
                column: "ScheduleMeetingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleSwapRequests");

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
        }
    }
}
