using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260621123000_AddProductNutritionDetails")]
    public partial class AddProductNutritionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "base",
                table: "info_nutricional_producto",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "porcion",
                table: "info_nutricional_producto",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "info_nutricional_producto_detalle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    info_nutricional_producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    unidad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    porcentaje_diario = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("info_nutricional_producto_detalle_pkey", x => x.id);
                    table.ForeignKey(
                        name: "info_nutricional_producto_detalle_info_id_fkey",
                        column: x => x.info_nutricional_producto_id,
                        principalTable: "info_nutricional_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_info_nutricional_producto_detalle_info_id",
                table: "info_nutricional_producto_detalle",
                column: "info_nutricional_producto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "info_nutricional_producto_detalle");

            migrationBuilder.DropColumn(
                name: "base",
                table: "info_nutricional_producto");

            migrationBuilder.DropColumn(
                name: "porcion",
                table: "info_nutricional_producto");
        }
    }
}
