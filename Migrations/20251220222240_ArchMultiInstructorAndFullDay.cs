using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class ArchMultiInstructorAndFullDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullDay",
                table: "CourseOfferingSections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CourseOfferingSectionInstructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseOfferingSectionId = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<string>(type: "nvarchar(450)", nullable: false)
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
                        name: "FK_CourseOfferingSectionInstructors_CourseOfferingSections_CourseOfferingSectionId",
                        column: x => x.CourseOfferingSectionId,
                        principalTable: "CourseOfferingSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSectionInstructors_CourseOfferingSectionId_InstructorId",
                table: "CourseOfferingSectionInstructors",
                columns: new[] { "CourseOfferingSectionId", "InstructorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseOfferingSectionInstructors_InstructorId",
                table: "CourseOfferingSectionInstructors",
                column: "InstructorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseOfferingSectionInstructors");

            migrationBuilder.DropColumn(
                name: "IsFullDay",
                table: "CourseOfferingSections");

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
    }
}
