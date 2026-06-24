using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260617180000_ExtendListaComprasForHistory")]
    public partial class ExtendListaComprasForHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "comprado_en",
                table: "lista_compras",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "comprado_por",
                table: "lista_compras",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "lista_compras",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "grupo_nombre",
                table: "lista_compras",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "Productos agregados");

            migrationBuilder.AddColumn<int>(
                name: "orden",
                table: "lista_compras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "producto_nombre_snapshot",
                table: "lista_compras",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "removido_de_lista_at",
                table: "lista_compras",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE lista_compras AS lc
                SET producto_nombre_snapshot = p.nombre
                FROM productos AS p
                WHERE lc.producto_id = p.id
                  AND lc.producto_nombre_snapshot = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_lista_compras_comprado_por",
                table: "lista_compras",
                column: "comprado_por");

            migrationBuilder.AddForeignKey(
                name: "lista_compras_comprado_por_fkey",
                table: "lista_compras",
                column: "comprado_por",
                principalTable: "usuarios",
                principalColumn: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "lista_compras_comprado_por_fkey",
                table: "lista_compras");

            migrationBuilder.DropIndex(
                name: "IX_lista_compras_comprado_por",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "comprado_en",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "comprado_por",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "grupo_nombre",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "orden",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "producto_nombre_snapshot",
                table: "lista_compras");

            migrationBuilder.DropColumn(
                name: "removido_de_lista_at",
                table: "lista_compras");
        }
    }
}
