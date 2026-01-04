using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobBackgroundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "background_job_id",
                table: "import_job",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "import_job",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "background_job_id",
                table: "import_job");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "import_job");
        }
    }
}
