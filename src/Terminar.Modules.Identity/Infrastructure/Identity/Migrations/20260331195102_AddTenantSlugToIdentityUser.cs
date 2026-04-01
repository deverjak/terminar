using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Terminar.Modules.Identity.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSlugToIdentityUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantSlug",
                schema: "identity",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantSlug",
                schema: "identity",
                table: "AspNetUsers");
        }
    }
}
