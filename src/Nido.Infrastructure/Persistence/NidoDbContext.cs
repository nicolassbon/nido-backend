using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Persistence;

public partial class NidoDbContext : DbContext
{
    private readonly IServiceProvider? _serviceProvider;

    public NidoDbContext(DbContextOptions<NidoDbContext> options, IServiceProvider? serviceProvider = null)
        : base(options)
    {
        _serviceProvider = serviceProvider;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var addedNotifications = ChangeTracker.Entries<Notificacione>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (addedNotifications.Count > 0 && _serviceProvider != null)
        {
            foreach (var notif in addedNotifications)
            {
                var title = notif.Tipo switch
                {
                    "tarea_vencida" => "Tarea Vencida",
                    "producto_vencido" => "Producto Vencido",
                    "producto_por_vencer" => "Producto por Vencer",
                    "stock_bajo" => "Stock Bajo",
                    "asignacion_tarea" => "Aviso de tareas asignadas",
                    _ => "Nueva Notificación"
                };

                var redirectUrl = notif.ReferenciaTipo switch
                {
                    "tarea" when notif.ReferenciaId.HasValue => $"/tareas?taskId={notif.ReferenciaId.Value}",
                    "tarea" => "/tareas",
                    "alacena" when notif.ReferenciaId.HasValue => $"/alacena/{notif.ReferenciaId.Value}",
                    "alacena" => "/alacena",
                    _ => "/"
                };

                // Capture required values before starting the background thread
                var targetUsuarioId = notif.UsuarioId;
                var targetMensaje = notif.Mensaje ?? string.Empty;
                var targetTipo = notif.Tipo ?? string.Empty;
                var targetRedirectUrl = redirectUrl;

                // Send push notification in background using an isolated DI scope
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var pushService = scope.ServiceProvider.GetRequiredService<Nido.Application.Common.Notifications.IPushNotificationService>();
                            await pushService.SendNotificationAsync(targetUsuarioId, title, targetMensaje, redirectUrl, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending background push notification: {ex.Message}");
                    }
                }, CancellationToken.None);

                // Send Telegram notification in background if it matches target types
                if (targetTipo == "producto_vencido" || targetTipo == "producto_por_vencer" || targetTipo == "stock_bajo")
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

                                // Fetch active Telegram chat links for the user where they are still a member of the Hogar
                                var activeLinks = await context.TelegramChatLinks
                                    .AsNoTracking()
                                    .Where(l => l.UsuarioId == targetUsuarioId && l.UnpairedAt == null)
                                    .Join(context.MiembrosHogars.AsNoTracking(),
                                          l => new { l.UsuarioId, l.HogarId },
                                          m => new { m.UsuarioId, m.HogarId },
                                          (l, m) => new { l.HogarId, l.ChatId })
                                    .ToListAsync();

                                if (activeLinks.Count > 0)
                                {
                                    var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                                    var frontendBaseUrl = config["Frontend:BaseUrl"] ?? "http://localhost:4200";
                                    var fullUrl = $"{frontendBaseUrl.TrimEnd('/')}{targetRedirectUrl}";

                                    var escapedMessage = System.Net.WebUtility.HtmlEncode(targetMensaje);
                                    var escapedUrl = System.Net.WebUtility.HtmlEncode(fullUrl);
                                    var formattedText = $"{escapedMessage}\n\n👉 <a href=\"{escapedUrl}\">Ver en Nido</a>";

                                    var batcher = scope.ServiceProvider.GetRequiredService<Nido.Application.Telegram.Messaging.ITelegramNotificationBatcher>();
                                    var payloadJson = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        text = formattedText,
                                        parse_mode = "HTML"
                                    });

