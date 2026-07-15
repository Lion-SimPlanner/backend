using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Scheduling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sched");

            migrationBuilder.CreateTable(
                name: "simulator_sessions",
                schema: "sched",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    captain_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_officer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    engineer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    syllabus_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_graded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    instructor_notes = table.Column<string>(type: "text", nullable: false),
                    grade_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    trainee_employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulator_sessions", x => x.session_id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sessions_aog_lookup",
                schema: "sched",
                table: "simulator_sessions",
                columns: new[] { "simulator_id", "status", "start_time" });

            migrationBuilder.CreateIndex(
                name: "idx_sessions_simulator_id",
                schema: "sched",
                table: "simulator_sessions",
                column: "simulator_id");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_start_time",
                schema: "sched",
                table: "simulator_sessions",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_status",
                schema: "sched",
                table: "simulator_sessions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "simulator_sessions",
                schema: "sched");
        }
    }
}
