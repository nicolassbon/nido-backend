using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResenasRecetaConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_resenias_receta_receta_id",
                table: "resenias_receta");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "resenias_receta",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "resenias_receta_receta_id_usuario_id_key",
                table: "resenias_receta",
                columns: new[] { "receta_id", "usuario_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "resenias_receta_receta_id_usuario_id_key",
                table: "resenias_receta");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "resenias_receta");

            migrationBuilder.CreateIndex(
                name: "IX_resenias_receta_receta_id",
                table: "resenias_receta",
                column: "receta_id");
        }
    }
}
