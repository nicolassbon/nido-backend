using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facturas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    archivo_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pagada = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("facturas_pkey", x => x.id);
                    table.ForeignKey(
                        name: "facturas_creado_por_fkey",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "facturas_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_facturas_hogar",
                table: "facturas",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_facturas_creado_por",
                table: "facturas",
                column: "creado_por");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "facturas");
        }
    }
}
