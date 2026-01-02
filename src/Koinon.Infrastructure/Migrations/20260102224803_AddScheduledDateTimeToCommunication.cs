using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddScheduledDateTimeToCommunication : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "scheduled_date_time",
            table: "communication",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_communication_scheduled_date_time",
            table: "communication",
            column: "scheduled_date_time");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_communication_scheduled_date_time",
            table: "communication");

        migrationBuilder.DropColumn(
            name: "scheduled_date_time",
            table: "communication");
    }
}
