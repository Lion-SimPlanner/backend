using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Scheduling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEarlyTerminationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "original_end_time",
                schema: "sched",
                table: "simulator_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "termination_reason",
                schema: "sched",
                table: "simulator_sessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_end_time",
                schema: "sched",
                table: "simulator_sessions");

            migrationBuilder.DropColumn(
                name: "termination_reason",
                schema: "sched",
                table: "simulator_sessions");
        }
    }
}
