using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdPlanAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "grace_period_ends_at",
                table: "hogares",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mercado_pago_customer_id",
                table: "hogares",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mercado_pago_subscription_id",
                table: "hogares",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan",
                table: "hogares",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "free");

            migrationBuilder.AddColumn<DateTime>(
                name: "plan_updated_at",
                table: "hogares",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_status",
                table: "hogares",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "none");

            migrationBuilder.AddColumn<DateTime>(
                name: "trial_ends_at",
                table: "hogares",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    provider_payment_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    provider_subscription_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_webhook_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_webhook_events_hogar_id",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_webhook_events_hogar_id",
                table: "payment_webhook_events",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_provider_event_id",
                table: "payment_webhook_events",
                columns: new[] { "provider", "provider_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_webhook_events");

            migrationBuilder.DropColumn(
                name: "grace_period_ends_at",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "mercado_pago_customer_id",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "mercado_pago_subscription_id",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "plan",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "plan_updated_at",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "subscription_status",
                table: "hogares");

            migrationBuilder.DropColumn(
                name: "trial_ends_at",
                table: "hogares");
        }
    }
}
