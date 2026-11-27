using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetForge.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    StyleGuide_Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    StyleGuide_Perspective = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StyleGuide_Palette = table.Column<string>(type: "TEXT", nullable: false),
                    StyleGuide_Lighting = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StyleGuide_Outline = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StyleGuide_DetailLevel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    StyleGuide_Avoid = table.Column<string>(type: "TEXT", nullable: false),
                    StyleGuide_UpdatedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_NormalizedName",
                table: "Projects",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Slug",
                table: "Projects",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
