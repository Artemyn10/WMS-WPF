using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class PurchaseOrderService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public PurchaseOrderService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PurchaseOrder>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Warehouse)
            .Include(o => o.CreatedBy)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(o => o.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(text) ||
                o.Supplier!.Name.ToLower().Contains(text));
        }

        return await query.OrderByDescending(o => o.Date).ToListAsync();
    }

    public async Task<string> GenerateNextOrderNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var lastNumber = await context.PurchaseOrders
            .OrderByDescending(o => o.Id)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        var nextIndex = 1;
        if (lastNumber != null && lastNumber.StartsWith("PO-") &&
            int.TryParse(lastNumber.AsSpan(3), out var parsed))
        {
            nextIndex = parsed + 1;
        }

        return $"PO-{nextIndex:D4}";
    }

    public async Task<(bool Success, string? Error)> AddAsync(
        int supplierId, int warehouseId, string orderNumber, DateOnly? expectedDate,
        string? comment, List<PurchaseOrderItemInput> items)
    {
        var validationError = Validate(supplierId, warehouseId, orderNumber, expectedDate, items);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return (false, "Нет доступа к выбранному складу.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.PurchaseOrders.AnyAsync(o => o.OrderNumber == orderNumber.Trim()))
        {
            return (false, "Заказ поставщику с таким номером уже существует.");
        }

        context.PurchaseOrders.Add(new PurchaseOrder
        {
            OrderNumber = orderNumber.Trim(),
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            Date = DateTime.UtcNow,
            ExpectedDate = expectedDate,
            Status = PurchaseOrderStatus.New,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedByUserId = CurrentSession.CurrentUser?.Id,
            Items = items.Select(i => new PurchaseOrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    /// Отменить можно только заказ, по которому ещё ничего не получено
    public async Task<(bool Success, string? Error)> CancelAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return (false, "Заказ не найден.");
        }

        if (!CurrentSession.CanAccessWarehouse(order.WarehouseId))
        {
            return (false, "Нет доступа к складу этого заказа.");
        }

        if (order.Status != PurchaseOrderStatus.New)
        {
            return (false, "Отменить можно только заказ, по которому ещё ничего не получено.");
        }

        order.Status = PurchaseOrderStatus.Cancelled;
        await context.SaveChangesAsync();
        return (true, null);
    }

    /// Открытые заказы поставщика на склад - для выбора в окне приёмки
    public async Task<List<PurchaseOrderOption>> GetOpenForReceiptAsync(int supplierId, int warehouseId)
    {
        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return new List<PurchaseOrderOption>();
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var orders = await context.PurchaseOrders
            .Include(o => o.Items)
            .Where(o => o.SupplierId == supplierId
                        && o.WarehouseId == warehouseId
                        && (o.Status == PurchaseOrderStatus.New || o.Status == PurchaseOrderStatus.PartiallyReceived))
            .OrderBy(o => o.Date)
            .ToListAsync();

        return orders.Select(o => new PurchaseOrderOption
        {
            Id = o.Id,
            Display = $"{o.OrderNumber} от {o.Date.ToLocalTime():dd.MM.yyyy} " +
                      $"(получено {o.Items.Sum(i => i.ReceivedQuantity)} из {o.Items.Sum(i => i.Quantity)})"
        }).ToList();
    }

    /// Товары заказа, которые ещё осталось получить
    public async Task<List<PurchaseOrderLine>> GetRemainingLinesAsync(int purchaseOrderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.PurchaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == purchaseOrderId);

        if (order == null || !CurrentSession.CanAccessWarehouse(order.WarehouseId))
        {
            return new List<PurchaseOrderLine>();
        }

        return order.Items
            .Where(i => i.Quantity > i.ReceivedQuantity)
            .Select(i => new PurchaseOrderLine { ProductId = i.ProductId, Remaining = i.Quantity - i.ReceivedQuantity })
            .ToList();
    }

    private static string? Validate(
        int supplierId, int warehouseId, string orderNumber, DateOnly? expectedDate, List<PurchaseOrderItemInput> items)
    {
        if (supplierId <= 0)
        {
            return "Необходимо выбрать поставщика.";
        }

        if (warehouseId <= 0)
        {
            return "Необходимо выбрать склад, на который будет поставка.";
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return "Номер заказа обязателен для заполнения.";
        }

        if (expectedDate is DateOnly date && date < DateOnly.FromDateTime(DateTime.Now))
        {
            return "Ожидаемая дата поставки не может быть раньше сегодняшней.";
        }

        if (items.Count == 0)
        {
            return "Добавьте хотя бы один товар в заказ.";
        }

        if (items.Any(i => i.ProductId <= 0))
        {
            return "Для каждой строки необходимо выбрать товар.";
        }

        if (items.Any(i => i.Quantity <= 0))
        {
            return "Количество товара должно быть больше нуля.";
        }

        if (items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
        {
            return "Один и тот же товар нельзя указывать в заказе дважды — объедините количество в одной строке.";
        }

        return null;
    }
}