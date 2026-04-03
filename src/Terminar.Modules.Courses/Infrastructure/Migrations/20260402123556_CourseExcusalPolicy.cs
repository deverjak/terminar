using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CourseExcusalPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "excusal_credit_generation_override",
                schema: "courses",
                table: "Courses",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "excusal_tags",
                schema: "courses",
                table: "Courses",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "excusal_validity_window_id",
                schema: "courses",
                table: "Courses",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "excusal_credit_generation_override",
                schema: "courses",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "excusal_tags",
                schema: "courses",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "excusal_validity_window_id",
                schema: "courses",
                table: "Courses");
        }
    }
}
