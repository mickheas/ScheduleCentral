using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfRegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelfRegistered",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

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
                columns: new[] { "ConcurrencyStamp", "IsSelfRegistered", "PasswordHash", "RegisteredAtUtc", "SecurityStamp" },
                values: new object[] { "b4c6b4bb-452e-4243-845c-2b0e5bfe3c9f", null, "AQAAAAIAAYagAAAAEFJENxhGm2Y85kqWrusZaY0FA/8TWigeh/6PSWZq8Epgs/ExiK3zQLySo6ga7p7TFw==", null, "3b578d98-ae6a-47ab-9991-3b31bdf1d915" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelfRegistered",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RegisteredAtUtc",
                table: "AspNetUsers");

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
        }
    }
}
