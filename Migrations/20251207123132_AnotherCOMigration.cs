using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class AnotherCOMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingBatches_CourseOfferingYearLevels_YearLevelId",
                table: "CourseOfferingBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingSections_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingSections");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingYearLevels_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingYearLevels");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferingBatches_YearLevelId",
                table: "CourseOfferingBatches");

            migrationBuilder.DropColumn(
                name: "ProgramType",
                table: "CourseOfferingBatches");

            migrationBuilder.RenameColumn(
                name: "YearLevelId",
                table: "CourseOfferingBatches",
                newName: "YearLevel");

            migrationBuilder.AlterColumn<int>(
                name: "OfferingBatchId",
                table: "CourseOfferingSections",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CourseOfferingId",
                table: "CourseOfferingSections",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CourseOfferingYearLevelId",
                table: "CourseOfferingBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CourseOfferingBatches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "aef4886d-2f5b-44b2-bce5-336f236fa5d8");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "8702657a-043c-43fa-8eb4-cb21f41922a4");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "d01e9cf5-92e6-41bb-8f30-d7d95aaa069a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "52dddfdf-c5b2-44e9-b9b1-8853b886ee95");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "ad0397ae-a41a-405d-b99a-55238219e4f5");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "24abe397-a71a-405b-b183-92e1da612883");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4e669cf1-effe-48e8-bfe2-3d66d81f7852", "AQAAAAIAAYagAAAAEIMO1gVZ/j2EB+5uEO/yUkfxw2fv9nVcSfq6tj0xtsz5DiqH3U7iPxj/48wfqdBK4w==", "624f8d85-ff28-4727-afb1-f78cce62910f" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingYearLevelId",
                table: "CourseOfferingBatches",
                column: "CourseOfferingYearLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingBatches_CourseOfferingYearLevels_CourseOfferingYearLevelId",
                table: "CourseOfferingBatches",
                column: "CourseOfferingYearLevelId",
                principalTable: "CourseOfferingYearLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingSections_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingSections",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingYearLevels_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingYearLevels",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingBatches_CourseOfferingYearLevels_CourseOfferingYearLevelId",
                table: "CourseOfferingBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingSections_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingSections");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseOfferingYearLevels_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingYearLevels");

            migrationBuilder.DropIndex(
                name: "IX_CourseOfferingBatches_CourseOfferingYearLevelId",
                table: "CourseOfferingBatches");

            migrationBuilder.DropColumn(
                name: "CourseOfferingYearLevelId",
                table: "CourseOfferingBatches");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CourseOfferingBatches");

            migrationBuilder.RenameColumn(
                name: "YearLevel",
                table: "CourseOfferingBatches",
                newName: "YearLevelId");

            migrationBuilder.AlterColumn<int>(
                name: "OfferingBatchId",
                table: "CourseOfferingSections",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CourseOfferingId",
                table: "CourseOfferingSections",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramType",
                table: "CourseOfferingBatches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2a8e9d3c-5f7a-4b6d-9c1e-0d2f3a4e5b6c",
                column: "ConcurrencyStamp",
                value: "364a13ca-24bf-4fec-b899-71725e72e627");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "34f669a9-3c3b-4c0d-a320-c1143f295621",
                column: "ConcurrencyStamp",
                value: "369bb57c-183a-488e-a933-4404604aff56");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5798b3f2-1d5d-4f81-a75d-6c1b3f9d44e5",
                column: "ConcurrencyStamp",
                value: "4b044384-2412-44ec-b1f9-b0327cf3db73");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "62a4c8e7-6b4e-4f3a-a5c1-8b0d2e4f7c9e",
                column: "ConcurrencyStamp",
                value: "c8407fcc-1731-4413-a3a4-3afebb662973");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "98c2d1b8-2a2b-4d4b-9e0a-7c9d1a8f6d53",
                column: "ConcurrencyStamp",
                value: "3f27d3a9-3c38-422d-9f51-981c5e942c0a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d1b5f2c4-8g9h-4i0j-k2l3-m4n5o6p7q8r9",
                column: "ConcurrencyStamp",
                value: "c3f193c2-2d26-4e7a-811a-d186485b5e2b");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "51caa770-fd36-4fed-bf8e-5df4904d08f9", "AQAAAAIAAYagAAAAEAOs1hMNzf/RMV9VQWNsEcrEbWqyQsQ8aHKeVulbqy2AotJSmrPLVOegxXBddCDZfw==", "17558c69-b50d-4593-8190-0d91fb200e69" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingBatches_YearLevelId",
                table: "CourseOfferingBatches",
                column: "YearLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingBatches_CourseOfferingYearLevels_YearLevelId",
                table: "CourseOfferingBatches",
                column: "YearLevelId",
                principalTable: "CourseOfferingYearLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingSections_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingSections",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseOfferingYearLevels_CourseOfferings_CourseOfferingId",
                table: "CourseOfferingYearLevels",
                column: "CourseOfferingId",
                principalTable: "CourseOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
