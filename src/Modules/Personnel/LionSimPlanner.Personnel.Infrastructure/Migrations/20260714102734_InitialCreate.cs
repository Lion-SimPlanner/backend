using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Personnel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hr");

            migrationBuilder.CreateTable(
                name: "instructors",
                schema: "hr",
                columns: table => new
                {
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    corporate_email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    role_level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    certified_types = table.Column<string>(type: "jsonb", nullable: false),
                    authorized_syllabi = table.Column<string>(type: "jsonb", nullable: false),
                    license_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_duty_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_duty_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_monthly_hours = table.Column<int>(type: "integer", nullable: false),
                    max_monthly_hours = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instructors", x => x.instructor_id);
                });

            migrationBuilder.CreateTable(
                name: "pilots",
                schema: "hr",
                columns: table => new
                {
                    pilot_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    corporate_email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    rank = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    type_ratings = table.Column<string>(type: "jsonb", nullable: false),
                    medical_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_training_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_training_due = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    required_syllabus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_duty_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_duty_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilots", x => x.pilot_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_instructors_employee_code",
                schema: "hr",
                table: "instructors",
                column: "employee_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilots_employee_code",
                schema: "hr",
                table: "pilots",
                column: "employee_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instructors",
                schema: "hr");

            migrationBuilder.DropTable(
                name: "pilots",
                schema: "hr");
        }
    }
}
