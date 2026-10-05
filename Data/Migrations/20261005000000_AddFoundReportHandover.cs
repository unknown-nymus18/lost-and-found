using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusLostAndFound.Data.Migrations
{
    public partial class AddFoundReportHandover : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HandoverMethod",
                schema: "campus_lost_found",
                table: "FoundReports",
                type: "text",
                nullable: false,
                defaultValue: "DropOff");

            migrationBuilder.AddColumn<string>(
                name: "DropOffLocation",
                schema: "campus_lost_found",
                table: "FoundReports",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                schema: "campus_lost_found",
                table: "FoundReports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HandoverMethod",
                schema: "campus_lost_found",
                table: "FoundReports");

            migrationBuilder.DropColumn(
                name: "DropOffLocation",
                schema: "campus_lost_found",
                table: "FoundReports");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                schema: "campus_lost_found",
                table: "FoundReports");
        }
    }
}
