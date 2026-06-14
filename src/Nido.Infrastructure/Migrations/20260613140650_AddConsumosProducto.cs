using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumosProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumos_producto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    producto_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cantidad = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unidad_medida = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    fecha_consumo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumos_producto", x => x.id);
                    table.ForeignKey(
                        name: "FK_consumos_producto_hogares_hogar_id",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_consumos_producto_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consumos_producto_hogar_fecha",
                table: "consumos_producto",
                columns: new[] { "hogar_id", "fecha_consumo" });

            migrationBuilder.CreateIndex(
                name: "ix_consumos_producto_hogar_producto_fecha",
                table: "consumos_producto",
                columns: new[] { "hogar_id", "producto_id", "fecha_consumo" });

            migrationBuilder.CreateIndex(
                name: "IX_consumos_producto_producto_id",
                table: "consumos_producto",
                column: "producto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumos_producto");
        }
    }
}
