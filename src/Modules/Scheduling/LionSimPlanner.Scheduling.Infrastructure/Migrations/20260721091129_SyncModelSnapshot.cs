using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Scheduling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_simulator_sessions",
                schema: "sched",
                table: "simulator_sessions");

            migrationBuilder.AddPrimaryKey(
                name: "pk_simulator_sessions",
                schema: "sched",
                table: "simulator_sessions",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_simulator_sessions",
                schema: "sched",
                table: "simulator_sessions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_simulator_sessions",
                schema: "sched",
                table: "simulator_sessions",
                column: "session_id");
        }
    }
}
