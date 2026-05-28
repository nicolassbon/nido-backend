using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

public partial class NidoDbContext : DbContext
{
    public NidoDbContext(DbContextOptions<NidoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AsignacionesTarea> AsignacionesTareas { get; set; }

    public virtual DbSet<CategoriasProducto> CategoriasProductos { get; set; }

    public virtual DbSet<Electrodomestico> Electrodomesticos { get; set; }

    public virtual DbSet<Gasto> Gastos { get; set; }

    public virtual DbSet<Hogare> Hogares { get; set; }

    public virtual DbSet<InfoNutricionalProducto> InfoNutricionalProductos { get; set; }

    public virtual DbSet<InfoNutricionalRecetum> InfoNutricionalReceta { get; set; }

    public virtual DbSet<IngredientesRecetum> IngredientesReceta { get; set; }

    public virtual DbSet<InvitacionesHogar> InvitacionesHogars { get; set; }

    public virtual DbSet<ListaCompra> ListaCompras { get; set; }

    public virtual DbSet<Logro> Logros { get; set; }

    public virtual DbSet<LogrosHogar> LogrosHogars { get; set; }

    public virtual DbSet<LogrosUsuario> LogrosUsuarios { get; set; }

    public virtual DbSet<MiembrosHogar> MiembrosHogars { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Receta> Recetas { get; set; }

    public virtual DbSet<RecetaElectrodomestico> RecetaElectrodomesticos { get; set; }

    public virtual DbSet<RecetasCocinada> RecetasCocinadas { get; set; }

    public virtual DbSet<OnboardingState> OnboardingStates { get; set; }

    public virtual DbSet<OnboardingGoal> OnboardingGoals { get; set; }

    public virtual DbSet<ReseniasRecetum> ReseniasReceta { get; set; }

    public virtual DbSet<RestriccionesUsuario> RestriccionesUsuarios { get; set; }

    public virtual DbSet<StockHogar> StockHogars { get; set; }

    public virtual DbSet<Tarea> Tareas { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AsignacionesTarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("asignaciones_tarea_pkey");

            entity.ToTable("asignaciones_tarea");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.FechaAsignacion)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_asignacion");
            entity.Property(e => e.TareaId).HasColumnName("tarea_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Tarea).WithMany(p => p.AsignacionesTareas)
                .HasForeignKey(d => d.TareaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asignaciones_tarea_tarea_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.AsignacionesTareas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asignaciones_tarea_usuario_id_fkey");
        });

        modelBuilder.Entity<CategoriasProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categorias_producto_pkey");

            entity.ToTable("categorias_producto");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.TtlDias).HasColumnName("ttl_dias");
        });

        modelBuilder.Entity<Electrodomestico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("electrodomesticos_pkey");

            entity.ToTable("electrodomesticos");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(100)
                .HasColumnName("estado");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .HasColumnName("tipo");

            entity.HasOne(d => d.Hogar).WithMany(p => p.Electrodomesticos)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("electrodomesticos_hogar_id_fkey");
        });

        modelBuilder.Entity<Gasto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gastos_pkey");

            entity.ToTable("gastos");

            entity.HasIndex(e => e.HogarId, "idx_gastos_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Categoria)
                .HasMaxLength(100)
                .HasColumnName("categoria");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .HasColumnName("descripcion");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Monto)
                .HasPrecision(10, 2)
                .HasColumnName("monto");
            entity.Property(e => e.PagadoPor).HasColumnName("pagado_por");

            entity.HasOne(d => d.Hogar).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gastos_hogar_id_fkey");

            entity.HasOne(d => d.PagadoPorNavigation).WithMany(p => p.Gastos)
                .HasForeignKey(d => d.PagadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gastos_pagado_por_fkey");
        });

        modelBuilder.Entity<Hogare>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hogares_pkey");

            entity.ToTable("hogares");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<InfoNutricionalProducto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("info_nutricional_producto_pkey");

            entity.ToTable("info_nutricional_producto");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Calorias)
                .HasPrecision(10, 2)
                .HasColumnName("calorias");
            entity.Property(e => e.Carbohidratos)
                .HasPrecision(10, 2)
                .HasColumnName("carbohidratos");
            entity.Property(e => e.Grasas)
                .HasPrecision(10, 2)
                .HasColumnName("grasas");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.Proteinas)
                .HasPrecision(10, 2)
                .HasColumnName("proteinas");

            entity.HasOne(d => d.Producto).WithMany(p => p.InfoNutricionalProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("info_nutricional_producto_producto_id_fkey");
        });

        modelBuilder.Entity<InfoNutricionalRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("info_nutricional_receta_pkey");

            entity.ToTable("info_nutricional_receta");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Calorias)
                .HasPrecision(10, 2)
                .HasColumnName("calorias");
            entity.Property(e => e.Carbohidratos)
                .HasPrecision(10, 2)
                .HasColumnName("carbohidratos");
            entity.Property(e => e.Grasas)
                .HasPrecision(10, 2)
                .HasColumnName("grasas");
            entity.Property(e => e.Proteinas)
                .HasPrecision(10, 2)
                .HasColumnName("proteinas");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");

            entity.HasOne(d => d.Receta).WithMany(p => p.InfoNutricionalReceta)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("info_nutricional_receta_receta_id_fkey");
        });

        modelBuilder.Entity<IngredientesRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ingredientes_receta_pkey");

            entity.ToTable("ingredientes_receta");

            entity.HasIndex(e => e.RecetaId, "idx_ingredientes_receta");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(10, 2)
                .HasColumnName("cantidad");
            entity.Property(e => e.NombreIngrediente)
                .HasMaxLength(255)
                .HasColumnName("nombre_ingrediente");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.Unidad)
                .HasMaxLength(100)
                .HasColumnName("unidad");

            entity.HasOne(d => d.Producto).WithMany(p => p.IngredientesReceta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ingredientes_receta_producto_id_fkey");

            entity.HasOne(d => d.Receta).WithMany(p => p.IngredientesReceta)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ingredientes_receta_receta_id_fkey");
        });

        modelBuilder.Entity<InvitacionesHogar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invitaciones_hogar_pkey");

            entity.ToTable("invitaciones_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Codigo)
                .HasMaxLength(255)
                .HasColumnName("codigo");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Estado)
                .HasMaxLength(100)
                .HasColumnName("estado");
            entity.Property(e => e.ExpiraEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expira_en");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.InvitadoPor).HasColumnName("invitado_por");
            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .HasColumnName("token");

            entity.HasOne(d => d.Hogar).WithMany(p => p.InvitacionesHogars)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invitaciones_hogar_hogar_id_fkey");

            entity.HasOne(d => d.InvitadoPorNavigation).WithMany(p => p.InvitacionesHogars)
                .HasForeignKey(d => d.InvitadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invitaciones_hogar_invitado_por_fkey");
        });

        modelBuilder.Entity<ListaCompra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lista_compras_pkey");

            entity.ToTable("lista_compras");

            entity.HasIndex(e => e.HogarId, "idx_lista_compras_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AgregadoAlInventario)
                .HasDefaultValue(false)
                .HasColumnName("agregado_al_inventario");
            entity.Property(e => e.AgregadoPor).HasColumnName("agregado_por");
            entity.Property(e => e.Cantidad)
                .HasPrecision(10, 2)
                .HasColumnName("cantidad");
            entity.Property(e => e.Comprado)
                .HasDefaultValue(false)
                .HasColumnName("comprado");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.Unidad)
                .HasMaxLength(100)
                .HasColumnName("unidad");

            entity.HasOne(d => d.AgregadoPorNavigation).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.AgregadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lista_compras_agregado_por_fkey");

            entity.HasOne(d => d.Hogar).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lista_compras_hogar_id_fkey");

            entity.HasOne(d => d.Producto).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lista_compras_producto_id_fkey");
        });

        modelBuilder.Entity<Logro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("logros_pkey");

            entity.ToTable("logros");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.IconoUrl)
                .HasMaxLength(500)
                .HasColumnName("icono_url");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<LogrosHogar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("logros_hogar_pkey");

            entity.ToTable("logros_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.LogroId).HasColumnName("logro_id");

            entity.HasOne(d => d.Hogar).WithMany(p => p.LogrosHogars)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("logros_hogar_hogar_id_fkey");

            entity.HasOne(d => d.Logro).WithMany(p => p.LogrosHogars)
                .HasForeignKey(d => d.LogroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("logros_hogar_logro_id_fkey");
        });

        modelBuilder.Entity<LogrosUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("logros_usuario_pkey");

            entity.ToTable("logros_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.FechaObtenido)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_obtenido");
            entity.Property(e => e.LogroId).HasColumnName("logro_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Logro).WithMany(p => p.LogrosUsuarios)
                .HasForeignKey(d => d.LogroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("logros_usuario_logro_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.LogrosUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("logros_usuario_usuario_id_fkey");
        });

        modelBuilder.Entity<MiembrosHogar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("miembros_hogar_pkey");

            entity.ToTable("miembros_hogar");

            entity.HasIndex(e => e.HogarId, "idx_miembros_hogar_hogar");

            entity.HasIndex(e => e.UsuarioId, "idx_miembros_hogar_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Puntos)
                .HasDefaultValue(0)
                .HasColumnName("puntos");
            entity.Property(e => e.Rol)
                .HasMaxLength(100)
                .HasColumnName("rol");
            entity.Property(e => e.NombreRepresentado)
                .HasMaxLength(255)
                .HasColumnName("nombre_representado");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Hogar).WithMany(p => p.MiembrosHogars)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("miembros_hogar_hogar_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MiembrosHogars)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("miembros_hogar_usuario_id_fkey");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notificaciones_pkey");

            entity.ToTable("notificaciones");

            entity.HasIndex(e => e.UsuarioId, "idx_notificaciones_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Leida)
                .HasDefaultValue(false)
                .HasColumnName("leida");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.ReferenciaId).HasColumnName("referencia_id");
            entity.Property(e => e.ReferenciaTipo)
                .HasMaxLength(100)
                .HasColumnName("referencia_tipo");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .HasColumnName("tipo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notificaciones_usuario_id_fkey");
        });

        modelBuilder.Entity<OnboardingGoal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("onboarding_goals_pkey");
            entity.ToTable("onboarding_goals");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Titulo).HasMaxLength(255).HasColumnName("titulo");
            entity.Property(e => e.Descripcion).HasMaxLength(500).HasColumnName("descripcion");
            entity.HasOne(d => d.Hogar).WithMany(p => p.OnboardingGoals)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("onboarding_goals_hogar_id_fkey");
        });

        modelBuilder.Entity<OnboardingState>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("onboarding_state_pkey");
            entity.ToTable("onboarding_state");
            entity.HasIndex(e => new { e.UsuarioId, e.HogarId }, "ux_onboarding_state_usuario_hogar").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Step1CompletedAt).HasColumnType("timestamp without time zone").HasColumnName("step1_completed_at");
            entity.Property(e => e.Step2CompletedAt).HasColumnType("timestamp without time zone").HasColumnName("step2_completed_at");
            entity.Property(e => e.Step2Skipped).HasColumnName("step2_skipped");
            entity.Property(e => e.Step3CompletedAt).HasColumnType("timestamp without time zone").HasColumnName("step3_completed_at");
            entity.Property(e => e.Step3Skipped).HasColumnName("step3_skipped");
            entity.Property(e => e.Step4CompletedAt).HasColumnType("timestamp without time zone").HasColumnName("step4_completed_at");
            entity.Property(e => e.Step4Skipped).HasColumnName("step4_skipped");
            entity.Property(e => e.UpdatedAt).HasColumnType("timestamp without time zone").HasColumnName("updated_at");
            entity.HasOne(d => d.Usuario).WithMany(p => p.OnboardingStates)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("onboarding_state_usuario_id_fkey");
            entity.HasOne(d => d.Hogar).WithMany(p => p.OnboardingStates)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("onboarding_state_hogar_id_fkey");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("productos_pkey");

            entity.ToTable("productos");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(255)
                .HasColumnName("codigo_barras");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CategoriaId)
                .HasConstraintName("productos_categoria_id_fkey");
        });

        modelBuilder.Entity<Receta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recetas_pkey");

            entity.ToTable("recetas");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Dificultad)
                .HasMaxLength(100)
                .HasColumnName("dificultad");
            entity.Property(e => e.FuenteId)
                .HasMaxLength(255)
                .HasColumnName("fuente_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.Porciones).HasColumnName("porciones");
            entity.Property(e => e.TiempoCoccionMin).HasColumnName("tiempo_coccion_min");
        });

        modelBuilder.Entity<RecetaElectrodomestico>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("receta_electrodomestico_pkey");

            entity.ToTable("receta_electrodomestico");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.TipoRequerido)
                .HasMaxLength(100)
                .HasColumnName("tipo_requerido");

            entity.HasOne(d => d.Receta).WithMany(p => p.RecetaElectrodomesticos)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("receta_electrodomestico_receta_id_fkey");
        });

        modelBuilder.Entity<RecetasCocinada>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recetas_cocinadas_pkey");

            entity.ToTable("recetas_cocinadas");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CocinadoPor).HasColumnName("cocinado_por");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.PorcionesCocinadas).HasColumnName("porciones_cocinadas");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");

            entity.HasOne(d => d.CocinadoPorNavigation).WithMany(p => p.RecetasCocinada)
                .HasForeignKey(d => d.CocinadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recetas_cocinadas_cocinado_por_fkey");

            entity.HasOne(d => d.Hogar).WithMany(p => p.RecetasCocinada)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recetas_cocinadas_hogar_id_fkey");

            entity.HasOne(d => d.Receta).WithMany(p => p.RecetasCocinada)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recetas_cocinadas_receta_id_fkey");
        });

        modelBuilder.Entity<ReseniasRecetum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("resenias_receta_pkey");

            entity.ToTable("resenias_receta");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Comentario).HasColumnName("comentario");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Puntuacion).HasColumnName("puntuacion");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Receta).WithMany(p => p.ReseniasReceta)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resenias_receta_receta_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ReseniasReceta)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("resenias_receta_usuario_id_fkey");
        });

        modelBuilder.Entity<RestriccionesUsuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("restricciones_usuario_pkey");

            entity.ToTable("restricciones_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .HasColumnName("descripcion");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .HasColumnName("tipo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.RestriccionesUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("restricciones_usuario_usuario_id_fkey");
        });

        modelBuilder.Entity<StockHogar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_hogar_pkey");

            entity.ToTable("stock_hogar");

            entity.HasIndex(e => e.HogarId, "idx_stock_hogar_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CantidadActual)
                .HasPrecision(10, 2)
                .HasColumnName("cantidad_actual");
            entity.Property(e => e.CargadoPor).HasColumnName("cargado_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(100)
                .HasColumnName("unidad_medida");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.Ubicacion)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Alacena")
                .HasColumnName("ubicacion");
            entity.Property(e => e.EstaAbierto)
                .HasDefaultValue(false)
                .HasColumnName("esta_abierto");
            entity.Property(e => e.PorcentajeConsumido)
                .HasPrecision(5, 2)
                .HasDefaultValue(0m)
                .HasColumnName("porcentaje_consumido");

            entity.HasOne(d => d.CargadoPorNavigation).WithMany(p => p.StockHogarCargadoPorNavigations)
                .HasForeignKey(d => d.CargadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_hogar_cargado_por_fkey");

            entity.HasOne(d => d.Hogar).WithMany(p => p.StockHogars)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_hogar_hogar_id_fkey");

            entity.HasOne(d => d.Producto).WithMany(p => p.StockHogars)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_hogar_producto_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.StockHogarUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_hogar_updated_by_fkey");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tareas_pkey");

            entity.ToTable("tareas");

            entity.HasIndex(e => e.HogarId, "idx_tareas_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompletadoPor).HasColumnName("completado_por");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Estado)
                .HasMaxLength(100)
                .HasColumnName("estado");
            entity.Property(e => e.FechaCompletado)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_completado");
            entity.Property(e => e.FechaLimite)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_limite");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Titulo)
                .HasMaxLength(255)
                .HasColumnName("titulo");

            entity.HasOne(d => d.CompletadoPorNavigation).WithMany(p => p.TareaCompletadoPorNavigations)
                .HasForeignKey(d => d.CompletadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tareas_completado_por_fkey");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.TareaCreadoPorNavigations)
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tareas_creado_por_fkey");

            entity.HasOne(d => d.Hogar).WithMany(p => p.Tareas)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tareas_hogar_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Email, "usuarios_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.OauthId)
                .HasMaxLength(255)
                .HasColumnName("oauth_id");
            entity.Property(e => e.OauthProvider)
                .HasMaxLength(100)
                .HasColumnName("oauth_provider");
            entity.Property(e => e.Sexo)
                .HasMaxLength(30)
                .HasColumnName("sexo");
            entity.Property(e => e.FotoUrl)
                .HasMaxLength(500)
                .HasColumnName("foto_url");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
