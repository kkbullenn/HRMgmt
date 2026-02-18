using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace HRMgmt.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftDateToShiftAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ShiftDate",
                table: "ShiftAssignments",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2026, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftDate",
                table: "ShiftAssignments");
        }
    }
}
