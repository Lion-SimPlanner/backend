using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Asset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceLifecycleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "checkout_time",
                schema: "maint",
                table: "engineers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "maintenance_logs",
                schema: "maint",
                columns: table => new
                {
                    maintenance_log_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    simulator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fault_description = table.Column<string>(type: "text", nullable: false),
                    resolution_description = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_logs", x => x.maintenance_log_id);
                    table.ForeignKey(
                        name: "FK_maintenance_logs_simulators_simulator_id",
                        column: x => x.simulator_id,
                        principalSchema: "maint",
                        principalTable: "simulators",
                        principalColumn: "simulator_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_logs_resolved_at",
                schema: "maint",
                table: "maintenance_logs",
                column: "resolved_at");

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_logs_simulator_id",
                schema: "maint",
                table: "maintenance_logs",
                column: "simulator_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_logs",
                schema: "maint");

            migrationBuilder.DropColumn(
                name: "checkout_time",
                schema: "maint",
                table: "engineers");
        }
    }
}
