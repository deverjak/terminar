using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Courses.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCustomFieldNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_course_field_assignments_Courses_CourseId",
                schema: "courses",
                table: "course_field_assignments",
                column: "CourseId",
                principalSchema: "courses",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_course_field_assignments_Courses_CourseId",
                schema: "courses",
                table: "course_field_assignments");
        }
    }
}
