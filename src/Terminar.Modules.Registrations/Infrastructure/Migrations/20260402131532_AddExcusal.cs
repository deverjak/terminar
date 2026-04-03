using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Registrations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExcusal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcusalCredits",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceExcusalId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    ValidWindowIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RedeemedCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcusalCredits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Excusals",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExcusalCreditId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Excusals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExcusalCreditAuditEntries",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExcusalCreditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldChanged = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PreviousValue = table.Column<string>(type: "text", nullable: false),
                    NewValue = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcusalCreditAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcusalCreditAuditEntries_ExcusalCredits_ExcusalCreditId",
                        column: x => x.ExcusalCreditId,
                        principalSchema: "registrations",
                        principalTable: "ExcusalCredits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcusalCreditAuditEntries_ExcusalCreditId",
                schema: "registrations",
                table: "ExcusalCreditAuditEntries",
                column: "ExcusalCreditId");

            migrationBuilder.CreateIndex(
                name: "IX_Excusals_CourseId",
                schema: "registrations",
                table: "Excusals",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Excusals_RegistrationId_SessionId",
                schema: "registrations",
                table: "Excusals",
                columns: new[] { "RegistrationId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcusalCreditAuditEntries",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "Excusals",
                schema: "registrations");

            migrationBuilder.DropTable(
                name: "ExcusalCredits",
                schema: "registrations");
        }
    }
}
