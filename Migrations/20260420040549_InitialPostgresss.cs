using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScheduleCentral.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "ab058b5c-b1c0-4362-8f2e-db929c50870d", "AQAAAAIAAYagAAAAEACspi9EhL/6nXbTE1TFzfeBVlcXzElu9DqgIqf557z9160ILA8AlF7GVqgieXfFfA==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b1f8e5d0-8b1e-45a7-86f2-8c9a6d0c73e0",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "dffb9fd2-0a31-485d-a1e7-4278972a292d", "AQAAAAIAAYagAAAAEO/Z0gngWR3mqspAtuMO3PBX23B6UstFq1WakFIhByHZpWw0fLSf1iCS+Lz32KeSwA==" });
        }
    }
}
