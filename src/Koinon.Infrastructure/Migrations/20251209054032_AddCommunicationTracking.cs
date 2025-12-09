using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCommunicationTracking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "OpenCount",
            table: "communication_recipient",
            newName: "open_count");

        migrationBuilder.RenameColumn(
            name: "ClickedDateTime",
            table: "communication_recipient",
            newName: "clicked_date_time");

        migrationBuilder.RenameColumn(
            name: "ClickCount",
            table: "communication_recipient",
            newName: "click_count");

        migrationBuilder.RenameColumn(
            name: "ClickedCount",
            table: "communication",
            newName: "clicked_count");

        migrationBuilder.AlterColumn<int>(
            name: "open_count",
            table: "communication_recipient",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<int>(
            name: "click_count",
            table: "communication_recipient",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<int>(
            name: "clicked_count",
            table: "communication",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "open_count",
            table: "communication_recipient",
            newName: "OpenCount");

        migrationBuilder.RenameColumn(
            name: "clicked_date_time",
            table: "communication_recipient",
            newName: "ClickedDateTime");

        migrationBuilder.RenameColumn(
            name: "click_count",
            table: "communication_recipient",
            newName: "ClickCount");

        migrationBuilder.RenameColumn(
            name: "clicked_count",
            table: "communication",
            newName: "ClickedCount");

        migrationBuilder.AlterColumn<int>(
            name: "OpenCount",
            table: "communication_recipient",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);

        migrationBuilder.AlterColumn<int>(
            name: "ClickCount",
            table: "communication_recipient",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);

        migrationBuilder.AlterColumn<int>(
            name: "ClickedCount",
            table: "communication",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);
    }
}
