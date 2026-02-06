using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionCountToCourseOfferingBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionCount",
                table: "CourseOfferingBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "5c35e0d3-7fba-4ac2-8d7f-6e9e76c06d92");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "b6732149-1f5a-4d9b-a677-f21787285006");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "efbd7d86-98c7-4daf-b67c-a1b69da027d8");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "c076978e-dfb5-4334-9f13-e25f6be9810b");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "7a5d4990-8ab8-4947-ba2d-ff9dc954f791");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "a6764438-3bb5-4471-a5a5-7323dda1fd2c");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "89616e81-37f9-4722-a17b-21053d1978d4", "AQAAAAIAAYagAAAAEJbHTxY+jvJiD3gwSE7S7Jg0N/VJynknrU7SbGzDmnDXekpsJWN39jFDIoDQKYfbiw==", "50df008d-c2bc-4ffb-9f74-36c0bc83d3aa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SectionCount",
                table: "CourseOfferingBatches");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "433c5b37-e616-466e-9fcd-fd6d8d55d3e2");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "3a478ee4-cfe9-4cf3-8c93-ccb91a494b3d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "9f5c0119-4e52-4329-9968-fc8cad29210c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "25e4902f-57d4-4be8-9bcf-7f34d6ff9940");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "ec113259-89d2-44dd-ae5e-55630ac23222");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "137adb78-7a79-4a6e-95a4-cd8f76270dad");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d797679c-6735-4323-b710-d086a6fd947d", "AQAAAAIAAYagAAAAEN/KE/y9bCZ3Y1Y+DHFd/6FvPPJ7Muh8rqyy4m/ezROppQbEat36BBMp1sgLP6pnmg==", "4e5b0748-7bed-4c6e-b5a1-ae37143526ac" });
        }
    }
}
