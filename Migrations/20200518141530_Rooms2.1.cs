using Microsoft.EntityFrameworkCore.Migrations;

namespace Rooms.Migrations
{
    public partial class Rooms21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GUID",
                table: "Messages",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Messages",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GUID",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Messages");
        }
    }
}
