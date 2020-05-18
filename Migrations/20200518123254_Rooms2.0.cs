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

            migrationBuilder.DropColumn(
                name: "GUID",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "UserId",
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

            migrationBuilder.AddColumn<string>(
                name: "GUID",
                table: "Messages",
                type: "varchar(40) CHARACTER SET utf8mb4",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Messages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
