using WMS.Models;

namespace WMS.Services;

public static class RolePermissions
{
    private static readonly Dictionary<UserRole, HashSet<AppSection>> Map = new()
    {
        [UserRole.Administrator] = new HashSet<AppSection>(Enum.GetValues<AppSection>()),

        [UserRole.Director] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.ProductPrices,
            AppSection.Inventory,
            AppSection.WarehouseSummary,
            AppSection.History
        },

        [UserRole.WarehouseManager] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.Receipts,
            AppSection.Placement,
            AppSection.Movement,
            AppSection.InterWarehouseTransfers,
            AppSection.Inventory,
            AppSection.WarehouseSummary,
            AppSection.Orders,
            AppSection.Picking,
            AppSection.Shipment,
            AppSection.WriteOff,
            AppSection.StockTaking,
            AppSection.History
        },

        [UserRole.Storekeeper] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.Receipts,
            AppSection.Placement,
            AppSection.Movement,
            AppSection.InterWarehouseTransfers,
            AppSection.Inventory,
            AppSection.Picking,
            AppSection.Shipment,
            AppSection.WriteOff,
            AppSection.History
        },

        [UserRole.PurchasingManager] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.ProductsEdit,
            AppSection.ProductPrices,
            AppSection.Suppliers,
            AppSection.PurchaseOrders,
            AppSection.Inventory
        },

        [UserRole.SalesManager] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.Customers,
            AppSection.Orders,
            AppSection.Inventory
        },

        [UserRole.InventorySpecialist] = new HashSet<AppSection>
        {
            AppSection.Dashboard,
            AppSection.Products,
            AppSection.Inventory,
            AppSection.StockTaking,
            AppSection.History
        }
    };

    public static bool IsAllowed(UserRole role, AppSection section) =>
        Map.TryGetValue(role, out var sections) && sections.Contains(section);
}