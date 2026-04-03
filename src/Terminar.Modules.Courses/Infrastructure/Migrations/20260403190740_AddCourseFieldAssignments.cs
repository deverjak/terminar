using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseFieldAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_field_assignments",
                schema: "courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_field_assignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_field_assignments_CourseId_FieldDefinitionId",
                schema: "courses",
                table: "course_field_assignments",
                columns: new[] { "CourseId", "FieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_field_assignments",
                schema: "courses");
        }
    }
}
