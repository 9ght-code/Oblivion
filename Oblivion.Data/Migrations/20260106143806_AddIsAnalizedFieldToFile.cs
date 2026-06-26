using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblivion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAnalizedFieldToFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnalized",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnalized",
                table: "Files");
        }
    }
}
