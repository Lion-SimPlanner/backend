using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LionSimPlanner.Personnel.Infrastructure.Migrations
{
    public partial class AddExternalPilots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "type_ratings",
                schema: "hr",
                table: "pilots",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "required_syllabus",
                schema: "hr",
                table: "pilots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "type_ratings",
                schema: "hr",
                table: "pilots",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "required_syllabus",
                schema: "hr",
                table: "pilots",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

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
        }
    }
}
