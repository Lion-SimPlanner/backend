using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Asset.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maintenance_logs_simulators_simulator_id",
                schema: "maint",
                table: "maintenance_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_simulators",
                schema: "maint",
                table: "simulators");

            migrationBuilder.DropPrimaryKey(
                name: "PK_maintenance_logs",
                schema: "maint",
                table: "maintenance_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_maintenance_checklists",
                schema: "maint",
                table: "maintenance_checklists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_engineers",
                schema: "maint",
                table: "engineers");

            migrationBuilder.RenameIndex(
                name: "IX_engineers_employee_code",
                schema: "maint",
                table: "engineers",
                newName: "ix_engineers_employee_code");

            migrationBuilder.AddPrimaryKey(
                name: "pk_simulators",
                schema: "maint",
                table: "simulators",
                column: "simulator_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_maintenance_logs",
                schema: "maint",
                table: "maintenance_logs",
                column: "maintenance_log_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_maintenance_checklists",
                schema: "maint",
                table: "maintenance_checklists",
                column: "checklist_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_engineers",
                schema: "maint",
                table: "engineers",
                column: "engineer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_maintenance_logs_simulators_simulator_id",
                schema: "maint",
                table: "maintenance_logs",
                column: "simulator_id",
                principalSchema: "maint",
                principalTable: "simulators",
                principalColumn: "simulator_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_maintenance_logs_simulators_simulator_id",
                schema: "maint",
                table: "maintenance_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_simulators",
                schema: "maint",
                table: "simulators");

            migrationBuilder.DropPrimaryKey(
                name: "pk_maintenance_logs",
                schema: "maint",
                table: "maintenance_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_maintenance_checklists",
                schema: "maint",
                table: "maintenance_checklists");

            migrationBuilder.DropPrimaryKey(
                name: "pk_engineers",
                schema: "maint",
                table: "engineers");

            migrationBuilder.RenameIndex(
                name: "ix_engineers_employee_code",
                schema: "maint",
                table: "engineers",
                newName: "IX_engineers_employee_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_simulators",
                schema: "maint",
                table: "simulators",
                column: "simulator_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_maintenance_logs",
                schema: "maint",
                table: "maintenance_logs",
                column: "maintenance_log_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_maintenance_checklists",
                schema: "maint",
                table: "maintenance_checklists",
                column: "checklist_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_engineers",
                schema: "maint",
                table: "engineers",
                column: "engineer_id");

            migrationBuilder.AddForeignKey(
                name: "FK_maintenance_logs_simulators_simulator_id",
                schema: "maint",
                table: "maintenance_logs",
                column: "simulator_id",
                principalSchema: "maint",
                principalTable: "simulators",
                principalColumn: "simulator_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
