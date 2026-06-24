using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260619120000_ShoppingListsSavedRecipesAndStockOrigin")]
    public partial class ShoppingListsSavedRecipesAndStockOrigin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "origen_carga",
                table: "stock_hogar",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.Sql("""
                ALTER TABLE stock_hogar
                ADD CONSTRAINT ck_stock_hogar_origen_carga
                CHECK (origen_carga IN ('manual', 'codigo_barras', 'ticket_compra'));
                """);

            migrationBuilder.CreateTable(
                name: "listas_compra_hogar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creada_por = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("listas_compra_hogar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "listas_compra_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "listas_compra_hogar_creada_por_fkey",
                        column: x => x.creada_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_listas_compra_hogar_hogar",
                table: "listas_compra_hogar",
                column: "hogar_id");

            migrationBuilder.CreateTable(
                name: "recetas_guardadas_hogar",
                columns: table => new
                {
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardada_por = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recetas_guardadas_hogar_pkey", x => new { x.hogar_id, x.receta_id });
                    table.ForeignKey(
                        name: "recetas_guardadas_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recetas_guardadas_hogar_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recetas_guardadas_hogar_guardada_por_fkey",
                        column: x => x.guardada_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_recetas_guardadas_hogar_guardada_por",
                table: "recetas_guardadas_hogar",
                column: "guardada_por");

            migrationBuilder.CreateIndex(
                name: "IX_recetas_guardadas_hogar_receta_id",
                table: "recetas_guardadas_hogar",
                column: "receta_id");

            migrationBuilder.AddColumn<Guid>(
                name: "lista_id",
                table: "lista_compras",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nombre_manual",
                table: "lista_compras",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO listas_compra_hogar (id, hogar_id, nombre, creada_por, created_at)
                SELECT uuid_generate_v4(), lc.hogar_id, 'Principal', (array_agg(lc.agregado_por ORDER BY lc.created_at))[1], MIN(lc.created_at)
                FROM lista_compras lc
                WHERE NOT EXISTS (
                    SELECT 1 FROM listas_compra_hogar lch WHERE lch.hogar_id = lc.hogar_id
                )
                GROUP BY lc.hogar_id;

                UPDATE lista_compras lc
                SET lista_id = lch.id,
                    nombre_manual = NULLIF(lc.producto_nombre_snapshot, '')
                FROM listas_compra_hogar lch
                WHERE lc.lista_id IS NULL
                  AND lch.hogar_id = lc.hogar_id
                  AND lch.nombre = 'Principal';
                """);

            migrationBuilder.DropForeignKey(
                name: "lista_compras_producto_id_fkey",
                table: "lista_compras");

            migrationBuilder.AlterColumn<Guid>(
                name: "producto_id",
                table: "lista_compras",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "lista_compras_producto_id_fkey",
                table: "lista_compras",
                column: "producto_id",
                principalTable: "productos",
                principalColumn: "id");

            migrationBuilder.CreateIndex(
                name: "IX_lista_compras_lista_id",
                table: "lista_compras",
                column: "lista_id");

            migrationBuilder.AddForeignKey(
                name: "lista_compras_lista_id_fkey",
                table: "lista_compras",
                column: "lista_id",
                principalTable: "listas_compra_hogar",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "lista_compras_lista_id_fkey",
                table: "lista_compras");

            migrationBuilder.DropForeignKey(
                name: "lista_compras_producto_id_fkey",
                table: "lista_compras");

            migrationBuilder.DropIndex(
                name: "IX_lista_compras_lista_id",
                table: "lista_compras");

            migrationBuilder.AlterColumn<Guid>(
                name: "producto_id",
                table: "lista_compras",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "lista_compras_producto_id_fkey",
                table: "lista_compras",
                column: "producto_id",
                principalTable: "productos",
                principalColumn: "id");

            migrationBuilder.DropColumn(
                name: "lista_id",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "nombre_manual",
                table: "lista_compras");

            migrationBuilder.DropTable(name: "recetas_guardadas_hogar");

            migrationBuilder.DropTable(name: "listas_compra_hogar");

            migrationBuilder.Sql("ALTER TABLE stock_hogar DROP CONSTRAINT IF EXISTS ck_stock_hogar_origen_carga;");

            migrationBuilder.DropColumn(
                name: "origen_carga",
                table: "stock_hogar");
        }
    }
}
