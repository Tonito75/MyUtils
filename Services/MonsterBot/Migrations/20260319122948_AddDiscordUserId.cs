using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonsterBot.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscordUserId",
                table: "MonsterScans",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUserId",
                table: "MonsterScans");
        }
    }
}
