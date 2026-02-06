using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityAndBatchUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingId",
                table: "CourseOfferingBatches");

            migrationBuilder.AlterColumn<string>(
                name: "BatchName",
                table: "CourseOfferingBatches",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AvailabilitySlots",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "f5bda479-3e5f-405a-9a1f-cc902e1e2040");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "da976062-9d6e-4371-b215-a88c17b9e5d2");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "391eb11d-6511-4c84-a70a-58f212699951");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "eb5050f7-a18b-4dfb-a21e-3e169af812e9");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "21f756f0-1493-4b1f-9b61-e2b1912cf4da");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "9c9f3ebb-8b5c-4c3c-8efc-dd494e960515");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "AvailabilitySlots", "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { null, "3ff8ec5e-66f4-4c03-9db4-b12eb3c23a60", "AQAAAAIAAYagAAAAEG+Ye7XAe6JrBRnzlm1s7mC9kPgvJx4yHT2fpqNyAxAnkKLxtlJcCKNjVZZ/inNELQ==", "49e52a6e-9494-48ce-beff-408cf99c156b" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingId_YearLevel_BatchName",
                table: "CourseOfferingBatches",
                columns: new[] { "CourseOfferingId", "YearLevel", "BatchName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingId_YearLevel_BatchName",
                table: "CourseOfferingBatches");

            migrationBuilder.DropColumn(
                name: "AvailabilitySlots",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "BatchName",
                table: "CourseOfferingBatches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "78d2965f-9832-48b1-bcb3-05b9a6cc748d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "7486cff1-ec6f-4ad2-8dae-a891a8bcf430");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "a24057d9-b253-482e-b6c8-9cd650bbde99");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "e621a066-853c-4ed9-8f62-12f2685af0ed");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "c3f1306a-d05f-4230-9adb-6752bcf7bd8d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "79b8f4d9-6f78-461d-9ced-2b06e7ff9c84");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "19da40ee-0eef-4c1e-88f3-17d676a6e723", "AQAAAAIAAYagAAAAENTgbSGsMJRKPo6VJaa9qd600g4nwht5fvLSbq9fAEQArmc32PuwD+5wojeHcmTO7w==", "32691d8a-e21a-42a2-bd0b-7ee9392a18b7" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingId",
                table: "CourseOfferingBatches",
                column: "CourseOfferingId");
        }
    }
}
