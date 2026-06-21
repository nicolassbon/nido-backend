using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nido.Application.ListaCompras;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.ListaCompras;

public sealed class ListaComprasRepository : IListaComprasRepository
{
    private readonly NidoDbContext _db;

    public ListaComprasRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ListaCompraGrupoResult>> GetActiveAsync(Guid hogarId, CancellationToken ct)
    {
        var items = await QueryActive(hogarId)
            .AsNoTracking()
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.GrupoNombre)
            .ThenBy(item => item.Orden)
            .ToListAsync(ct);

        var map = await GetProductCategoryMapAsync(ct);
        return ToGroups(items, map);
    }

    public async Task<IReadOnlyList<ListaCompraListResult>> GetListsAsync(Guid hogarId, CancellationToken ct)
    {
        var listas = await _db.ListasCompraHogar
            .AsNoTracking()
            .Where(lista => lista.HogarId == hogarId)
            .Include(lista => lista.Items.Where(item => item.RemovidoDeListaAt == null && item.Comprado != true))
                .ThenInclude(item => item.Producto)
                    .ThenInclude(p => p!.Categoria)
            .OrderBy(lista => lista.CreatedAt)
            .ThenBy(lista => lista.Nombre)
            .ToListAsync(ct);

        var map = await GetProductCategoryMapAsync(ct);
        return listas.Select(lista => ToListResult(lista, map)).ToList();
    }

    public async Task<ListaCompraListResult> CreateListAsync(Guid hogarId, Guid usuarioId, string nombre, CancellationToken ct)
    {
        var lista = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            Nombre = nombre,
            CreadaPor = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ListasCompraHogar.Add(lista);
        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToListResult(lista, map);
    }

    public async Task<ListaCompraListResult?> UpdateListAsync(Guid hogarId, Guid listaId, string nombre, CancellationToken ct)
    {
        var lista = await _db.ListasCompraHogar
            .Include(l => l.Items.Where(item => item.RemovidoDeListaAt == null && item.Comprado != true))
                .ThenInclude(item => item.Producto)
                    .ThenInclude(p => p!.Categoria)
            .FirstOrDefaultAsync(lista => lista.Id == listaId && lista.HogarId == hogarId, ct);

        if (lista is null)
        {
            return null;
        }

        lista.Nombre = nombre;
        lista.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToListResult(lista, map);
    }

    public async Task<bool> DeleteListAsync(Guid hogarId, Guid listaId, CancellationToken ct)
    {
        var lista = await _db.ListasCompraHogar
            .Include(l => l.Items)
            .FirstOrDefaultAsync(lista => lista.Id == listaId && lista.HogarId == hogarId, ct);

        if (lista is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        foreach (var item in lista.Items)
        {
            if (item.RemovidoDeListaAt == null)
            {
                item.RemovidoDeListaAt = now;
            }

            item.ListaId = null;
        }

        _db.ListasCompraHogar.Remove(lista);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ListaCompraItemResult?> AddItemToListAsync(
        Guid hogarId,
        Guid listaId,
        Guid usuarioId,
        string nombre,
        decimal? cantidad,
        string? unidad,
        CancellationToken ct)
    {
        var exists = await _db.ListasCompraHogar.AnyAsync(lista => lista.Id == listaId && lista.HogarId == hogarId, ct);
        if (!exists)
        {
            return null;
        }

        var nextOrder = await QueryActive(hogarId)
            .Where(item => item.ListaId == listaId)
            .Select(item => (int?)item.Orden)
            .MaxAsync(ct) ?? -1;

        var item = new ListaCompra
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            ListaId = listaId,
            ProductoId = null,
            AgregadoPor = usuarioId,
            Cantidad = cantidad,
            Unidad = NormalizeOptional(unidad),
            Comprado = false,
            AgregadoAlInventario = false,
            GrupoNombre = ListaComprasDefaults.ManualGroupName,
            Orden = nextOrder + 1,
            CreatedAt = DateTime.UtcNow,
            NombreManual = nombre.Trim(),
            ProductoNombreSnapshot = nombre.Trim()
        };

        _db.ListaCompras.Add(item);
        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToItemResult(item, map);
    }

    public async Task<ListaCompraItemResult?> UpdateItemAsync(
        Guid hogarId,
        Guid listaId,
        Guid itemId,
        string? nombre,
        decimal? cantidad,
        string? unidad,
        bool? comprado,
        Guid usuarioId,
        CancellationToken ct)
    {
        var item = await QueryActive(hogarId)
            .FirstOrDefaultAsync(item => item.Id == itemId && item.ListaId == listaId, ct);

        if (item is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            item.NombreManual = nombre.Trim();
            item.ProductoNombreSnapshot = nombre.Trim();
            item.ProductoId = null;
            item.Producto = null;
        }

        item.Cantidad = cantidad;
        item.Unidad = NormalizeOptional(unidad);

        if (comprado.HasValue && comprado.Value != (item.Comprado == true))
        {
            if (comprado.Value)
            {
                MarkPurchased(item, usuarioId);
            }
            else
            {
                item.Comprado = false;
                item.CompradoEn = null;
                item.CompradoPor = null;
            }
        }

        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToItemResult(item, map);
    }

    public async Task<IReadOnlyList<ListaCompraHistorialItemResult>> GetHistorialAsync(Guid hogarId, CancellationToken ct)
    {
        var items = await _db.ListaCompras
            .AsNoTracking()
            .Where(item =>
                item.HogarId == hogarId &&
                item.Comprado == true &&
                item.CompradoEn != null)
            .Include(item => item.Producto)
                .ThenInclude(producto => producto!.Categoria)
            .OrderByDescending(item => item.CompradoEn)
            .ThenBy(item => item.ProductoNombreSnapshot)
            .ToListAsync(ct);

        var map = await GetProductCategoryMapAsync(ct);

        return items.Select(item => {
            var resolved = ResolveCategory(item, map);
            return new ListaCompraHistorialItemResult(
                item.Id,
                item.ProductoId,
                item.ProductoNombreSnapshot,
                item.Cantidad,
                item.Unidad,
                item.GrupoNombre,
                item.CompradoEn!.Value,
                item.CompradoPor,
                item.AgregadoAlInventario == true,
                resolved.CategoriaNombre,
                resolved.IconoSvg);
        }).ToList();
    }

    public async Task<IReadOnlyList<ListaCompraGrupoResult>> ReplaceGroupAsync(
        Guid hogarId,
        Guid usuarioId,
        string grupoNombre,
        IReadOnlyList<ListaCompraItemInput> items,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var existing = await _db.ListaCompras
            .Where(item => item.HogarId == hogarId &&
                           item.GrupoNombre == grupoNombre &&
                           item.RemovidoDeListaAt == null)
            .ToListAsync(ct);

        foreach (var item in existing)
        {
            item.RemovidoDeListaAt = now;
        }

        var lista = await EnsureDefaultListAsync(hogarId, usuarioId, ct);
        for (var i = 0; i < items.Count; i++)
        {
            var input = items[i];
            var producto = await GetOrCreateProductoAsync(input.Nombre, ct);
            _db.ListaCompras.Add(new ListaCompra
            {
                Id = Guid.NewGuid(),
                HogarId = hogarId,
                ListaId = lista.Id,
                ProductoId = producto.Id,
                Producto = producto,
                AgregadoPor = usuarioId,
                Cantidad = input.Cantidad,
                Unidad = NormalizeOptional(input.Unidad),
                Comprado = false,
                AgregadoAlInventario = false,
                GrupoNombre = grupoNombre,
                Orden = i,
                CreatedAt = now.AddTicks(i),
                NombreManual = producto.Nombre,
                ProductoNombreSnapshot = producto.Nombre
            });
        }

        await _db.SaveChangesAsync(ct);
        return await GetActiveAsync(hogarId, ct);
    }

    public async Task<ListaCompraItemResult> AddItemAsync(
        Guid hogarId,
        Guid usuarioId,
        string nombre,
        decimal? cantidad,
        string? unidad,
        string grupoNombre,
        CancellationToken ct)
    {
        var nextOrder = await QueryActive(hogarId)
            .Where(item => item.GrupoNombre == grupoNombre)
            .Select(item => (int?)item.Orden)
            .MaxAsync(ct) ?? -1;

        var producto = await GetOrCreateProductoAsync(nombre, ct);
        var lista = await EnsureDefaultListAsync(hogarId, usuarioId, ct);
        var item = new ListaCompra
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            ListaId = lista.Id,
            ProductoId = producto.Id,
            Producto = producto,
            AgregadoPor = usuarioId,
            Cantidad = cantidad,
            Unidad = NormalizeOptional(unidad),
            Comprado = false,
            AgregadoAlInventario = false,
            GrupoNombre = grupoNombre,
            Orden = nextOrder + 1,
            CreatedAt = DateTime.UtcNow,
            NombreManual = producto.Nombre,
            ProductoNombreSnapshot = producto.Nombre
        };

        _db.ListaCompras.Add(item);
        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToItemResult(item, map);
    }

    public async Task<ListaCompraItemResult?> MarkPurchasedAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var item = await QueryActive(hogarId)
            .FirstOrDefaultAsync(item => item.Id == id, ct);

        if (item is null)
        {
            return null;
        }

        MarkPurchased(item, usuarioId);
        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return ToItemResult(item, map);
    }

    public async Task<IReadOnlyList<ListaCompraItemResult>> MarkPurchasedByNameAsync(
        Guid hogarId,
        Guid usuarioId,
        string nombre,
        CancellationToken ct)
    {
        var normalizedName = NormalizeName(nombre);
        var candidates = await QueryActive(hogarId)
            .Where(item => item.Comprado != true)
            .ToListAsync(ct);

        var matched = candidates
            .Where(item =>
            {
                var itemName = NormalizeName(item.ProductoNombreSnapshot);
                return itemName.Contains(normalizedName, StringComparison.Ordinal) ||
                       normalizedName.Contains(itemName, StringComparison.Ordinal);
            })
            .ToList();

        foreach (var item in matched)
        {
            MarkPurchased(item, usuarioId);
        }

        await _db.SaveChangesAsync(ct);
        var map = await GetProductCategoryMapAsync(ct);
        return matched.Select(item => ToItemResult(item, map)).ToList();
    }

    public async Task<bool> MarkAddedToInventoryAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var item = await _db.ListaCompras
            .FirstOrDefaultAsync(item => item.Id == id && item.HogarId == hogarId, ct);

        if (item is null)
        {
            return false;
        }

        item.AgregadoAlInventario = true;
        item.RemovidoDeListaAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveItemAsync(Guid id, Guid hogarId, CancellationToken ct)
    {
        var item = await QueryActive(hogarId)
            .FirstOrDefaultAsync(item => item.Id == id, ct);

        if (item is null)
        {
            return false;
        }

        item.RemovidoDeListaAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveItemAsync(Guid id, Guid hogarId, Guid listaId, CancellationToken ct)
    {
        var item = await QueryActive(hogarId)
            .FirstOrDefaultAsync(item => item.Id == id && item.ListaId == listaId, ct);

        if (item is null)
        {
            return false;
        }

        item.RemovidoDeListaAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task ClearActiveAsync(Guid hogarId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var items = await QueryActive(hogarId).ToListAsync(ct);

        foreach (var item in items)
        {
            item.RemovidoDeListaAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<ListaCompra> QueryActive(Guid hogarId)
        => _db.ListaCompras
            .Include(item => item.Producto)
                .ThenInclude(producto => producto!.Categoria)
            .Where(item => item.HogarId == hogarId && item.RemovidoDeListaAt == null && item.Comprado != true);

    private async Task<Producto> GetOrCreateProductoAsync(string nombre, CancellationToken ct)
    {
        var normalizedName = NormalizeName(nombre);
        var products = await _db.Productos.ToListAsync(ct);
        var existing = products.FirstOrDefault(producto => NormalizeName(producto.Nombre) == normalizedName);

        if (existing is not null)
        {
            return existing;
        }

        var producto = new Producto
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim()
        };

        _db.Productos.Add(producto);
        return producto;
    }

    private async Task<ListaCompraHogar> EnsureDefaultListAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var lista = await _db.ListasCompraHogar
            .FirstOrDefaultAsync(lista => lista.HogarId == hogarId && lista.Nombre == "Principal", ct);

        if (lista is not null)
        {
            return lista;
        }

        lista = new ListaCompraHogar
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            Nombre = "Principal",
            CreadaPor = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ListasCompraHogar.Add(lista);
        return lista;
    }

    private static void MarkPurchased(ListaCompra item, Guid usuarioId)
    {
        if (item.Comprado == true)
        {
            return;
        }

        item.Comprado = true;
        item.CompradoEn = DateTime.UtcNow;
        item.CompradoPor = usuarioId;
        item.RemovidoDeListaAt ??= item.CompradoEn;
    }

    private static IReadOnlyList<ListaCompraGrupoResult> ToGroups(
        IReadOnlyList<ListaCompra> items,
        Dictionary<string, (string Nombre, string? IconoSvg)> map)
        => items
            .GroupBy(item => item.GrupoNombre)
            .Select(group => new ListaCompraGrupoResult(
                group.Key,
                group.OrderBy(item => item.Orden).ThenBy(item => item.CreatedAt).Select(item => ToItemResult(item, map)).ToList()))
            .ToList();

    private static ListaCompraListResult ToListResult(
        ListaCompraHogar lista,
        Dictionary<string, (string Nombre, string? IconoSvg)> map)
        => new(
            lista.Id,
            lista.Nombre,
            lista.CreatedAt,
            lista.UpdatedAt,
            lista.Items
                .Where(item => item.RemovidoDeListaAt == null && item.Comprado != true)
                .OrderBy(item => item.Orden)
                .ThenBy(item => item.CreatedAt)
                .Select(item => ToItemResult(item, map))
                .ToList());

    private static ListaCompraItemResult ToItemResult(
        ListaCompra item,
        Dictionary<string, (string Nombre, string? IconoSvg)> map)
    {
        var resolved = ResolveCategory(item, map);
        return new(
            item.Id,
            item.ProductoId,
            GetItemName(item),
            item.Cantidad,
            item.Unidad,
            item.Comprado == true,
            item.CompradoEn,
            item.Orden,
            resolved.CategoriaNombre,
            resolved.IconoSvg);
    }

    private static (string CategoriaNombre, string? IconoSvg) ResolveCategory(
        ListaCompra item,
        Dictionary<string, (string Nombre, string? IconoSvg)> map)
    {
        if (item.Producto?.Categoria is not null)
        {
            return (item.Producto.Categoria.Nombre, item.Producto.Categoria.IconoSvg);
        }

        var name = GetItemName(item);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ("Otros", "otros.svg");
        }

        var normalized = NormalizeName(name);
        if (map.TryGetValue(normalized, out var productCat))
        {
            return productCat;
        }

        return ResolveCategoryByKeyword(normalized);
    }

    private static (string CategoriaNombre, string? IconoSvg) ResolveCategoryByKeyword(string normalizedName)
    {
        if (ContainsAny(normalizedName, "leche", "queso", "yogur", "crema", "manteca", "lacteo", "ricota", "dulce de leche"))
            return ("Lácteos", "lacteos.svg");
        if (ContainsAny(normalizedName, "carne", "pollo", "cerdo", "vaca", "pescado", "atun", "merluza", "bife", "milanesa", "jamon", "panceta", "chorizo", "salchicha"))
            return ("Carnes", "carnes.svg");
        if (ContainsAny(normalizedName, "manzana", "banana", "naranja", "limon", "frutilla", "uva", "pera", "durazno", "cereza", "fruta"))
            return ("Frutas", "frutas.svg");
        if (ContainsAny(normalizedName, "tomate", "zanahoria", "cebolla", "lechuga", "papa", "batata", "morron", "ajo", "verdura", "zapallo", "espinaca", "acelga"))
            return ("Verduras", "verduras.svg");
        if (ContainsAny(normalizedName, "harina", "azucar", "chocolate", "levadura", "esencia", "reposteria", "polvo de hornear", "coco rallado"))
            return ("Repostería", "reposteria.svg");
        if (ContainsAny(normalizedName, "arroz", "fideo", "pasta", "aceite", "sal", "pimienta", "oregano", "condimento", "lentejas", "garbanzos", "caldo", "vinagre", "salsa", "almacen", "despensa"))
            return ("Almacén", "almacen.svg");
        if (ContainsAny(normalizedName, "agua", "jugo", "gaseosa", "soda", "coca", "cerveza", "vino", "bebida", "te", "cafe"))
            return ("Bebidas", "bebidas.svg");
        if (ContainsAny(normalizedName, "detergente", "jabón", "jabon", "limpieza", "lavandina", "desinfectante", "trapo", "esponja", "suavizante"))
            return ("Limpieza", "limpieza.svg");
        if (ContainsAny(normalizedName, "shampoo", "acondicionador", "papel higienico", "dentifrico", "desodorante", "baño", "bano", "jabon de tocador"))
            return ("Baño", "bano.svg");
        if (ContainsAny(normalizedName, "congelado", "hielo", "helado", "papas fritas congeladas"))
            return ("Congelados", "congelados.svg");

        return ("Otros", "otros.svg");
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task<Dictionary<string, (string Nombre, string? IconoSvg)>> GetProductCategoryMapAsync(CancellationToken ct)
    {
        var products = await _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.CategoriaId != null)
            .ToListAsync(ct);

        var map = new Dictionary<string, (string Nombre, string? IconoSvg)>();
        foreach (var p in products)
        {
            var normalized = NormalizeName(p.Nombre);
            if (p.Categoria != null && !map.ContainsKey(normalized))
            {
                map[normalized] = (p.Categoria.Nombre, p.Categoria.IconoSvg);
            }
        }
        return map;
    }

    private static string GetItemName(ListaCompra item)
        => !string.IsNullOrWhiteSpace(item.NombreManual)
            ? item.NombreManual
            : !string.IsNullOrWhiteSpace(item.ProductoNombreSnapshot)
                ? item.ProductoNombreSnapshot
                : item.Producto?.Nombre ?? string.Empty;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
