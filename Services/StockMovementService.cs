using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Helpers;
using WMS.Models;

namespace WMS.Services;

public class StockMovementService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public StockMovementService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<StockMovementRow>> GetHistoryAsync(string? productSearch = null, OperationType? operationType = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var s = productSearch.ToLower();
            query = query.Where(m => m.Product!.Name.ToLower().Contains(s) || m.Product!.Article.ToLower().Contains(s));
        }

        if (operationType != null)
        {
            query = query.Where(m => m.OperationType == operationType);
        }

        var movements = await query.OrderByDescending(m => m.Date).ToListAsync();

        return movements.Select(m => new StockMovementRow
        {
            Date = m.Date,
            OperationTypeDisplay = OperationTypeDisplay.ToRussian(m.OperationType),
            ProductName = m.Product!.Name,
            Quantity = m.Quantity,
            FromLocationCode = m.FromLocation?.Code ?? "—",
            ToLocationCode = m.ToLocation?.Code ?? "—"
        }).ToList();
    }
}