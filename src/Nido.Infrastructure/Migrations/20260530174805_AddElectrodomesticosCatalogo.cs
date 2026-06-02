using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddElectrodomesticosCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "catalogo_id",
                table: "electrodomesticos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "electrodomesticos_catalogo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    icono = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("electrodomesticos_catalogo_pkey", x => x.id);
                });


            migrationBuilder.InsertData(
    table: "electrodomesticos_catalogo",
    columns: new[] { "id", "nombre", "tipo", "icono", "imagen_url", "orden", "activo" },
    values: new object[,]
    {
        { Guid.Parse("11111111-1111-1111-1111-111111111111"), "Licuadora", "licuadora", "blender", null, 1, true },
        { Guid.Parse("22222222-2222-2222-2222-222222222222"), "Microondas", "microondas", "microwave", null, 2, true },
        { Guid.Parse("33333333-3333-3333-3333-333333333333"), "Horno/Cocina", "horno_cocina", "cooking-pot", null, 3, true },
        { Guid.Parse("44444444-4444-4444-4444-444444444444"), "Mixer", "mixer", "blend", null, 4, true },
        { Guid.Parse("55555555-5555-5555-5555-555555555555"), "Procesadora", "procesadora", "cog", null, 5, true },
        { Guid.Parse("66666666-6666-6666-6666-666666666666"), "Freidora de aire", "freidora_aire", "air-vent", null, 6, true },
        { Guid.Parse("77777777-7777-7777-7777-777777777777"), "Cafetera", "cafetera", "coffee", null, 7, true },
        { Guid.Parse("88888888-8888-8888-8888-888888888888"), "Tostadora", "tostadora", "square", null, 8, true },
        { Guid.Parse("99999999-9999-9999-9999-999999999999"), "Olla de presión", "olla_presion", "pressure-cooker", null, 9, true},
        { Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Parrilla eléctrica", "parrilla_electrica", "grill", null, 10, true},
});

            migrationBuilder.CreateIndex(
                name: "IX_electrodomesticos_catalogo_id",
                table: "electrodomesticos",
                column: "catalogo_id");

            migrationBuilder.AddForeignKey(
                name: "electrodomesticos_catalogo_id_fkey",
                table: "electrodomesticos",
                column: "catalogo_id",
                principalTable: "electrodomesticos_catalogo",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "electrodomesticos_catalogo_id_fkey",
                table: "electrodomesticos");

            migrationBuilder.DropTable(
                name: "electrodomesticos_catalogo");

            migrationBuilder.DropIndex(
                name: "IX_electrodomesticos_catalogo_id",
                table: "electrodomesticos");

            migrationBuilder.DropColumn(
                name: "catalogo_id",
                table: "electrodomesticos");
        }
    }
}
