using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeIngredienteRecetaProductoOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql"))
            {
                migrationBuilder.Sql("""
                    ALTER TABLE ingredientes_receta
                    ALTER COLUMN producto_id DROP NOT NULL;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty: reverting this safely would require
            // deciding what to do with existing rows that legitimately have
            // no associated product.
        }
    }
}
