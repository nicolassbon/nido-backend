using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHogarToResenasReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Agregamos la columna nullable para poder backfillear sin fallar.
            migrationBuilder.AddColumn<Guid>(
                name: "hogar_id",
                table: "resenias_receta",
                type: "uuid",
                nullable: true);

            // 2. Backfill: asignamos a cada reseña el hogar del usuario que la creó.
            //    Si un usuario está en varios hogares, tomamos el primero (poco común).
            migrationBuilder.Sql(@"
                UPDATE resenias_receta r
                SET hogar_id = m.hogar_id
                FROM (
                    SELECT DISTINCT ON (usuario_id) usuario_id, hogar_id
                    FROM miembros_hogar
                    ORDER BY usuario_id, hogar_id
                ) m
                WHERE m.usuario_id = r.usuario_id
                  AND r.hogar_id IS NULL;
            ");

            // 3. Borramos reseñas huérfanas (usuario que ya no pertenece a ningún hogar).
            migrationBuilder.Sql("DELETE FROM resenias_receta WHERE hogar_id IS NULL;");

            // 4. Ahora sí, forzamos NOT NULL.
            migrationBuilder.AlterColumn<Guid>(
                name: "hogar_id",
                table: "resenias_receta",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_resenias_receta_hogar_id",
                table: "resenias_receta",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "uq_resenias_receta_hogar_usuario",
                table: "resenias_receta",
                columns: new[] { "receta_id", "hogar_id", "usuario_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_resenias_receta_hogares_hogar_id",
                table: "resenias_receta",
                column: "hogar_id",
                principalTable: "hogares",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_resenias_receta_hogares_hogar_id",
                table: "resenias_receta");

            migrationBuilder.DropIndex(
                name: "IX_resenias_receta_hogar_id",
                table: "resenias_receta");

            migrationBuilder.DropIndex(
                name: "uq_resenias_receta_hogar_usuario",
                table: "resenias_receta");

            migrationBuilder.DropColumn(
                name: "hogar_id",
                table: "resenias_receta");
        }
    }
}
