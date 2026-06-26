using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblivion.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_Sha256",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Sha256",
                table: "Files",
                column: "Sha256",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Files_Sha256",
                table: "Files");

            migrationBuilder.CreateIndex(
                name: "IX_Files_Sha256",
                table: "Files",
                column: "Sha256");
        }
    }
}
