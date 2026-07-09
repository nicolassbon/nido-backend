using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // `icono` pasa a almacenar la key relativa del bucket de imágenes (Spaces) para
            // cada categoría, consumida por la vista Alacena. `icono_svg` no se toca: sigue
            // sirviendo a la vista de Detalle de Producto.
            migrationBuilder.Sql("""
                UPDATE categorias_producto SET icono = 'categorias/aceites.jpg' WHERE nombre = 'Aceites';
                UPDATE categorias_producto SET icono = 'categorias/arroz.jpg' WHERE nombre = 'Arroz';
                UPDATE categorias_producto SET icono = 'categorias/azucar-endulzantes.png' WHERE nombre = 'Azúcar y Endulzantes';
                UPDATE categorias_producto SET icono = 'categorias/bebes.jpg' WHERE nombre = 'Bebés';
                UPDATE categorias_producto SET icono = 'categorias/bebidas.jpg' WHERE nombre = 'Bebidas';
                UPDATE categorias_producto SET icono = 'categorias/bebidas-alcoholicas.jpeg' WHERE nombre = 'Bebidas Alcohólicas';
                UPDATE categorias_producto SET icono = 'categorias/carnes-porcinas.jpg' WHERE nombre = 'Carnes Porcinas';
                UPDATE categorias_producto SET icono = 'categorias/carnes-vacunas.jpg' WHERE nombre = 'Carnes Vacunas';
                UPDATE categorias_producto SET icono = 'categorias/cereales.jpg' WHERE nombre = 'Cereales';
                UPDATE categorias_producto SET icono = 'categorias/condimentos.jpg' WHERE nombre = 'Condimentos';
                UPDATE categorias_producto SET icono = 'categorias/congelados.jpg' WHERE nombre = 'Congelados';
                UPDATE categorias_producto SET icono = 'categorias/conservas.jpg' WHERE nombre = 'Conservas';
                UPDATE categorias_producto SET icono = 'categorias/fiambres-embutidos.jpg' WHERE nombre = 'Fiambres y Embutidos';
                UPDATE categorias_producto SET icono = 'categorias/frutas.jpg' WHERE nombre = 'Frutas';
                UPDATE categorias_producto SET icono = 'categorias/golosinas.jpg' WHERE nombre = 'Golosinas';
                UPDATE categorias_producto SET icono = 'categorias/harinas.jpg' WHERE nombre = 'Harinas';
                UPDATE categorias_producto SET icono = 'categorias/higiene-personal.jpg' WHERE nombre = 'Higiene Personal';
                UPDATE categorias_producto SET icono = 'categorias/huevos.jpg' WHERE nombre = 'Huevos';
                UPDATE categorias_producto SET icono = 'categorias/lacteos.jpg' WHERE nombre = 'Lácteos';
                UPDATE categorias_producto SET icono = 'categorias/legumbres.jpg' WHERE nombre = 'Legumbres';
                UPDATE categorias_producto SET icono = 'categorias/limpieza.jpg' WHERE nombre = 'Limpieza';
                UPDATE categorias_producto SET icono = 'categorias/mascotas.jpeg' WHERE nombre = 'Mascotas';
                UPDATE categorias_producto SET icono = 'categorias/otros.jpg' WHERE nombre = 'Otros';
                UPDATE categorias_producto SET icono = 'categorias/panificados.jpg' WHERE nombre = 'Panificados';
                UPDATE categorias_producto SET icono = 'categorias/pastas.jpg' WHERE nombre = 'Pastas';
                UPDATE categorias_producto SET icono = 'categorias/pescados-mariscos.jpg' WHERE nombre = 'Pescados y Mariscos';
                UPDATE categorias_producto SET icono = 'categorias/pollo-aves.jpg' WHERE nombre = 'Pollo y Aves';
                UPDATE categorias_producto SET icono = 'categorias/productos-dieteticos.jpg' WHERE nombre = 'Productos Dietéticos';
                UPDATE categorias_producto SET icono = 'categorias/productos-sin-tacc.jpg' WHERE nombre = 'Productos Sin TACC';
                UPDATE categorias_producto SET icono = 'categorias/reposteria.jpg' WHERE nombre = 'Repostería';
                UPDATE categorias_producto SET icono = 'categorias/salsas-aderezos.jpg' WHERE nombre = 'Salsas y Aderezos';
                UPDATE categorias_producto SET icono = 'categorias/snacks.jpg' WHERE nombre = 'Snacks';
                UPDATE categorias_producto SET icono = 'categorias/verduras.jpeg' WHERE nombre = 'Verduras';
            """);

            migrationBuilder.Sql("""
                INSERT INTO categorias_producto (id, nombre, ttl_dias, icono_svg, icono)
                VALUES
                ('045a4233-fdcf-4260-af59-f82db8d000a9', 'Farmacia', 365, NULL, 'categorias/farmacia.png'),
                ('607dcdb4-4174-4c87-906e-9c000ca4e1ef', 'Galletas', 14, NULL, 'categorias/galletas.jpg')
                ON CONFLICT DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE productos SET categoria_id = NULL WHERE categoria_id IN (
                    SELECT id FROM categorias_producto WHERE nombre IN ('Farmacia', 'Galletas')
                );
                DELETE FROM categorias_producto WHERE nombre IN ('Farmacia', 'Galletas');
                UPDATE categorias_producto SET icono = NULL;
            """);
        }
    }
}
