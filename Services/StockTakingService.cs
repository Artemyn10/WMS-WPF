using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class StockTakingService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;

    public StockTakingService(IDbContextFactory<WmsDbContext> contextFactory, InventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    public async Task<List<StockTaking>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.StockTakings
            .Include(s => s.Warehouse)
            .Include(s => s.CreatedBy)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.StorageLocation)
            .AsSplitQuery()
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(s => s.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(s => s.DocumentNumber.ToLower().Contains(text));
        }

        return await query.OrderByDescending(s => s.StartDate).ToListAsync();
    }

    /// Документ со всеми позициями для окна пересчёта (null - не найден или нет доступа)
    public async Task<StockTaking?> GetForCountAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var document = await context.StockTakings
            .Include(s => s.Warehouse)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.StorageLocation)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (document == null || !CurrentSession.CanAccessWarehouse(document.WarehouseId))
        {
            return null;
        }

        return document;
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateAsync(int warehouseId, string? comment)
    {
        if (warehouseId <= 0)
        {
            return (false, "Необходимо выбрать склад.", null);
        }

        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return (false, "Нет доступа к выбранному складу.", null);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var alreadyInProgress = await context.StockTakings
            .AnyAsync(s => s.WarehouseId == warehouseId && s.Status == StockTakingStatus.InProgress);
        if (alreadyInProgress)
        {
            return (false, "Для этого склада уже есть незавершённая инвентаризация.", null);
        }

        // Снимок учётных остатков склада на текущий момент
        var stock = await context.Inventories
            .Where(i => i.Quantity > 0 && i.StorageLocation!.WarehouseId == warehouseId)
            .Select(i => new { i.ProductId, i.StorageLocationId, i.Quantity })
            .ToListAsync();

        if (stock.Count == 0)
        {
            return (false, "На складе нет остатков для инвентаризации.", null);
        }

        var document = new StockTaking
        {
            DocumentNumber = await GenerateNextDocumentNumberAsync(context),
            WarehouseId = warehouseId,
            StartDate = DateTime.UtcNow,
            Status = StockTakingStatus.InProgress,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedByUserId = CurrentSession.CurrentUser?.Id,
            Items = stock.Select(s => new StockTakingItem
            {
                ProductId = s.ProductId,
                StorageLocationId = s.StorageLocationId,
                ExpectedQuantity = s.Quantity
            }).ToList()
        };

        context.StockTakings.Add(document);
        await context.SaveChangesAsync();

        return (true, null, document.Id);
    }

    /// Сохранить введённые фактические количества (null - строка ещё не пересчитана)
    public async Task<(bool Success, string? Error)> SaveCountAsync(int id, Dictionary<int, int?> actuals)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var (document, error) = await LoadInProgressAsync(context, id);
        if (document == null)
        {
            return (false, error);
        }

        var applyError = ApplyActuals(document, actuals);
        if (applyError != null)
        {
            return (false, applyError);
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// Сохранить пересчёт, исправить остатки и завершить документ
    public async Task<(bool Success, string? Error)> CompleteAsync(int id, Dictionary<int, int?> actuals)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var (document, error) = await LoadInProgressAsync(context, id);
        if (document == null)
        {
            return (false, error);
        }

        var applyError = ApplyActuals(document, actuals);
        if (applyError != null)
        {
            return (false, applyError);
        }

        var notCounted = document.Items.Count(i => i.ActualQuantity == null);
        if (notCounted > 0)
        {
            return (false, $"Не пересчитано позиций: {notCounted}. Заполните фактическое количество во всех строках.");
        }

        var now = DateTime.UtcNow;

        // Остатки, история и статус документа сохраняются одним SaveChanges - одной транзакцией
        foreach (var item in document.Items)
        {
            var current = await _inventoryService.GetQuantityAsync(context, item.ProductId, item.StorageLocationId);
            var actual = item.ActualQuantity!.Value;

            if (actual == current)
            {
                continue;
            }

            if (actual > current)
            {
                // Излишек
                await _inventoryService.IncreaseAsync(context, item.ProductId, item.StorageLocationId, actual - current);

                context.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    FromLocationId = null,
                    ToLocationId = item.StorageLocationId,
                    Quantity = actual - current,
                    OperationType = OperationType.StockTaking,
                    Date = now,
                    WarehouseId = document.WarehouseId
                });
            }
            else
            {
                // Недостача
                await _inventoryService.DecreaseAsync(context, item.ProductId, item.StorageLocationId, current - actual);

                context.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    FromLocationId = item.StorageLocationId,
                    ToLocationId = null,
                    Quantity = current - actual,
                    OperationType = OperationType.StockTaking,
                    Date = now,
                    WarehouseId = document.WarehouseId
                });
            }
        }

        document.Status = StockTakingStatus.Completed;
        document.CompletedDate = now;

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// Удалить можно только незавершённую инвентаризацию
    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var (document, error) = await LoadInProgressAsync(context, id);
        if (document == null)
        {
            return (false, error);
        }

        context.StockTakings.Remove(document);
        await context.SaveChangesAsync();
        return (true, null);
    }

    private static async Task<string> GenerateNextDocumentNumberAsync(WmsDbContext context)
    {
        var lastNumber = await context.StockTakings
            .OrderByDescending(s => s.Id)
            .Select(s => s.DocumentNumber)
            .FirstOrDefaultAsync();

        var nextIndex = 1;
        if (lastNumber != null && lastNumber.StartsWith("INV-") &&
            int.TryParse(lastNumber.AsSpan(4), out var parsed))
        {
            nextIndex = parsed + 1;
        }

        return $"INV-{nextIndex:D4}";
    }

    private static async Task<(StockTaking? Document, string? Error)> LoadInProgressAsync(WmsDbContext context, int id)
    {
        var document = await context.StockTakings
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (document == null)
        {
            return (null, "Документ инвентаризации не найден.");
        }

        if (!CurrentSession.CanAccessWarehouse(document.WarehouseId))
        {
            return (null, "Нет доступа к складу этого документа.");
        }

        if (document.Status != StockTakingStatus.InProgress)
        {
            return (null, "Инвентаризация уже завершена.");
        }

        return (document, null);
    }

    private static string? ApplyActuals(StockTaking document, Dictionary<int, int?> actuals)
    {
        if (actuals.Values.Any(v => v < 0))
        {
            return "Фактическое количество не может быть отрицательным.";
        }

        foreach (var item in document.Items)
        {
            if (actuals.TryGetValue(item.Id, out var value))
            {
                item.ActualQuantity = value;
            }
        }

        return null;
    }
}