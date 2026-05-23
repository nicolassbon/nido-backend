using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nido.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "categorias_producto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ttl_dias = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categorias_producto_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hogares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("hogares_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "logros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    icono_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("logros_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recetas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    tiempo_coccion_min = table.Column<int>(type: "integer", nullable: true),
                    dificultad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    porciones = table.Column<int>(type: "integer", nullable: true),
                    fuente_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recetas_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    oauth_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    oauth_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("usuarios_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("productos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "productos_categoria_id_fkey",
                        column: x => x.categoria_id,
                        principalTable: "categorias_producto",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "electrodomesticos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    estado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("electrodomesticos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "electrodomesticos_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "logros_hogar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logro_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("logros_hogar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "logros_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "logros_hogar_logro_id_fkey",
                        column: x => x.logro_id,
                        principalTable: "logros",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "info_nutricional_receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calorias = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    proteinas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    carbohidratos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    grasas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("info_nutricional_receta_pkey", x => x.id);
                    table.ForeignKey(
                        name: "info_nutricional_receta_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "receta_electrodomestico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_requerido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("receta_electrodomestico_pkey", x => x.id);
                    table.ForeignKey(
                        name: "receta_electrodomestico_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "gastos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pagado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("gastos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "gastos_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "gastos_pagado_por_fkey",
                        column: x => x.pagado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "invitaciones_hogar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    estado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    expira_en = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("invitaciones_hogar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "invitaciones_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "invitaciones_hogar_invitado_por_fkey",
                        column: x => x.invitado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "logros_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    logro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_obtenido = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("logros_usuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "logros_usuario_logro_id_fkey",
                        column: x => x.logro_id,
                        principalTable: "logros",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "logros_usuario_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "miembros_hogar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    puntos = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("miembros_hogar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "miembros_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "miembros_hogar_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mensaje = table.Column<string>(type: "text", nullable: true),
                    leida = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    referencia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    referencia_tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notificaciones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notificaciones_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recetas_cocinadas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cocinado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    porciones_cocinadas = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recetas_cocinadas_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recetas_cocinadas_cocinado_por_fkey",
                        column: x => x.cocinado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recetas_cocinadas_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recetas_cocinadas_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "resenias_receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntuacion = table.Column<int>(type: "integer", nullable: true),
                    comentario = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("resenias_receta_pkey", x => x.id);
                    table.ForeignKey(
                        name: "resenias_receta_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "resenias_receta_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "restricciones_usuario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("restricciones_usuario_pkey", x => x.id);
                    table.ForeignKey(
                        name: "restricciones_usuario_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tareas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fecha_limite = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    completado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_completado = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("tareas_pkey", x => x.id);
                    table.ForeignKey(
                        name: "tareas_completado_por_fkey",
                        column: x => x.completado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "tareas_creado_por_fkey",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "tareas_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "info_nutricional_producto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calorias = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    proteinas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    carbohidratos = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    grasas = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("info_nutricional_producto_pkey", x => x.id);
                    table.ForeignKey(
                        name: "info_nutricional_producto_producto_id_fkey",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ingredientes_receta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    receta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_ingrediente = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    unidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ingredientes_receta_pkey", x => x.id);
                    table.ForeignKey(
                        name: "ingredientes_receta_producto_id_fkey",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "ingredientes_receta_receta_id_fkey",
                        column: x => x.receta_id,
                        principalTable: "recetas",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "lista_compras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agregado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    unidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comprado = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    agregado_al_inventario = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("lista_compras_pkey", x => x.id);
                    table.ForeignKey(
                        name: "lista_compras_agregado_por_fkey",
                        column: x => x.agregado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "lista_compras_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "lista_compras_producto_id_fkey",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_hogar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    hogar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cargado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_actual = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    unidad_medida = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ubicacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Alacena"),
                    esta_abierto = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    porcentaje_consumido = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_hogar_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stock_hogar_cargado_por_fkey",
                        column: x => x.cargado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_hogar_hogar_id_fkey",
                        column: x => x.hogar_id,
                        principalTable: "hogares",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_hogar_producto_id_fkey",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_hogar_updated_by_fkey",
                        column: x => x.updated_by,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "asignaciones_tarea",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    tarea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_asignacion = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("asignaciones_tarea_pkey", x => x.id);
                    table.ForeignKey(
                        name: "asignaciones_tarea_tarea_id_fkey",
                        column: x => x.tarea_id,
                        principalTable: "tareas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "asignaciones_tarea_usuario_id_fkey",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_tarea_tarea_id",
                table: "asignaciones_tarea",
                column: "tarea_id");

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_tarea_usuario_id",
                table: "asignaciones_tarea",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_electrodomesticos_hogar_id",
                table: "electrodomesticos",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "idx_gastos_hogar",
                table: "gastos",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_gastos_pagado_por",
                table: "gastos",
                column: "pagado_por");

            migrationBuilder.CreateIndex(
                name: "IX_info_nutricional_producto_producto_id",
                table: "info_nutricional_producto",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_info_nutricional_receta_receta_id",
                table: "info_nutricional_receta",
                column: "receta_id");

            migrationBuilder.CreateIndex(
                name: "idx_ingredientes_receta",
                table: "ingredientes_receta",
                column: "receta_id");

            migrationBuilder.CreateIndex(
                name: "IX_ingredientes_receta_producto_id",
                table: "ingredientes_receta",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitaciones_hogar_hogar_id",
                table: "invitaciones_hogar",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_invitaciones_hogar_invitado_por",
                table: "invitaciones_hogar",
                column: "invitado_por");

            migrationBuilder.CreateIndex(
                name: "idx_lista_compras_hogar",
                table: "lista_compras",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_lista_compras_agregado_por",
                table: "lista_compras",
                column: "agregado_por");

            migrationBuilder.CreateIndex(
                name: "IX_lista_compras_producto_id",
                table: "lista_compras",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_logros_hogar_hogar_id",
                table: "logros_hogar",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_logros_hogar_logro_id",
                table: "logros_hogar",
                column: "logro_id");

            migrationBuilder.CreateIndex(
                name: "IX_logros_usuario_logro_id",
                table: "logros_usuario",
                column: "logro_id");

            migrationBuilder.CreateIndex(
                name: "IX_logros_usuario_usuario_id",
                table: "logros_usuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_miembros_hogar_hogar",
                table: "miembros_hogar",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "idx_miembros_hogar_usuario",
                table: "miembros_hogar",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_notificaciones_usuario",
                table: "notificaciones",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_categoria_id",
                table: "productos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_receta_electrodomestico_receta_id",
                table: "receta_electrodomestico",
                column: "receta_id");

            migrationBuilder.CreateIndex(
                name: "IX_recetas_cocinadas_cocinado_por",
                table: "recetas_cocinadas",
                column: "cocinado_por");

            migrationBuilder.CreateIndex(
                name: "IX_recetas_cocinadas_hogar_id",
                table: "recetas_cocinadas",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_recetas_cocinadas_receta_id",
                table: "recetas_cocinadas",
                column: "receta_id");

            migrationBuilder.CreateIndex(
                name: "IX_resenias_receta_receta_id",
                table: "resenias_receta",
                column: "receta_id");

            migrationBuilder.CreateIndex(
                name: "IX_resenias_receta_usuario_id",
                table: "resenias_receta",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_restricciones_usuario_usuario_id",
                table: "restricciones_usuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_hogar_hogar",
                table: "stock_hogar",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_hogar_cargado_por",
                table: "stock_hogar",
                column: "cargado_por");

            migrationBuilder.CreateIndex(
                name: "IX_stock_hogar_producto_id",
                table: "stock_hogar",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_hogar_updated_by",
                table: "stock_hogar",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_tareas_hogar",
                table: "tareas",
                column: "hogar_id");

            migrationBuilder.CreateIndex(
                name: "IX_tareas_completado_por",
                table: "tareas",
                column: "completado_por");

            migrationBuilder.CreateIndex(
                name: "IX_tareas_creado_por",
                table: "tareas",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "usuarios_email_key",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asignaciones_tarea");

            migrationBuilder.DropTable(
                name: "electrodomesticos");

            migrationBuilder.DropTable(
                name: "gastos");

            migrationBuilder.DropTable(
                name: "info_nutricional_producto");

            migrationBuilder.DropTable(
                name: "info_nutricional_receta");

            migrationBuilder.DropTable(
                name: "ingredientes_receta");

            migrationBuilder.DropTable(
                name: "invitaciones_hogar");

            migrationBuilder.DropTable(
                name: "lista_compras");

            migrationBuilder.DropTable(
                name: "logros_hogar");

            migrationBuilder.DropTable(
                name: "logros_usuario");

            migrationBuilder.DropTable(
                name: "miembros_hogar");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "receta_electrodomestico");

            migrationBuilder.DropTable(
                name: "recetas_cocinadas");

            migrationBuilder.DropTable(
                name: "resenias_receta");

            migrationBuilder.DropTable(
                name: "restricciones_usuario");

            migrationBuilder.DropTable(
                name: "stock_hogar");

            migrationBuilder.DropTable(
                name: "tareas");

            migrationBuilder.DropTable(
                name: "logros");

            migrationBuilder.DropTable(
                name: "recetas");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "hogares");

            migrationBuilder.DropTable(
                name: "categorias_producto");
        }
    }
}
