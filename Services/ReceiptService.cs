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
            .Include(r => r.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(r => r.DocumentNumber.ToLower().Contains(s));
        }

        return await query.OrderByDescending(r => r.Date).ToListAsync();
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

    public async Task<(bool Success, string? Error)> AddAsync(
    int supplierId, int warehouseId, string documentNumber, List<ReceiptItemInput> items)
    {
        var validationError = Validate(supplierId, warehouseId, documentNumber, items);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Receipts.AnyAsync(r => r.DocumentNumber == documentNumber.Trim()))
        {
            return (false, "Документ приёмки с таким номером уже существует.");
        }

        var receipt = new Receipt
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            DocumentNumber = documentNumber.Trim(),
            Date = DateTime.UtcNow,
            Status = ReceiptStatus.Confirmed,
            Items = items.Select(i => new ReceiptItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        context.Receipts.Add(receipt);

        // Фиксируем факт приёмки в истории движения (без ячейки - она появится при Размещении)
        foreach (var line in items)
        {
            context.StockMovements.Add(new StockMovement
            {
                ProductId = line.ProductId,
                FromLocationId = null,
                ToLocationId = null,
                Quantity = line.Quantity,
                OperationType = OperationType.Receipt,
                Date = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    private static string? Validate(int supplierId, int warehouseId, string documentNumber, List<ReceiptItemInput> items)
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