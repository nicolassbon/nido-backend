using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260620090000_AddProductoCompraEstandar")]
    public partial class AddProductoCompraEstandar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cantidad_compra_estandar",
                table: "productos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unidad_compra_estandar",
                table: "productos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                WITH defaults(nombre_normalizado, cantidad, unidad) AS (
                    VALUES
                        ('arroz', 1.00::numeric, 'kg'),
                        ('harina', 1.00::numeric, 'kg'),
                        ('azucar', 1.00::numeric, 'kg'),
                        ('sal', 1.00::numeric, 'kg'),
                        ('leche', 1.00::numeric, 'lt'),
                        ('aceite', 1.00::numeric, 'lt'),
                        ('ajo en polvo', 1.00::numeric, 'unidad'),
                        ('queso rallado', 1.00::numeric, 'unidad'),
                        ('manteca', 1.00::numeric, 'unidad'),
                        ('fideos', 1.00::numeric, 'unidad')
                )
                UPDATE productos p
                SET cantidad_compra_estandar = d.cantidad,
                    unidad_compra_estandar = d.unidad
                FROM defaults d
                WHERE lower(btrim(p.nombre)) = d.nombre_normalizado;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cantidad_compra_estandar",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "unidad_compra_estandar",
                table: "productos");
        }
    }
}
