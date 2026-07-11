using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCasePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedLegalReviewerId",
                table: "RegulatoryCases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedScientificReviewerId",
                table: "RegulatoryCases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedLegalReviewerId",
                table: "RegulatoryCases");

            migrationBuilder.DropColumn(
                name: "AssignedScientificReviewerId",
                table: "RegulatoryCases");
        }
    }
}
