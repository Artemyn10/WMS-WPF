using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class WriteOffService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;

    public WriteOffService(IDbContextFactory<WmsDbContext> contextFactory, InventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    public async Task<List<WriteOff>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.WriteOffs
            .Include(w => w.Warehouse)
            .Include(w => w.CreatedBy)
            .Include(w => w.Items).ThenInclude(i => i.Product)
            .Include(w => w.Items).ThenInclude(i => i.StorageLocation)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(w => w.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w => w.DocumentNumber.ToLower().Contains(s) || w.Reason.ToLower().Contains(s));
        }

        return await query.OrderByDescending(w => w.Date).ToListAsync();
    }

    public async Task<string> GenerateNextDocumentNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var lastNumber = await context.WriteOffs
            .OrderByDescending(w => w.Id)
            .Select(w => w.DocumentNumber)
            .FirstOrDefaultAsync();

        var nextIndex = 1;
        if (lastNumber != null && lastNumber.StartsWith("WO-") &&
            int.TryParse(lastNumber.AsSpan(3), out var parsed))
        {
            nextIndex = parsed + 1;
        }

        return $"WO-{nextIndex:D4}";
    }

    /// Товары, которые сейчас лежат на складе, по ячейкам
    public async Task<List<StockOption>> GetStockOptionsAsync(int warehouseId)
    {
        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return new List<StockOption>();
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Inventories
            .Where(i => i.Quantity > 0 && i.StorageLocation!.WarehouseId == warehouseId)
            .OrderBy(i => i.Product!.Name)
            .ThenBy(i => i.StorageLocation!.Code)
            .Select(i => new StockOption
            {
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                LocationId = i.StorageLocationId,
                LocationCode = i.StorageLocation!.Code,
                Available = i.Quantity
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(
        int warehouseId, string documentNumber, string reason, string? comment, List<WriteOffItemInput> items)
    {
        var validationError = Validate(warehouseId, documentNumber, reason, items);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return (false, "Нет доступа к выбранному складу.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.WriteOffs.AnyAsync(w => w.DocumentNumber == documentNumber.Trim()))
        {
            return (false, "Документ списания с таким номером уже существует.");
        }

        var locationIds = items.Select(i => i.LocationId).Distinct().ToList();
        var locations = await context.StorageLocations
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id);

        // Проверяем все позиции до того, как что-то менять
        foreach (var line in items)
        {
            if (!locations.TryGetValue(line.LocationId, out var location) || location.WarehouseId != warehouseId)
            {
                return (false, "Выбранная ячейка не относится к выбранному складу.");
            }

            var available = await _inventoryService.GetQuantityAsync(context, line.ProductId, line.LocationId);
            if (line.Quantity > available)
            {
                var product = await context.Products.FindAsync(line.ProductId);
                return (false,
                    $"Недостаточно товара «{product?.Name}» в ячейке {location.Code}: доступно {available}, списывается {line.Quantity}.");
            }
        }

        var now = DateTime.UtcNow;

        context.WriteOffs.Add(new WriteOff
        {
            DocumentNumber = documentNumber.Trim(),
            Date = now,
            WarehouseId = warehouseId,
            Reason = reason.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedByUserId = CurrentSession.CurrentUser?.Id,
            Items = items.Select(i => new WriteOffItem
            {
                ProductId = i.ProductId,
                StorageLocationId = i.LocationId,
                Quantity = i.Quantity
            }).ToList()
        });

        try
        {
            // Остатки, история и документ сохраняются одним SaveChanges - одной транзакцией
            foreach (var line in items)
            {
                await _inventoryService.DecreaseAsync(context, line.ProductId, line.LocationId, line.Quantity);

                context.StockMovements.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    FromLocationId = line.LocationId,
                    ToLocationId = null,
                    Quantity = line.Quantity,
                    OperationType = OperationType.WriteOff,
                    Date = now,
                    WarehouseId = warehouseId
                });
            }

            await context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }

        return (true, null);
    }

    private static string? Validate(int warehouseId, string documentNumber, string reason, List<WriteOffItemInput> items)
    {
        if (warehouseId <= 0)
        {
            return "Необходимо выбрать склад.";
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return "Номер документа обязателен для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Укажите причину списания.";
        }

        if (items.Count == 0)
        {
            return "Добавьте хотя бы одну позицию для списания.";
        }

        if (items.Any(i => i.Quantity <= 0))
        {
            return "Количество товара должно быть больше нуля.";
        }

        if (items.GroupBy(i => new { i.ProductId, i.LocationId }).Any(g => g.Count() > 1))
        {
            return "Один и тот же товар в одной ячейке нельзя указывать дважды — объедините количество в одной строке.";
        }

        return null;
    }
}