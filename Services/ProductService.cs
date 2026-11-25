using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ProductService
{
    // Максимальный размер одного прикреплённого файла
    public const int MaxFileSize = 10 * 1024 * 1024;

    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ProductService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// Простой список товаров - для выпадающих списков в других окнах
    public async Task<List<Product>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Article.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    /// Справочник номенклатуры: поиск по тексту и фильтр по категории (categoryId = 0 - все категории)
    public async Task<List<Product>> GetCatalogAsync(string? search = null, int categoryId = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (categoryId > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Article.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)) ||
                (p.Manufacturer != null && p.Manufacturer.ToLower().Contains(s)) ||
                p.Category!.Name.ToLower().Contains(s));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        // Количество файлов у каждого товара (сами файлы не загружаются)
        var ids = products.Select(p => p.Id).ToList();
        var counts = await context.ProductFiles
            .Where(f => ids.Contains(f.ProductId))
            .GroupBy(f => f.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);

        foreach (var product in products)
        {
            product.FilesCount = counts.GetValueOrDefault(product.Id);
        }

        return products;
    }

    public async Task<(bool Success, string? Error)> AddAsync(Product product, List<ProductFileInput> newFiles)
    {
        var validationError = Validate(product) ?? ValidateFiles(newFiles);
        if (validationError != null)
        {
            return (false, validationError);
        }

        var article = product.Article.Trim();
        var barcode = Clean(product.Barcode);

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Products.AnyAsync(p => p.Article == article))
        {
            return (false, "Товар с таким артикулом уже существует.");
        }

        if (barcode != null && await context.Products.AnyAsync(p => p.Barcode == barcode))
        {
            return (false, "Товар с таким штрихкодом уже существует.");
        }

        context.Products.Add(new Product
        {
            Name = product.Name.Trim(),
            Article = article,
            Barcode = barcode,
            CategoryId = product.CategoryId,
            Unit = product.Unit.Trim(),
            Description = Clean(product.Description),
            ReleaseYear = product.ReleaseYear,
            ItemType = product.ItemType,
            Manufacturer = Clean(product.Manufacturer),
            SupplierId = product.SupplierId,
            Characteristics = Clean(product.Characteristics),
            PurchasePrice = product.PurchasePrice,
            AccountingPrice = product.AccountingPrice,
            Files = newFiles.Select(ToEntity).ToList()
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        Product product, List<ProductFileInput> newFiles, List<int> removedFileIds)
    {
        var validationError = Validate(product) ?? ValidateFiles(newFiles);
        if (validationError != null)
        {
            return (false, validationError);
        }

        var article = product.Article.Trim();
        var barcode = Clean(product.Barcode);

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Products.AnyAsync(p => p.Article == article && p.Id != product.Id))
        {
            return (false, "Товар с таким артикулом уже существует.");
        }

        if (barcode != null && await context.Products.AnyAsync(p => p.Barcode == barcode && p.Id != product.Id))
        {
            return (false, "Товар с таким штрихкодом уже существует.");
        }

        var existing = await context.Products.FindAsync(product.Id);
        if (existing == null)
        {
            return (false, "Товар не найден.");
        }

        existing.Name = product.Name.Trim();
        existing.Article = article;
        existing.Barcode = barcode;
        existing.CategoryId = product.CategoryId;
        existing.Unit = product.Unit.Trim();
        existing.Description = Clean(product.Description);
        existing.ReleaseYear = product.ReleaseYear;
        existing.ItemType = product.ItemType;
        existing.Manufacturer = Clean(product.Manufacturer);
        existing.SupplierId = product.SupplierId;
        existing.Characteristics = Clean(product.Characteristics);
        existing.PurchasePrice = product.PurchasePrice;
        existing.AccountingPrice = product.AccountingPrice;

        // Удалённые в окне файлы и новые файлы сохраняются вместе с товаром, одной транзакцией
        if (removedFileIds.Count > 0)
        {
            var toRemove = await context.ProductFiles
                .Where(f => f.ProductId == product.Id && removedFileIds.Contains(f.Id))
                .ToListAsync();
            context.ProductFiles.RemoveRange(toRemove);
        }

        foreach (var file in newFiles)
        {
            var entity = ToEntity(file);
            entity.ProductId = existing.Id;
            context.ProductFiles.Add(entity);
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null)
        {
            return (false, "Товар не найден.");
        }

        context.Products.Remove(product);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить товар: он используется в остатках или операциях.");
        }
    }

    /// Список файлов товара без содержимого
    public async Task<List<ProductFileInfo>> GetFilesAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ProductFiles
            .Where(f => f.ProductId == productId)
            .OrderBy(f => f.Kind)
            .ThenBy(f => f.Id)
            .Select(f => new ProductFileInfo
            {
                Id = f.Id,
                FileName = f.FileName,
                Kind = f.Kind,
                Size = f.Size,
                UploadedAt = f.UploadedAt
            })
            .ToListAsync();
    }

    /// Содержимое файла (загружается только когда оно нужно)
    public async Task<byte[]?> GetFileDataAsync(int fileId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ProductFiles
            .Where(f => f.Id == fileId)
            .Select(f => f.Data)
            .FirstOrDefaultAsync();
    }

    /// Первая фотография товара (null - фотографий нет)
    public async Task<byte[]?> GetMainPhotoAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ProductFiles
            .Where(f => f.ProductId == productId && f.Kind == ProductFileKind.Photo)
            .OrderBy(f => f.Id)
            .Select(f => f.Data)
            .FirstOrDefaultAsync();
    }

    /// Сколько товара на каждом складе: размещённый плюс принятый и ещё не размещённый.
    /// Справочник один на все склады, а наличие у каждого склада своё.
    /// Ограниченный пользователь видит только свой склад.
    public async Task<List<ProductStockRow>> GetStockByWarehouseAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var placedQuery = context.Inventories
            .Where(i => i.ProductId == productId && i.Quantity > 0);

        var pendingQuery = context.ReceiptItems
            .Where(i => i.ProductId == productId
                        && i.Receipt!.Status == ReceiptStatus.Confirmed
                        && i.Quantity > i.PlacedQuantity);

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            placedQuery = placedQuery.Where(i => i.StorageLocation!.WarehouseId == scopeId);
            pendingQuery = pendingQuery.Where(i => i.Receipt!.WarehouseId == scopeId);
        }

        var placed = await placedQuery
            .GroupBy(i => new { i.StorageLocation!.WarehouseId, i.StorageLocation.Warehouse!.Name })
            .Select(g => new { g.Key.WarehouseId, g.Key.Name, Quantity = g.Sum(i => i.Quantity) })
            .ToListAsync();

        var pending = await pendingQuery
            .GroupBy(i => new { i.Receipt!.WarehouseId, i.Receipt.Warehouse!.Name })
            .Select(g => new { g.Key.WarehouseId, g.Key.Name, Quantity = g.Sum(i => i.Quantity - i.PlacedQuantity) })
            .ToListAsync();

        return placed
            .Concat(pending)
            .GroupBy(x => new { x.WarehouseId, x.Name })
            .Select(g => new ProductStockRow { WarehouseName = g.Key.Name, Quantity = g.Sum(x => x.Quantity) })
            .OrderBy(r => r.WarehouseName)
            .ToList();
    }

    private static ProductFile ToEntity(ProductFileInput file) => new()
    {
        FileName = file.FileName,
        Kind = file.Kind,
        Size = file.Data.Length,
        Data = file.Data,
        UploadedAt = DateTime.UtcNow
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ValidateFiles(List<ProductFileInput> files)
    {
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FileName) || file.Data.Length == 0)
            {
                return "Прикреплён пустой файл.";
            }

            if (file.Data.Length > MaxFileSize)
            {
                return $"Файл «{file.FileName}» слишком большой (максимум 10 МБ).";
            }
        }

        return null;
    }

    private static string? Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return "Название товара обязательно для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(product.Article))
        {
            return "Артикул товара обязателен для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(product.Unit))
        {
            return "Единица измерения обязательна для заполнения.";
        }

        if (product.CategoryId <= 0)
        {
            return "Необходимо выбрать категорию.";
        }

        if (product.ReleaseYear is < 1900 or > 2100)
        {
            return "Год выпуска указан некорректно.";
        }

        if (product.PurchasePrice is < 0)
        {
            return "Закупочная стоимость не может быть отрицательной.";
        }

        if (product.AccountingPrice is < 0)
        {
            return "Учётная стоимость не может быть отрицательной.";
        }

        return null;
    }
}