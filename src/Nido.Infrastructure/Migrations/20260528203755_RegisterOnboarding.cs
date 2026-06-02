using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RegisterOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "foto_url",
                table: "usuarios",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sexo",
                table: "usuarios",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nombre_representado",
                table: "miembros_hogar",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "onboarding_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("onboarding_goals_pkey", x => x.id);
                    table.ForeignKey(
                        name: "onboarding_goals_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "onboarding_state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step1_completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    step2_completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    step2_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    step3_completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    step3_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    step4_completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    step4_skipped = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("onboarding_state_pkey", x => x.id);
                    table.ForeignKey(
                        name: "onboarding_state_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "onboarding_state_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_goals_hogar_id",
                table: "onboarding_goals",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_onboarding_state_hogar_id",
                table: "onboarding_state",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "ux_onboarding_state_usuario_hogar",
                table: "onboarding_state",
                columns: new[] { "usuario_id", "hogar_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "onboarding_goals");

            migrationBuilder.DropTable(
                name: "onboarding_state");

            migrationBuilder.DropColumn(
                name: "foto_url",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "sexo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "nombre_representado",
                table: "miembros_hogar");
        }
    }
}
