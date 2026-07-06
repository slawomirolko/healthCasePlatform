using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthCasePlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToRegulatoryCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "RegulatoryCases",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryCases_Country",
                table: "RegulatoryCases",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryCases_CreatedAt_Id",
                table: "RegulatoryCases",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryCases_Priority",
                table: "RegulatoryCases",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryCases_Status",
                table: "RegulatoryCases",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegulatoryCases_Country",
                table: "RegulatoryCases");

            migrationBuilder.DropIndex(
                name: "IX_RegulatoryCases_CreatedAt_Id",
                table: "RegulatoryCases");

            migrationBuilder.DropIndex(
                name: "IX_RegulatoryCases_Priority",
                table: "RegulatoryCases");

            migrationBuilder.DropIndex(
                name: "IX_RegulatoryCases_Status",
                table: "RegulatoryCases");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "RegulatoryCases");
        }
    }
}
