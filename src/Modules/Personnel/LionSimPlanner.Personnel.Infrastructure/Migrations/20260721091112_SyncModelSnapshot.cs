using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Personnel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_pilots",
                schema: "hr",
                table: "pilots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_instructors",
                schema: "hr",
                table: "instructors");

            migrationBuilder.RenameIndex(
                name: "IX_pilots_employee_code",
                schema: "hr",
                table: "pilots",
                newName: "ix_pilots_employee_code");

            migrationBuilder.RenameIndex(
                name: "IX_instructors_employee_code",
                schema: "hr",
                table: "instructors",
                newName: "ix_instructors_employee_code");

            migrationBuilder.AlterColumn<string>(
                name: "corporate_email",
                schema: "hr",
                table: "pilots",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                schema: "hr",
                table: "pilots",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_number",
                schema: "hr",
                table: "pilots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ftl_status",
                schema: "hr",
                table: "pilots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_external_user",
                schema: "hr",
                table: "pilots",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "corporate_email",
                schema: "hr",
                table: "instructors",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_pilots",
                schema: "hr",
                table: "pilots",
                column: "pilot_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_instructors",
                schema: "hr",
                table: "instructors",
                column: "instructor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_pilots",
                schema: "hr",
                table: "pilots");

            migrationBuilder.DropPrimaryKey(
                name: "pk_instructors",
                schema: "hr",
                table: "instructors");

            migrationBuilder.DropColumn(
                name: "company_name",
                schema: "hr",
                table: "pilots");

            migrationBuilder.DropColumn(
                name: "contact_number",
                schema: "hr",
                table: "pilots");

            migrationBuilder.DropColumn(
                name: "ftl_status",
                schema: "hr",
                table: "pilots");

            migrationBuilder.DropColumn(
                name: "is_external_user",
                schema: "hr",
                table: "pilots");

            migrationBuilder.RenameIndex(
                name: "ix_pilots_employee_code",
                schema: "hr",
                table: "pilots",
                newName: "IX_pilots_employee_code");

            migrationBuilder.RenameIndex(
                name: "ix_instructors_employee_code",
                schema: "hr",
                table: "instructors",
                newName: "IX_instructors_employee_code");

            migrationBuilder.AlterColumn<string>(
                name: "corporate_email",
                schema: "hr",
                table: "pilots",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "corporate_email",
                schema: "hr",
                table: "instructors",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AddPrimaryKey(
                name: "PK_pilots",
                schema: "hr",
                table: "pilots",
                column: "pilot_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_instructors",
                schema: "hr",
                table: "instructors",
                column: "instructor_id");
        }
    }
}
