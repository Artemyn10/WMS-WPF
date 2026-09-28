using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ShipmentService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ShipmentService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// Заказы, полностью собранные и ожидающие отгрузки
    public async Task<List<Order>> GetOrdersPendingShipmentAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.Status == OrderStatus.Assembled)
            .OrderBy(o => o.Date)
            .ToListAsync();
    }

    public async Task<List<Shipment>> GetShipmentsAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Shipments
            .Include(s => s.Order)
                .ThenInclude(o => o!.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.Order!.OrderNumber.ToLower().Contains(s));
        }

        return await query.OrderByDescending(s => s.Date).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> ShipAsync(int orderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return (false, "Заказ не найден.");
        }

        if (order.Status != OrderStatus.Assembled)
        {
            return (false, "Отгрузить можно только полностью собранный заказ.");
        }

        var shipment = new Shipment
        {
            OrderId = order.Id,
            Date = DateTime.UtcNow,
            Status = ShipmentStatus.Completed,
            Items = order.Items.Select(i => new ShipmentItem
            {
                ProductId = i.ProductId,
                Quantity = i.PickedQuantity
            }).ToList()
        };

        context.Shipments.Add(shipment);

        order.Status = OrderStatus.Shipped;

        foreach (var item in order.Items)
        {
            context.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                FromLocationId = null,
                ToLocationId = null,
                Quantity = item.PickedQuantity,
                OperationType = OperationType.Shipment,
                Date = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return (true, null);
    }
}