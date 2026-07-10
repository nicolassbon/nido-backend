using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "suscripcion_vence_el",
                table: "hogares",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE hogares SET suscripcion_vence_el = NOW() + INTERVAL '30 days' WHERE plan = 'premium';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "suscripcion_vence_el",
                table: "hogares");
        }
    }
}
