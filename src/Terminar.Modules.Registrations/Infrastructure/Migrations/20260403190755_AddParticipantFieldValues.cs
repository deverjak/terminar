using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Registrations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participant_field_values",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_participant_field_values", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_participant_field_values_RegistrationId_FieldDefinitionId",
                schema: "registrations",
                table: "participant_field_values",
                columns: new[] { "RegistrationId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_participant_field_values_TenantId",
                schema: "registrations",
                table: "participant_field_values",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participant_field_values",
                schema: "registrations");
        }
    }
}
