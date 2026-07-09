using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamificacionNivelesDesbloqueados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gamificacion_niveles_desbloqueados",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nivel = table.Column<int>(type: "integer", nullable: false),
                    desbloqueado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gamificacion_niveles_desbloqueados", x => x.id);
                    table.ForeignKey(
                        name: "FK_gamificacion_niveles_desbloqueados_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gamificacion_niveles_usuario",
                table: "gamificacion_niveles_desbloqueados",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "uq_gamificacion_niveles_usuario_nivel",
                table: "gamificacion_niveles_desbloqueados",
                columns: new[] { "usuario_id", "nivel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gamificacion_niveles_desbloqueados");
        }
    }
}
