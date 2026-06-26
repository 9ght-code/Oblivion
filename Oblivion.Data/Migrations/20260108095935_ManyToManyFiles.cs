using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblivion.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Workspaces_WorkspaceID",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_WorkspaceID",
                table: "Files");

            migrationBuilder.CreateTable(
                name: "AnalyzedFileWorkspace",
                columns: table => new
                {
                    FilesID = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspacesID = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzedFileWorkspace", x => new { x.FilesID, x.WorkspacesID });
                    table.ForeignKey(
                        name: "FK_AnalyzedFileWorkspace_Files_FilesID",
                        column: x => x.FilesID,
                        principalTable: "Files",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalyzedFileWorkspace_Workspaces_WorkspacesID",
                        column: x => x.WorkspacesID,
                        principalTable: "Workspaces",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzedFileWorkspace_WorkspacesID",
                table: "AnalyzedFileWorkspace",
                column: "WorkspacesID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzedFileWorkspace");

            migrationBuilder.CreateIndex(
                name: "IX_Files_WorkspaceID",
                table: "Files",
                column: "WorkspaceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Workspaces_WorkspaceID",
                table: "Files",
                column: "WorkspaceID",
                principalTable: "Workspaces",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
