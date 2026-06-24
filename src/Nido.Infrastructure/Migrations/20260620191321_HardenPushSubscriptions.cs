using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenPushSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY usuario_id, endpoint
                               ORDER BY created_at DESC, id DESC) AS rn
                    FROM suscripciones_push
                )
                DELETE FROM suscripciones_push AS subscription
                USING ranked
                WHERE subscription.id = ranked.id
                  AND ranked.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_suscripciones_push_usuario_endpoint",
                table: "suscripciones_push",
                columns: new[] { "usuario_id", "endpoint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_suscripciones_push_usuario_endpoint",
                table: "suscripciones_push");
        }
    }
}
