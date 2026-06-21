using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotasReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notas_receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_receta", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_receta_hogares_hogar_id",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notas_receta_recetas_receta_id",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notas_receta_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notas_receta_hogar_id",
                table: "notas_receta",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "ix_notas_receta_receta_hogar",
                table: "notas_receta",
                columns: new[] { "receta_id", "hogar_id" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_receta_usuario_id",
                table: "notas_receta",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notas_receta");
        }
    }
}
