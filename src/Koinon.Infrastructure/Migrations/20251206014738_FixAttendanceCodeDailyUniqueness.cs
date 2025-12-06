using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Koinon.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FixAttendanceCodeDailyUniqueness : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "uix_attendance_code_issue_date_code",
            table: "attendance_code");

        migrationBuilder.AddColumn<DateOnly>(
            name: "issue_date",
            table: "attendance_code",
            type: "date",
            nullable: false,
            defaultValue: new DateOnly(1, 1, 1));

        // Backfill existing records with the correct date derived from issue_date_time
        migrationBuilder.Sql("UPDATE attendance_code SET issue_date = DATE(issue_date_time)");

        migrationBuilder.CreateIndex(
            name: "uix_attendance_code_issue_date_code",
            table: "attendance_code",
            columns: new[] { "issue_date", "code" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "uix_attendance_code_issue_date_code",
            table: "attendance_code");

        migrationBuilder.DropColumn(
            name: "issue_date",
            table: "attendance_code");

        migrationBuilder.CreateIndex(
            name: "uix_attendance_code_issue_date_code",
            table: "attendance_code",
            columns: new[] { "issue_date_time", "code" },
            unique: true);
    }
}
