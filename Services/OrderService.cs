using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class OrderService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public OrderService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Order>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(s));
        }

        return await query.OrderByDescending(o => o.Date).ToListAsync();
    }

    public async Task<string> GenerateNextOrderNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var lastNumber = await context.Orders
            .OrderByDescending(o => o.Id)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        var nextIndex = 1001;
        if (lastNumber != null && int.TryParse(lastNumber, out var parsed))
        {
            nextIndex = parsed + 1;
        }

        return nextIndex.ToString();
    }

    public async Task<(bool Success, string? Error)> AddAsync(
        int customerId, int warehouseId, string orderNumber, List<OrderItemInput> items)
    {
        var validationError = Validate(customerId, warehouseId, orderNumber, items);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Orders.AnyAsync(o => o.OrderNumber == orderNumber.Trim()))
        {
            return (false, "Заказ с таким номером уже существует.");
        }

        // Проверяем наличие товара на складе (суммарно по всем ячейкам склада)
        var stockByProduct = await context.Inventories
            .Where(i => i.StorageLocation!.WarehouseId == warehouseId)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total);

        foreach (var line in items)
        {
            var available = stockByProduct.GetValueOrDefault(line.ProductId);
            if (line.Quantity > available)
            {
                var product = await context.Products.FindAsync(line.ProductId);
                return (false, $"Недостаточно товара «{product?.Name}» на складе: доступно {available}, требуется {line.Quantity}.");
            }
        }

        var order = new Order
        {
            CustomerId = customerId,
            WarehouseId = warehouseId,
            OrderNumber = orderNumber.Trim(),
            Date = DateTime.UtcNow,
            Status = OrderStatus.New,
            Items = items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return (true, null);
    }

    private static string? Validate(int customerId, int warehouseId, string orderNumber, List<OrderItemInput> items)
    {
        if (customerId <= 0)
        {
            return "Необходимо выбрать клиента.";
        }

        if (warehouseId <= 0)
        {
            return "Необходимо выбрать склад.";
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return "Номер заказа обязателен для заполнения.";
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