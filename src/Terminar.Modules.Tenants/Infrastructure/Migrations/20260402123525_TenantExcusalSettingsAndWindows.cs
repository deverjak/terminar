using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Tenants.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantExcusalSettingsAndWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "excusal_credit_generation_enabled",
                schema: "tenants",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "excusal_deadline_hours",
                schema: "tenants",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.AddColumn<int>(
                name: "excusal_forward_window_count",
                schema: "tenants",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "excusal_unenrollment_deadline_days",
                schema: "tenants",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.CreateTable(
                name: "ExcusalValidityWindows",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcusalValidityWindows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcusalValidityWindows_TenantId_Name",
                schema: "tenants",
                table: "ExcusalValidityWindows",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcusalValidityWindows",
                schema: "tenants");

            migrationBuilder.DropColumn(
                name: "excusal_credit_generation_enabled",
                schema: "tenants",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "excusal_deadline_hours",
                schema: "tenants",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "excusal_forward_window_count",
                schema: "tenants",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "excusal_unenrollment_deadline_days",
                schema: "tenants",
                table: "tenants");
        }
    }
}
