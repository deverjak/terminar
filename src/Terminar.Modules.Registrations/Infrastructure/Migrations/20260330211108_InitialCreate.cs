using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Registrations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registrations");

            migrationBuilder.CreateTable(
                name: "Registrations",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    RegistrationSource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegisteredByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SafeLinkToken = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_CourseId",
                schema: "registrations",
                table: "Registrations",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_TenantId",
                schema: "registrations",
                table: "Registrations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Registrations",
                schema: "registrations");
        }
    }
}