                                    foreach (var link in activeLinks)
                                    {
                                        await batcher.EnqueueEventAsync(
                                            link.HogarId,
                                            link.ChatId,
                                            targetTipo,
                                            payloadJson,
                                            isCritical: false,
                                            CancellationToken.None);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending background Telegram notification: {ex.Message}");
                        }
                    }, CancellationToken.None);
                }
            }
        }

        return result;
    }

    public virtual DbSet<AsignacionesTarea> AsignacionesTareas { get; set; }

    public virtual DbSet<CategoriasProducto> CategoriasProductos { get; set; }

    public virtual DbSet<Electrodomestico> Electrodomesticos { get; set; }

    public virtual DbSet<ElectrodomesticoCatalogo> ElectrodomesticosCatalogo { get; set; }

    public virtual DbSet<PasoReceta> PasosReceta { get; set; }

    public virtual DbSet<Factura> Facturas { get; set; }

    public virtual DbSet<Gasto> Gastos { get; set; }

    public virtual DbSet<Hogare> Hogares { get; set; }

    public virtual DbSet<InfoNutricionalProducto> InfoNutricionalProductos { get; set; }

    public virtual DbSet<InfoNutricionalProductoDetalle> InfoNutricionalProductoDetalles { get; set; }

    public virtual DbSet<InfoNutricionalRecetum> InfoNutricionalReceta { get; set; }

    public virtual DbSet<IngredientesRecetum> IngredientesReceta { get; set; }

    public virtual DbSet<InvitacionesHogar> InvitacionesHogars { get; set; }

    public virtual DbSet<ListaCompra> ListaCompras { get; set; }

    public virtual DbSet<ListaCompraHogar> ListasCompraHogar { get; set; }

    public virtual DbSet<Logro> Logros { get; set; }

    public virtual DbSet<LogrosHogar> LogrosHogars { get; set; }

    public virtual DbSet<LogrosUsuario> LogrosUsuarios { get; set; }

    public virtual DbSet<MiembrosHogar> MiembrosHogars { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Receta> Recetas { get; set; }

    public virtual DbSet<RecetaElectrodomestico> RecetaElectrodomesticos { get; set; }

    public virtual DbSet<RecetasCocinada> RecetasCocinadas { get; set; }

    public virtual DbSet<RecetaGuardadaHogar> RecetasGuardadasHogar { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<OnboardingState> OnboardingStates { get; set; }

    public virtual DbSet<OnboardingGoal> OnboardingGoals { get; set; }

    public virtual DbSet<ReseniasRecetum> ReseniasReceta { get; set; }

    public virtual DbSet<RestriccionesUsuario> RestriccionesUsuarios { get; set; }

    public virtual DbSet<RestriccionesCatalogo> RestriccionesCatalogo { get; set; }

    public virtual DbSet<MetasCatalogo> MetasCatalogo { get; set; }

    public virtual DbSet<HogarMeta> HogarMetas { get; set; }

    public virtual DbSet<Entities.StockHogar> StockHogars { get; set; }

    public virtual DbSet<Tarea> Tareas { get; set; }

    public virtual DbSet<PresupuestoMensual> PresupuestosMensuales { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<SuscripcionPush> SuscripcionesPush { get; set; }

    public virtual DbSet<UnidadMedida> UnidadesMedida { get; set; }

    public virtual DbSet<UbicacionCatalogo> UbicacionesCatalogo { get; set; }

    public virtual DbSet<PlanificadorSemana> PlanificadorSemanas { get; set; }

    public virtual DbSet<PlanificadorItem> PlanificadorItems { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<PasoReceta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pasos_receta_pkey");

            entity.ToTable("pasos_receta");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");

            entity.Property(e => e.RecetaId)
                .HasColumnName("receta_id");

            entity.Property(e => e.Orden)
                .HasColumnName("orden");

            entity.Property(e => e.Descripcion)
                .HasColumnName("descripcion");

            entity.HasOne(e => e.Receta)
                .WithMany(r => r.PasosReceta)
                .HasForeignKey(e => e.RecetaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("pasos_receta_receta_id_fkey");
        });



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
            entity.Property(e => e.IconoSvg)
                .HasMaxLength(255)
                .HasColumnName("icono_svg");
            entity.Property(e => e.Icono)
                .HasMaxLength(255)
                .HasColumnName("icono");
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
            entity.Property(e => e.CatalogoId).HasColumnName("catalogo_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.Tipo)
                .HasMaxLength(100)
                .HasColumnName("tipo");
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .HasColumnName("marca");

            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");

            entity.HasOne(d => d.Hogar).WithMany(p => p.Electrodomesticos)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("electrodomesticos_hogar_id_fkey");

            entity.HasOne(d => d.Catalogo).WithMany(p => p.Electrodomesticos)
                .HasForeignKey(d => d.CatalogoId)
                .HasConstraintName("electrodomesticos_catalogo_id_fkey");

        });

        modelBuilder.Entity<ElectrodomesticoCatalogo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("electrodomesticos_catalogo_pkey");

            entity.ToTable("electrodomesticos_catalogo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Tipo)
                .HasMaxLength(80)
                .HasColumnName("tipo");
            entity.Property(e => e.Icono)
                .HasMaxLength(80)
                .HasColumnName("icono");
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
            entity.Property(e => e.Orden).HasColumnName("orden");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("facturas_pkey");
            entity.ToTable("facturas");
            entity.HasIndex(e => e.HogarId, "idx_facturas_hogar");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.Nombre).HasMaxLength(255).HasColumnName("nombre");
            entity.Property(e => e.Tipo).HasMaxLength(50).HasColumnName("tipo");
            entity.Property(e => e.Monto).HasPrecision(10, 2).HasColumnName("monto");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.ArchivoStorageKey).HasMaxLength(500).HasColumnName("archivo_storage_key");
            entity.Property(e => e.Pagada).HasDefaultValue(false).HasColumnName("pagada");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.HasOne(d => d.Hogar).WithMany()
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("facturas_hogar_id_fkey");
            entity.HasOne(d => d.CreadoPorNavigation).WithMany()
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("facturas_creado_por_fkey");
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
            entity.Property(e => e.FacturaId).HasColumnName("factura_id").IsRequired(false);

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
            entity.Property(e => e.ModoAhorro)
                .HasDefaultValue(false)
                .HasColumnName("modo_ahorro");
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
            entity.Property(e => e.Porcion)
                .HasMaxLength(100)
                .HasColumnName("porcion");
            entity.Property(e => e.Base)
                .HasMaxLength(100)
                .HasColumnName("base");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.Proteinas)
                .HasPrecision(10, 2)
                .HasColumnName("proteinas");

            entity.HasOne(d => d.Producto).WithMany(p => p.InfoNutricionalProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("info_nutricional_producto_producto_id_fkey");
        });

        modelBuilder.Entity<InfoNutricionalProductoDetalle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("info_nutricional_producto_detalle_pkey");

            entity.ToTable("info_nutricional_producto_detalle");

            entity.HasIndex(e => e.InfoNutricionalProductoId, "idx_info_nutricional_producto_detalle_info_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.InfoNutricionalProductoId).HasColumnName("info_nutricional_producto_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("nombre");
            entity.Property(e => e.Valor)
                .HasPrecision(10, 2)
                .HasColumnName("valor");
            entity.Property(e => e.Unidad)
                .HasMaxLength(30)
                .HasColumnName("unidad");
            entity.Property(e => e.PorcentajeDiario)
                .HasPrecision(6, 2)
                .HasColumnName("porcentaje_diario");
            entity.Property(e => e.Orden).HasColumnName("orden");

            entity.HasOne(d => d.InfoNutricionalProducto).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.InfoNutricionalProductoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("info_nutricional_producto_detalle_info_id_fkey");
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
            entity.Property(e => e.EmailInvitado)
                .HasMaxLength(255)
                .HasColumnName("email_invitado");

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
            entity.Property(e => e.CompradoEn)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("comprado_en");
            entity.Property(e => e.CompradoPor).HasColumnName("comprado_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GrupoNombre)
                .HasMaxLength(255)
                .HasDefaultValue("Productos agregados")
                .HasColumnName("grupo_nombre");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.ListaId).HasColumnName("lista_id");
            entity.Property(e => e.NombreManual)
                .HasMaxLength(255)
                .HasColumnName("nombre_manual");
            entity.Property(e => e.Orden)
                .HasDefaultValue(0)
                .HasColumnName("orden");
            entity.Property(e => e.ProductoId).HasColumnName("producto_id");
            entity.Property(e => e.ProductoNombreSnapshot)
                .HasMaxLength(255)
                .HasDefaultValue("")
                .HasColumnName("producto_nombre_snapshot");
            entity.Property(e => e.RemovidoDeListaAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("removido_de_lista_at");
            entity.Property(e => e.Unidad)
                .HasMaxLength(100)
                .HasColumnName("unidad");

            entity.HasOne(d => d.AgregadoPorNavigation).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.AgregadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lista_compras_agregado_por_fkey");

            entity.HasOne(d => d.CompradoPorNavigation).WithMany()
                .HasForeignKey(d => d.CompradoPor)
                .HasConstraintName("lista_compras_comprado_por_fkey");

            entity.HasOne(d => d.Hogar).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lista_compras_hogar_id_fkey");

            entity.HasOne(d => d.Lista).WithMany(p => p.Items)
                .HasForeignKey(d => d.ListaId)
                .HasConstraintName("lista_compras_lista_id_fkey");

            entity.HasOne(d => d.Producto).WithMany(p => p.ListaCompras)
                .HasForeignKey(d => d.ProductoId)
                .HasConstraintName("lista_compras_producto_id_fkey");
        });

        modelBuilder.Entity<ListaCompraHogar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("listas_compra_hogar_pkey");

            entity.ToTable("listas_compra_hogar");

            entity.HasIndex(e => e.HogarId, "idx_listas_compra_hogar_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
            entity.Property(e => e.CreadaPor).HasColumnName("creada_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Hogar).WithMany(p => p.ListasCompraHogar)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("listas_compra_hogar_hogar_id_fkey");

            entity.HasOne(d => d.CreadaPorNavigation).WithMany()
                .HasForeignKey(d => d.CreadaPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("listas_compra_hogar_creada_por_fkey");
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
            entity.Property(e => e.CantidadCompraEstandar)
                .HasPrecision(10, 2)
                .HasColumnName("cantidad_compra_estandar");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.UnidadCompraEstandar)
                .HasMaxLength(50)
                .HasColumnName("unidad_compra_estandar");

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
            entity.Property(e => e.ImagenUrl)
                .HasMaxLength(500)
                .HasColumnName("imagen_url");
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
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.Puntuacion).HasColumnName("puntuacion");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            // 1 reseña por (receta, usuario)
            entity.HasIndex(e => new { e.RecetaId, e.UsuarioId })
                .IsUnique()
                .HasDatabaseName("resenias_receta_receta_id_usuario_id_key");

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
            entity.HasKey(e => new { e.UsuarioId, e.RestriccionId }).HasName("restricciones_usuario_pkey");

            entity.ToTable("restricciones_usuario");

            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.RestriccionId).HasColumnName("restriccion_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.RestriccionesUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("restricciones_usuario_usuario_id_fkey");

            entity.HasOne(d => d.Restriccion).WithMany(p => p.RestriccionesUsuarios)
                .HasForeignKey(d => d.RestriccionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("restricciones_usuario_restriccion_id_fkey");
        });

        modelBuilder.Entity<RestriccionesCatalogo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("restricciones_catalogo_pkey");
            entity.ToTable("restricciones_catalogo");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
            entity.Property(e => e.Tipo).HasMaxLength(50).HasColumnName("tipo");
        });

        modelBuilder.Entity<MetasCatalogo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("metas_catalogo_pkey");
            entity.ToTable("metas_catalogo");
            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
        });

        modelBuilder.Entity<HogarMeta>(entity =>
        {
            entity.HasKey(e => new { e.HogarId, e.MetaId }).HasName("hogar_metas_pkey");
            entity.ToTable("hogar_metas");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.MetaId).HasColumnName("meta_id");

            entity.HasOne(d => d.Hogar).WithMany(p => p.HogarMetas)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("hogar_metas_hogar_id_fkey");

            entity.HasOne(d => d.Meta).WithMany(p => p.HogarMetas)
                .HasForeignKey(d => d.MetaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("hogar_metas_meta_id_fkey");
        });

        modelBuilder.Entity<Entities.StockHogar>(entity =>
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
            entity.Property(e => e.CantidadEnvases)
                .HasDefaultValue(1)
                .HasColumnName("cantidad_envases");
            entity.Property(e => e.OrigenCarga)
                .HasMaxLength(30)
                .HasDefaultValue("manual")
                .HasColumnName("origen_carga");

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

        modelBuilder.Entity<RecetaGuardadaHogar>(entity =>
        {
            entity.HasKey(e => new { e.HogarId, e.RecetaId }).HasName("recetas_guardadas_hogar_pkey");

            entity.ToTable("recetas_guardadas_hogar");

            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.GuardadaPor).HasColumnName("guardada_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Hogar).WithMany(p => p.RecetasGuardadasHogar)
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("recetas_guardadas_hogar_hogar_id_fkey");

            entity.HasOne(d => d.Receta).WithMany(p => p.RecetasGuardadasHogar)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("recetas_guardadas_hogar_receta_id_fkey");

            entity.HasOne(d => d.GuardadaPorNavigation).WithMany()
                .HasForeignKey(d => d.GuardadaPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recetas_guardadas_hogar_guardada_por_fkey");
        });

        modelBuilder.Entity<PresupuestoMensual>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("presupuestos_mensuales_pkey");
            entity.ToTable("presupuestos_mensuales");
            entity.HasIndex(e => new { e.HogarId, e.Anio, e.Mes }, "ux_presupuestos_hogar_anio_mes").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.Anio).HasColumnName("anio");
            entity.Property(e => e.Mes).HasColumnName("mes");
            entity.Property(e => e.Monto).HasPrecision(12, 2).HasColumnName("monto");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.HasOne(d => d.Hogar).WithMany()
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("presupuestos_mensuales_hogar_id_fkey");
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tareas_pkey");

            entity.ToTable("tareas");

            entity.HasIndex(e => e.HogarId, "idx_tareas_hogar");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CompletadoPor).HasColumnName("completado_por").IsRequired(false);
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
            entity.HasIndex(e => new { e.OauthProvider, e.OauthId }, "ux_usuarios_oauth_identity")
                .IsUnique()
                .HasFilter("oauth_provider IS NOT NULL AND oauth_id IS NOT NULL");

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
            entity.Property(e => e.Telefono)
                .HasMaxLength(50)
                .HasColumnName("telefono");
            entity.Property(e => e.FotoStorageKey)
                .HasMaxLength(512)
                .HasColumnName("foto_storage_key");
            entity.Property(e => e.FotoUpdatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("foto_updated_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.AlertaVencimientoDias)
                .HasDefaultValue(7)
                .HasColumnName("alerta_vencimiento_dias");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.ToTable("refresh_tokens");

            entity.HasIndex(e => e.TokenHash, "idx_refresh_tokens_hash");
            entity.HasIndex(e => e.UsuarioId, "idx_refresh_tokens_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("refresh_tokens_usuario_id_fkey");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_reset_tokens_pkey");

            entity.ToTable("password_reset_tokens");

            entity.HasIndex(e => e.TokenHash, "idx_password_reset_tokens_hash").IsUnique();
            entity.HasIndex(e => e.UsuarioId, "idx_password_reset_tokens_usuario");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires_at");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("used_at");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Usuario).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("password_reset_tokens_usuario_id_fkey");
        });

        modelBuilder.Entity<SuscripcionPush>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("suscripciones_push_pkey");

            entity.ToTable("suscripciones_push");

            entity.HasIndex(e => e.UsuarioId, "idx_suscripciones_push_usuario");

            entity.HasIndex(e => new { e.UsuarioId, e.Endpoint }, "ux_suscripciones_push_usuario_endpoint")
                .IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Endpoint).HasColumnName("endpoint");
            entity.Property(e => e.P256dh).HasColumnName("p256dh");
            entity.Property(e => e.Auth).HasColumnName("auth");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Usuario).WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("suscripciones_push_usuario_id_fkey");
        });

        modelBuilder.Entity<UnidadMedida>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("unidades_medida_pkey");
            entity.ToTable("unidades_medida");
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Codigo).HasMaxLength(20).HasColumnName("codigo");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
        });

        modelBuilder.Entity<UbicacionCatalogo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ubicaciones_catalogo_pkey");
            entity.ToTable("ubicaciones_catalogo");
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
            entity.Property(e => e.Icono).HasMaxLength(50).HasColumnName("icono");
            entity.Property(e => e.Color).HasMaxLength(20).HasColumnName("color");
        });

        modelBuilder.Entity<PlanificadorSemana>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("planificador_semana_pkey");
            entity.ToTable("planificador_semana");
            entity.HasIndex(e => e.HogarId, "idx_planificador_semana_hogar");
            entity.HasIndex(e => new { e.HogarId, e.FechaInicio }).IsUnique().HasDatabaseName("planificador_semana_hogar_fecha_key");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.HogarId).HasColumnName("hogar_id");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.HasOne(d => d.Hogar).WithMany()
                .HasForeignKey(d => d.HogarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("planificador_semana_hogar_id_fkey");
        });

        modelBuilder.Entity<PlanificadorItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("planificador_item_pkey");
            entity.ToTable("planificador_item");
            entity.HasIndex(e => e.SemanaId, "idx_planificador_item_semana");
            entity.HasIndex(e => e.Fecha, "idx_planificador_item_fecha");
            entity.HasIndex(e => e.TareaId, "idx_planificador_item_tarea");
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()").HasColumnName("id");
            entity.Property(e => e.SemanaId).HasColumnName("semana_id");
            entity.Property(e => e.TareaId).HasColumnName("tarea_id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.TipoComida).HasMaxLength(20).HasColumnName("tipo_comida");
            entity.Property(e => e.RecetaId).HasColumnName("receta_id");
            entity.Property(e => e.TituloLibre).HasMaxLength(255).HasColumnName("titulo_libre");
            entity.Property(e => e.ImagenUrl).HasMaxLength(500).HasColumnName("imagen_url");
            entity.Property(e => e.Hora).HasMaxLength(10).HasColumnName("hora");
            entity.Property(e => e.Orden).HasDefaultValue(0).HasColumnName("orden");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.HasOne(d => d.Semana).WithMany(p => p.Items)
                .HasForeignKey(d => d.SemanaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("planificador_item_semana_id_fkey");
            entity.HasOne(d => d.Receta).WithMany()
                .HasForeignKey(d => d.RecetaId)
                .HasConstraintName("planificador_item_receta_id_fkey");
            entity.HasOne(d => d.Tarea).WithMany(p => p.PlanificadorItems)
                .HasForeignKey(d => d.TareaId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("planificador_item_tarea_id_fkey");
            entity.HasOne(d => d.CreadoPorNavigation).WithMany()
                .HasForeignKey(d => d.CreadoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("planificador_item_creado_por_fkey");
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                        v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
                        v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)));
                }
            }
        }

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
