using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ReceiptService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ReceiptService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Receipt>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Receipts
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.PurchaseOrder)
            .Include(r => r.CreatedBy)
            .Include(r => r.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(r => r.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(r => r.DocumentNumber.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(r => r.ArrivalDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task<string> GenerateNextDocumentNumberAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var lastNumber = await context.Receipts
            .OrderByDescending(r => r.Id)
            .Select(r => r.DocumentNumber)
            .FirstOrDefaultAsync();

        var nextIndex = 1;
        if (lastNumber != null && lastNumber.StartsWith("REC-") &&
            int.TryParse(lastNumber.AsSpan(4), out var parsed))
        {
            nextIndex = parsed + 1;
        }

        return $"REC-{nextIndex:D4}";
    }

    /// Оформляет и сразу подтверждает поступление.
    /// arrivalDate - дата поступления товара на склад, purchaseOrderId - заказ поставщику (null - без заказа).
    public async Task<(bool Success, string? Error)> AddAsync(
        int supplierId, int warehouseId, string documentNumber, DateOnly? arrivalDate,
        List<ReceiptItemInput> items, int? purchaseOrderId = null)
    {
        var validationError = Validate(supplierId, warehouseId, documentNumber, arrivalDate, items);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!CurrentSession.CanAccessWarehouse(warehouseId))
        {
            return (false, "Нет доступа к выбранному складу.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Receipts.AnyAsync(r => r.DocumentNumber == documentNumber.Trim()))
        {
            return (false, "Документ приёмки с таким номером уже существует.");
        }

        PurchaseOrder? purchaseOrder = null;
        if (purchaseOrderId != null)
        {
            purchaseOrder = await context.PurchaseOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == purchaseOrderId);

            var linkError = await ValidatePurchaseOrderAsync(context, purchaseOrder, supplierId, warehouseId, items);
            if (linkError != null)
            {
                return (false, linkError);
            }
        }

        var now = DateTime.UtcNow;

        var receipt = new Receipt
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            DocumentNumber = documentNumber.Trim(),
            Date = now,
            ArrivalDate = arrivalDate!.Value,
            Status = ReceiptStatus.Confirmed,
            PurchaseOrderId = purchaseOrder?.Id,
            CreatedByUserId = CurrentSession.CurrentUser?.Id,
            Items = items.Select(i => new ReceiptItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        context.Receipts.Add(receipt);

        // Фиксируем факт поступления в истории движения (без ячейки - она появится при размещении)
        foreach (var line in items)
        {
            context.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                FromLocationId = null,
                ToLocationId = null,
                Quantity = line.Quantity,
                OperationType = OperationType.Receipt,
                Date = now,
                WarehouseId = warehouseId
            });
        }

        // Заказ поставщику: увеличиваем "получено" и обновляем статус.
        // Всё сохраняется одним SaveChanges - одной транзакцией.
        if (purchaseOrder != null)
        {
            foreach (var line in items)
            {
                var orderItem = purchaseOrder.Items.First(i => i.ProductId == line.ProductId);
                orderItem.ReceivedQuantity += line.Quantity;
            }

            purchaseOrder.Status = purchaseOrder.Items.All(i => i.ReceivedQuantity >= i.Quantity)
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.PartiallyReceived;
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    private static async Task<string?> ValidatePurchaseOrderAsync(
        WmsDbContext context, PurchaseOrder? order, int supplierId, int warehouseId, List<ReceiptItemInput> items)
    {
        if (order == null)
        {
            return "Заказ поставщику не найден.";
        }

        if (order.Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
        {
            return $"Заказ {order.OrderNumber} уже закрыт: получен полностью или отменён.";
        }

        if (order.SupplierId != supplierId || order.WarehouseId != warehouseId)
        {
            return $"Заказ {order.OrderNumber} оформлен на другого поставщика или на другой склад.";
        }

        var productIds = items.Select(i => i.ProductId).ToList();
        var names = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var line in items)
        {
            var name = names.GetValueOrDefault(line.ProductId, "?");
            var orderItem = order.Items.FirstOrDefault(i => i.ProductId == line.ProductId);

            if (orderItem == null)
            {
                return $"Товара «{name}» нет в заказе {order.OrderNumber}.";
            }

            var remaining = orderItem.Quantity - orderItem.ReceivedQuantity;
            if (line.Quantity > remaining)
            {
                return $"По заказу {order.OrderNumber} товара «{name}» осталось получить {remaining}, а принимается {line.Quantity}.";
            }
        }

        return null;
    }

    private static string? Validate(
        int supplierId, int warehouseId, string documentNumber, DateOnly? arrivalDate, List<ReceiptItemInput> items)
    {
        if (supplierId <= 0)
        {
            return "Необходимо выбрать поставщика.";
        }

        if (warehouseId <= 0)
        {
            return "Необходимо выбрать склад.";
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return "Номер документа обязателен для заполнения.";
        }

        if (arrivalDate == null)
        {
            return "Укажите дату поступления.";
        }

        if (arrivalDate.Value > DateOnly.FromDateTime(DateTime.Now))
        {
            return "Дата поступления не может быть в будущем.";
        }

        if (items.Count == 0)
        {
            return "Добавьте хотя бы один товар в приёмку.";
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
            return "Один и тот же товар нельзя указывать в приёмке дважды — объедините количество в одной строке.";
        }

        return null;
    }
}