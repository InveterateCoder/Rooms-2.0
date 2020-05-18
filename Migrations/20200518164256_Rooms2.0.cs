using Microsoft.EntityFrameworkCore.Migrations;

namespace Rooms.Migrations
{
    public partial class Rooms20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessIdsJson",
                table: "Messages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessIdsJson",
                table: "Messages",
                type: "longtext CHARACTER SET utf8mb4",
                maxLength: 5000,
                nullable: true);
        }
    }
}
