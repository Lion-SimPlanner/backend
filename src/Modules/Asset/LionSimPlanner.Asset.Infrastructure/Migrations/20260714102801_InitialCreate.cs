using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Asset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maint");

            migrationBuilder.CreateTable(
                name: "engineers",
                schema: "maint",
                columns: table => new
                {
                    engineer_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    clearance_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hardware_ratings = table.Column<string>(type: "jsonb", nullable: false),
                    shift_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    shift_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_on_call = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engineers", x => x.engineer_id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_checklists",
                schema: "maint",
                columns: table => new
                {
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engineer_id_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    engineer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    checklist_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_cleared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    signed_off_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    blocking_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_checklists", x => x.checklist_id);
                });

            migrationBuilder.CreateTable(
                name: "simulators",
                schema: "maint",
                columns: table => new
                {
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bay_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aircraft_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Ready"),
                    last_status_changed_by_engineer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_status_changed_by_engineer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_status_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulators", x => x.simulator_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_engineers_employee_code",
                schema: "maint",
                table: "engineers",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_checklist_simulator_date",
                schema: "maint",
                table: "maintenance_checklists",
                columns: new[] { "simulator_id", "checklist_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "engineers",
                schema: "maint");

            migrationBuilder.DropTable(
                name: "maintenance_checklists",
                schema: "maint");

            migrationBuilder.DropTable(
                name: "simulators",
                schema: "maint");
        }
    }
}
