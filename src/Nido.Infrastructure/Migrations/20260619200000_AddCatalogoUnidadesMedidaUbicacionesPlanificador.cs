using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nido.Infrastructure.Persistence;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(NidoDbContext))]
    [Migration("20260619200000_AddCatalogoUnidadesMedidaUbicacionesPlanificador")]
    public partial class AddCatalogoUnidadesMedidaUbicacionesPlanificador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── unidades_medida ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "unidades_medida",
                columns: table => new
                {
                    id     = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("unidades_medida_pkey", x => x.id);
                    table.UniqueConstraint("unidades_medida_codigo_key", x => x.codigo);
                });

            migrationBuilder.Sql("""
                INSERT INTO unidades_medida (id, codigo, nombre) VALUES
                  ('c1000000-0000-0000-0000-000000000001', 'unidad', 'Unidad'),
                  ('c1000000-0000-0000-0000-000000000002', 'gr',     'Gramos (gr)'),
                  ('c1000000-0000-0000-0000-000000000003', 'kg',     'Kilogramos (kg)'),
                  ('c1000000-0000-0000-0000-000000000004', 'ml',     'Mililitros (ml)'),
                  ('c1000000-0000-0000-0000-000000000005', 'lt',     'Litros (lt)'),
                  ('c1000000-0000-0000-0000-000000000006', 'cdita',  'Cucharadita'),
                  ('c1000000-0000-0000-0000-000000000007', 'cda',    'Cucharada');
                """);

            // ─── ubicaciones_catalogo ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ubicaciones_catalogo",
                columns: table => new
                {
                    id     = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icono  = table.Column<string>(type: "character varying(50)",  maxLength: 50,  nullable: true),
                    color  = table.Column<string>(type: "character varying(20)",  maxLength: 20,  nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ubicaciones_catalogo_pkey", x => x.id);
                    table.UniqueConstraint("ubicaciones_catalogo_nombre_key", x => x.nombre);
                });

            migrationBuilder.Sql("""
                INSERT INTO ubicaciones_catalogo (id, nombre, icono, color) VALUES
                  ('d1000000-0000-0000-0000-000000000001', 'Alacena',  'tag',          '#B48B6A'),
                  ('d1000000-0000-0000-0000-000000000002', 'Freezer',  'snowflake',    '#3E5E4A'),
                  ('d1000000-0000-0000-0000-000000000003', 'Heladera', 'refrigerator', '#927357');
                """);

            // ─── planificador_semana ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "planificador_semana",
                columns: table => new
                {
                    id           = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id     = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at   = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("planificador_semana_pkey", x => x.id);
                    table.UniqueConstraint("planificador_semana_hogar_fecha_key", x => new { x.hogar_id, x.fecha_inicio });
                    table.ForeignKey(
                        name: "planificador_semana_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_planificador_semana_hogar",
                table: "planificador_semana",
                column: "hogar_id");

            // ─── planificador_item ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "planificador_item",
                columns: table => new
                {
                    id           = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    semana_id    = table.Column<Guid>(type: "uuid", nullable: false),
                    tarea_id     = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha        = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_comida  = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receta_id    = table.Column<Guid>(type: "uuid", nullable: true),
                    titulo_libre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    imagen_url   = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hora         = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    orden        = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    creado_por   = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at   = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("planificador_item_pkey", x => x.id);
                    table.ForeignKey(
                        name: "planificador_item_semana_id_fkey",
                        column: x => x.semana_id,
                        principalTable: "planificador_semana",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "planificador_item_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "planificador_item_tarea_id_fkey",
                        column: x => x.tarea_id,
                        principalTable: "tareas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "planificador_item_creado_por_fkey",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_planificador_item_semana",
                table: "planificador_item",
                column: "semana_id");

            migrationBuilder.CreateIndex(
                name: "idx_planificador_item_fecha",
                table: "planificador_item",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "idx_planificador_item_tarea",
                table: "planificador_item",
                column: "tarea_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "planificador_item");
            migrationBuilder.DropTable(name: "planificador_semana");
            migrationBuilder.DropTable(name: "ubicaciones_catalogo");
            migrationBuilder.DropTable(name: "unidades_medida");
        }
    }
}
