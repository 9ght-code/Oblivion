using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblivion.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkspaceFilesCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyzedFileWorkspace");

            migrationBuilder.CreateTable(
                name: "WorkspaceFiles",
                columns: table => new
                {
                    FileID = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkspaceID = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceFiles", x => new { x.FileID, x.WorkspaceID });
                    table.ForeignKey(
                        name: "FK_WorkspaceFiles_Files_FileID",
                        column: x => x.FileID,
                        principalTable: "Files",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceFiles_Workspaces_WorkspaceID",
                        column: x => x.WorkspaceID,
                        principalTable: "Workspaces",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceFiles_WorkspaceID",
                table: "WorkspaceFiles",
                column: "WorkspaceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkspaceFiles");

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
    }
}
