using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260709213000_AddTutorialUsuario")]
    public partial class AddTutorialUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tutorial_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    alacena_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    recetas_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    lista_compras_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    finanzas_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    planificador_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    tareas_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notificaciones_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    perfil_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    configuracion_completado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("tutorial_usuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "tutorial_usuario_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tutorial_usuario_usuario",
                table: "tutorial_usuario",
                column: "usuario_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "tutorial_usuario");
        }
    }
}
