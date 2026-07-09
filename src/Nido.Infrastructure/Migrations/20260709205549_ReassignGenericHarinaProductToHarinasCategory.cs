using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReassignGenericHarinaProductToHarinasCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El producto genérico "Harina" quedó cargado bajo "Repostería" desde una seed
            // vieja (20260621183000_AddCategorySvgAndSeedData), en vez de la categoría
            // dedicada "Harinas" que ya existe con productos bien nombrados (Harina de Trigo
            // 0000, Harina Integral, etc.).
            migrationBuilder.Sql("""
                UPDATE productos
                SET categoria_id = (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1)
                WHERE nombre = 'Harina'
                  AND categoria_id = (SELECT id FROM categorias_producto WHERE nombre = 'Repostería' LIMIT 1);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE productos
                SET categoria_id = (SELECT id FROM categorias_producto WHERE nombre = 'Repostería' LIMIT 1)
                WHERE nombre = 'Harina'
                  AND categoria_id = (SELECT id FROM categorias_producto WHERE nombre = 'Harinas' LIMIT 1);
            """);
        }
    }
}
