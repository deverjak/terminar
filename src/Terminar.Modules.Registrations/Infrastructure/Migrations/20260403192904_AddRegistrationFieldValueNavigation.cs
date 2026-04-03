using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Registrations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationFieldValueNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_participant_field_values_Registrations_RegistrationId",
                schema: "registrations",
                table: "participant_field_values",
                column: "RegistrationId",
                principalSchema: "registrations",
                principalTable: "Registrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_participant_field_values_Registrations_RegistrationId",
                schema: "registrations",
                table: "participant_field_values");
        }
    }
}
