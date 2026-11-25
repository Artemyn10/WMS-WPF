using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Data;

public class WmsDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptItem> ReceiptItems => Set<ReceiptItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<WarehouseEmployee> WarehouseEmployees => Set<WarehouseEmployee>();
    public DbSet<WriteOff> WriteOffs => Set<WriteOff>();
    public DbSet<WriteOffItem> WriteOffItems => Set<WriteOffItem>();
    public DbSet<StockTaking> StockTakings => Set<StockTaking>();
    public DbSet<StockTakingItem> StockTakingItems => Set<StockTakingItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<ProductFile> ProductFiles => Set<ProductFile>();

    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Уникальные поля
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Article)
            .IsUnique();

        modelBuilder.Entity<StorageLocation>()
            .HasIndex(l => new { l.WarehouseId, l.Code })
            .IsUnique();

        // Product -> Category
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // StorageLocation -> Warehouse
        modelBuilder.Entity<StorageLocation>()
            .HasOne(l => l.Warehouse)
            .WithMany(w => w.StorageLocations)
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Inventory -> Product / StorageLocation
        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Product)
            .WithMany(p => p.InventoryRecords)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.StorageLocation)
            .WithMany(l => l.InventoryRecords)
            .HasForeignKey(i => i.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receipt -> Supplier / Warehouse
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.Supplier)
            .WithMany(s => s.Receipts)
            .HasForeignKey(r => r.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.Warehouse)
            .WithMany()
            .HasForeignKey(r => r.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ReceiptItem -> Receipt / Product
        modelBuilder.Entity<ReceiptItem>()
            .HasOne(ri => ri.Receipt)
            .WithMany(r => r.Items)
            .HasForeignKey(ri => ri.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReceiptItem>()
            .HasOne(ri => ri.Product)
            .WithMany()
            .HasForeignKey(ri => ri.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Order -> Customer / Warehouse
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrderItem -> Order / Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Shipment -> Order
        modelBuilder.Entity<Shipment>()
            .HasOne(s => s.Order)
            .WithMany()
            .HasForeignKey(s => s.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ShipmentItem -> Shipment / Product
        modelBuilder.Entity<ShipmentItem>()
            .HasOne(si => si.Shipment)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShipmentItem>()
            .HasOne(si => si.Product)
            .WithMany()
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockMovement -> Product / FromLocation / ToLocation
        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.FromLocation)
            .WithMany()
            .HasForeignKey(m => m.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.ToLocation)
            .WithMany()
            .HasForeignKey(m => m.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        // User -> Warehouse (привязка пользователя к складу)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Warehouse)
            .WithMany()
            .HasForeignKey(u => u.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockMovement -> Warehouse
        modelBuilder.Entity<StockMovement>()
            .HasOne(m => m.Warehouse)
            .WithMany()
            .HasForeignKey(m => m.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        // WriteOff
        modelBuilder.Entity<WriteOff>()
            .HasIndex(w => w.DocumentNumber)
            .IsUnique();

        modelBuilder.Entity<WriteOff>()
            .HasOne(w => w.Warehouse)
            .WithMany()
            .HasForeignKey(w => w.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WriteOff>()
            .HasOne(w => w.CreatedBy)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // WriteOffItem
        modelBuilder.Entity<WriteOffItem>()
            .HasOne(i => i.WriteOff)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WriteOffId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WriteOffItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WriteOffItem>()
            .HasOne(i => i.StorageLocation)
            .WithMany()
            .HasForeignKey(i => i.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        // StockTaking
        modelBuilder.Entity<StockTaking>()
            .HasIndex(s => s.DocumentNumber)
            .IsUnique();

        modelBuilder.Entity<StockTaking>()
            .HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTaking>()
            .HasOne(s => s.CreatedBy)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockTakingItem
        modelBuilder.Entity<StockTakingItem>()
            .HasOne(i => i.StockTaking)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.StockTakingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTakingItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTakingItem>()
            .HasOne(i => i.StorageLocation)
            .WithMany()
            .HasForeignKey(i => i.StorageLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTakingItem>()
            .HasIndex(i => new { i.StockTakingId, i.ProductId, i.StorageLocationId })
            .IsUnique();
        // PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.Supplier)
            .WithMany()
            .HasForeignKey(o => o.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(o => o.CreatedBy)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // PurchaseOrderItem
        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany(o => o.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receipt -> PurchaseOrder
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.PurchaseOrder)
            .WithMany()
            .HasForeignKey(r => r.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        // Product: стоимость с двумя знаками после запятой
        modelBuilder.Entity<Product>()
            .Property(p => p.PurchasePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.AccountingPrice)
            .HasPrecision(18, 2);

        // Product -> Supplier (основной поставщик)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductFile: при удалении товара его файлы удаляются вместе с ним
        modelBuilder.Entity<ProductFile>()
            .HasOne(f => f.Product)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        // Receipt -> User (кто оформил поступление)
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}