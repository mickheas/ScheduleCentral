using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleSwapRequestWorkflowV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TargetSlotStart",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TargetDayOfWeek",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalReviewedAtUtc",
                table: "ScheduleSwapRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalReviewerUserId",
                table: "ScheduleSwapRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InitialReviewedAtUtc",
                table: "ScheduleSwapRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialReviewerUserId",
                table: "ScheduleSwapRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeerDecision",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PeerInstructorId",
                table: "ScheduleSwapRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeerRespondedAtUtc",
                table: "ScheduleSwapRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeerScheduleMeetingId",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "e1b52003-137c-4431-876b-39a200d94d23");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "909d185e-efa0-47eb-9ff9-ce09ecaa8774");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "3e24571a-ac54-4cee-91e0-a4634e9781f5");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "00da8988-df63-4606-a96b-31368a67cfad");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "6a229cde-bc0f-4881-a912-cdb65d402ff5");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "349a34d2-ef44-462f-993b-c3c672b5e7f0");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7fac7b80-4ece-4cbc-bae5-c79d399112a7", "AQAAAAIAAYagAAAAEH+PV1Aodv8ntv8YTytZZqHVIAVmNED45RKQGtQV+QtmS2POLDtu9H/ot6WNU42V0A==", "b85eb6ba-4b6e-48c3-b53e-0555189bd75a" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_PeerInstructorId",
                table: "ScheduleSwapRequests",
                column: "PeerInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSwapRequests_PeerScheduleMeetingId",
                table: "ScheduleSwapRequests",
                column: "PeerScheduleMeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSwapRequests_AspNetUsers_PeerInstructorId",
                table: "ScheduleSwapRequests",
                column: "PeerInstructorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleSwapRequests_ScheduleMeetings_PeerScheduleMeetingId",
                table: "ScheduleSwapRequests",
                column: "PeerScheduleMeetingId",
                principalTable: "ScheduleMeetings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSwapRequests_AspNetUsers_PeerInstructorId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleSwapRequests_ScheduleMeetings_PeerScheduleMeetingId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSwapRequests_PeerInstructorId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleSwapRequests_PeerScheduleMeetingId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "FinalReviewedAtUtc",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "FinalReviewerUserId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "InitialReviewedAtUtc",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "InitialReviewerUserId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "PeerDecision",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "PeerInstructorId",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "PeerRespondedAtUtc",
                table: "ScheduleSwapRequests");

            migrationBuilder.DropColumn(
                name: "PeerScheduleMeetingId",
                table: "ScheduleSwapRequests");

            migrationBuilder.AlterColumn<int>(
                name: "TargetSlotStart",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TargetDayOfWeek",
                table: "ScheduleSwapRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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
        }
    }
}
