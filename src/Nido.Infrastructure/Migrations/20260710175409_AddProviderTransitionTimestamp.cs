using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderTransitionTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "provider_transition_at",
                table: "hogares",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "provider_transition_at",
                table: "hogares");
        }
    }
}
