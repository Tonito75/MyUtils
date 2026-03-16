using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonsterBot.Migrations
{
    /// <inheritdoc />
    public partial class RenameCouleurToNom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Couleur",
                table: "MonsterScans");

            migrationBuilder.AddColumn<string>(
                name: "Nom",
                table: "MonsterScans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nom",
                table: "MonsterScans");

            migrationBuilder.AddColumn<string>(
                name: "Couleur",
                table: "MonsterScans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
