using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Asset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulatorDefectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulator_defects",
                schema: "maint",
                columns: table => new
                {
                    defect_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reported_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    system_affected = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    instructor_notes = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    resolved_by_engineer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_by_engineer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulator_defects", x => x.defect_id);
                    table.ForeignKey(
                        name: "fk_simulator_defects_simulators_simulator_id",
                        column: x => x.simulator_id,
                        principalSchema: "maint",
                        principalTable: "simulators",
                        principalColumn: "simulator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_defects_severity",
                schema: "maint",
                table: "simulator_defects",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "idx_defects_simulator_id",
                schema: "maint",
                table: "simulator_defects",
                column: "simulator_id");

            migrationBuilder.CreateIndex(
                name: "idx_defects_status",
                schema: "maint",
                table: "simulator_defects",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "simulator_defects",
                schema: "maint");
        }
    }
}
