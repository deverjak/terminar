using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Tenants.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPluginActivations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_plugin_activations",
                schema: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plugin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_plugin_activations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_plugin_activations_tenant_id",
                schema: "tenants",
                table: "tenant_plugin_activations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_plugin_activations_tenant_id_plugin_id",
                schema: "tenants",
                table: "tenant_plugin_activations",
                columns: new[] { "tenant_id", "plugin_id" },
                unique: true);

            // Auto-activate excusals plugin for all existing tenants (pre-plugin-system tenants were all using excusals)
            migrationBuilder.Sql(@"
                INSERT INTO tenants.tenant_plugin_activations (""Id"", tenant_id, plugin_id, is_enabled, enabled_at)
                SELECT gen_random_uuid(), t.tenant_id, 'excusals', TRUE, NOW()
                FROM tenants.tenants t
                ON CONFLICT (tenant_id, plugin_id) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_plugin_activations",
                schema: "tenants");
        }
    }
}
