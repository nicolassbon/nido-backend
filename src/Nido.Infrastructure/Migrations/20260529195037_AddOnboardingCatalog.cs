using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "restricciones_usuario_usuario_id_fkey",
                table: "restricciones_usuario");

            migrationBuilder.DropPrimaryKey(
                name: "restricciones_usuario_pkey",
                table: "restricciones_usuario");

            migrationBuilder.DropIndex(
                name: "IX_restricciones_usuario_usuario_id",
                table: "restricciones_usuario");

            migrationBuilder.DropColumn(
                name: "id",
                table: "restricciones_usuario");

            migrationBuilder.DropColumn(
                name: "descripcion",
                table: "restricciones_usuario");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "restricciones_usuario");

            migrationBuilder.AddColumn<Guid>(
                name: "restriccion_id",
                table: "restricciones_usuario",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "restricciones_usuario_pkey",
                table: "restricciones_usuario",
                columns: new[] { "usuario_id", "restriccion_id" });

            migrationBuilder.CreateTable(
                name: "metas_catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("metas_catalogo_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restricciones_catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("restricciones_catalogo_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hogar_metas",
                columns: table => new
                {
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("hogar_metas_pkey", x => new { x.hogar_id, x.meta_id });
                    table.ForeignKey(
                        name: "hogar_metas_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "hogar_metas_meta_id_fkey",
                        column: x => x.meta_id,
                        principalTable: "metas_catalogo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "restricciones_catalogo",
                columns: new[] { "id", "nombre", "tipo" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "Vegetariano",   "restriccion_alimentaria" },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "Vegano",         "restriccion_alimentaria" },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "Sin TACC",       "restriccion_alimentaria" },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "Sin lactosa",    "restriccion_alimentaria" },
                    { new Guid("a1000000-0000-0000-0000-000000000011"), "Maní",           "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000012"), "Gluten",         "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000013"), "Lactosa",        "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000014"), "Mariscos",       "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000015"), "Soja",           "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000016"), "Huevo",          "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000017"), "Frutos secos",   "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000018"), "Pescado",        "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000019"), "Sésamo",         "alergia" },
                    { new Guid("a1000000-0000-0000-0000-000000000020"), "Mostaza",        "alergia" }
                });

            migrationBuilder.InsertData(
                table: "metas_catalogo",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), "Ahorro" },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), "Mejor organización" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_restricciones_usuario_restriccion_id",
                table: "restricciones_usuario",
                column: "restriccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_hogar_metas_meta_id",
                table: "hogar_metas",
                column: "meta_id");

            migrationBuilder.AddForeignKey(
                name: "restricciones_usuario_restriccion_id_fkey",
                table: "restricciones_usuario",
                column: "restriccion_id",
                principalTable: "restricciones_catalogo",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "restricciones_usuario_usuario_id_fkey",
                table: "restricciones_usuario",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "restricciones_usuario_restriccion_id_fkey",
                table: "restricciones_usuario");

            migrationBuilder.DropForeignKey(
                name: "restricciones_usuario_usuario_id_fkey",
                table: "restricciones_usuario");

            migrationBuilder.DropTable(
                name: "hogar_metas");

            migrationBuilder.DropTable(
                name: "restricciones_catalogo");

            migrationBuilder.DropTable(
                name: "metas_catalogo");

            migrationBuilder.DropPrimaryKey(
                name: "restricciones_usuario_pkey",
                table: "restricciones_usuario");

            migrationBuilder.DropIndex(
                name: "IX_restricciones_usuario_restriccion_id",
                table: "restricciones_usuario");

            migrationBuilder.DropColumn(
                name: "restriccion_id",
                table: "restricciones_usuario");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "restricciones_usuario",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()");

            migrationBuilder.AddColumn<string>(
                name: "descripcion",
                table: "restricciones_usuario",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "restricciones_usuario",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "restricciones_usuario_pkey",
                table: "restricciones_usuario",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_restricciones_usuario_usuario_id",
                table: "restricciones_usuario",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "restricciones_usuario_usuario_id_fkey",
                table: "restricciones_usuario",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id");
        }
    }
}
