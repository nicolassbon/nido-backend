using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processed_telegram_updates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    update_id = table.Column<long>(type: "bigint", nullable: false),
                    update_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_telegram_updates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_batches", x => x.id);
                    table.CheckConstraint("ck_telegram_batches_status", "status >= 0 AND status <= 4");
                });

            migrationBuilder.CreateTable(
                name: "telegram_chat_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    unpaired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_chat_links", x => x.id);
                    table.ForeignKey(
                        name: "telegram_chat_links_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "telegram_chat_links_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_pairing_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_pairing_codes", x => x.id);
                    table.CheckConstraint("ck_telegram_pairing_codes_attempt_count", "attempt_count <= 5");
                    table.ForeignKey(
                        name: "telegram_pairing_codes_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "telegram_pairing_codes_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_pairing_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_pairing_tokens", x => x.id);
                    table.ForeignKey(
                        name: "telegram_pairing_tokens_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "telegram_pairing_tokens_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    message_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    locked_until = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_outbox_messages", x => x.id);
                    table.ForeignKey(
                        name: "telegram_outbox_messages_batch_id_fkey",
                        column: x => x.batch_id,
                        principalTable: "telegram_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "uq_processed_telegram_updates_update_id",
                table: "processed_telegram_updates",
                column: "update_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_chat_links_hogar_id",
                table: "telegram_chat_links",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "uq_telegram_chat_links_active_chat_id",
                table: "telegram_chat_links",
                column: "chat_id",
                unique: true,
                filter: "unpaired_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_telegram_chat_links_active_usuario_hogar",
                table: "telegram_chat_links",
                columns: new[] { "usuario_id", "hogar_id" },
                unique: true,
                filter: "unpaired_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_outbox_messages_batch_id",
                table: "telegram_outbox_messages",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "uq_telegram_outbox_messages_pending",
                table: "telegram_outbox_messages",
                columns: new[] { "hogar_id", "chat_id", "message_type" },
                unique: true,
                filter: "status = 0");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_pairing_codes_hogar_id",
                table: "telegram_pairing_codes",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_pairing_codes_usuario_id",
                table: "telegram_pairing_codes",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_telegram_pairing_codes_hash",
                table: "telegram_pairing_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telegram_pairing_tokens_hogar_id",
                table: "telegram_pairing_tokens",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_telegram_pairing_tokens_usuario_id",
                table: "telegram_pairing_tokens",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_telegram_pairing_tokens_hash",
                table: "telegram_pairing_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_telegram_updates");

            migrationBuilder.DropTable(
                name: "telegram_chat_links");

            migrationBuilder.DropTable(
                name: "telegram_outbox_messages");

            migrationBuilder.DropTable(
                name: "telegram_pairing_codes");

            migrationBuilder.DropTable(
                name: "telegram_pairing_tokens");

            migrationBuilder.DropTable(
                name: "telegram_batches");
        }
    }
}
