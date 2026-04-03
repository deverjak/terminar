using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Registrations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSelfCancellationTokenToSafeLinkToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticipantMagicLinks",
                schema: "registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    MagicLinkToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MagicLinkExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MagicLinkUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PortalToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PortalTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipantMagicLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantMagicLinks_TenantId_MagicLinkToken",
                schema: "registrations",
                table: "ParticipantMagicLinks",
                columns: new[] { "TenantId", "MagicLinkToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipantMagicLinks_TenantId_PortalToken",
                schema: "registrations",
                table: "ParticipantMagicLinks",
                columns: new[] { "TenantId", "PortalToken" },
                unique: true,
                filter: "\"PortalToken\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticipantMagicLinks",
                schema: "registrations");
        }
    }
}
