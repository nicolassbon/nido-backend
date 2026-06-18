using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPushSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suscripciones_push",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    p256dh = table.Column<string>(type: "text", nullable: false),
                    auth = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("suscripciones_push_pkey", x => x.id);
                    table.ForeignKey(
                        name: "suscripciones_push_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_suscripciones_push_usuario",
                table: "suscripciones_push",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suscripciones_push");
        }
    }
}
