using WMS.Helpers;
using WMS.Models;

namespace WMS.Data;

public static class DbSeeder
{
    public static void Seed(WmsDbContext context)
    {
        // Если пользователи уже есть - считаем, что база уже наполнена, и ничего не делаем
        if (context.Users.Any())
        {
            return;
        }

        // --- Пользователи ---
        var admin = new User
        {
            Login = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            FullName = "Администратор Системы",
            Role = UserRole.Administrator
        };

        var storekeeper = new User
        {
            Login = "storekeeper",
            PasswordHash = PasswordHasher.Hash("store123"),
            FullName = "Иван Кладовщиков",
            Role = UserRole.Storekeeper
        };

        context.Users.AddRange(admin, storekeeper);

        // --- Категории ---
        var categories = new List<Category>
        {
            new() { Name = "Ноутбуки" },
            new() { Name = "Периферия" },
            new() { Name = "Мониторы" },
            new() { Name = "Комплектующие" },
            new() { Name = "Сетевое оборудование" }
        };
        context.Categories.AddRange(categories);

        // --- Поставщики ---
        var suppliers = new List<Supplier>
        {
            new() { Name = "ООО \"Техно\"", Phone = "+7 495 111-22-33", Email = "info@techno.ru", Address = "Москва, ул. Складская, 1" },
            new() { Name = "ЗАО \"ЭлектроПоставка\"", Phone = "+7 495 222-33-44", Email = "sales@elpostavka.ru", Address = "Москва, пр. Промышленный, 5" },
            new() { Name = "ИП Сидоров А.А.", Phone = "+7 900 333-44-55", Email = "sidorov@mail.ru", Address = "Санкт-Петербург, ул. Логистов, 10" }
        };
        context.Suppliers.AddRange(suppliers);

        // --- Клиенты ---
        var customers = new List<Customer>
        {
            new() { Name = "Иванов Иван Иванович", Phone = "+7 910 111-11-11", Email = "ivanov@mail.ru", Address = "Москва, ул. Ленина, 1" },
            new() { Name = "ООО \"Ромашка\"", Phone = "+7 495 222-22-22", Email = "office@romashka.ru", Address = "Москва, ул. Мира, 2" },
            new() { Name = "Петров Пётр Петрович", Phone = "+7 910 333-33-33", Email = "petrov@mail.ru", Address = "Москва, ул. Гагарина, 3" },
            new() { Name = "ООО \"Вектор\"", Phone = "+7 495 444-44-44", Email = "sales@vector.ru", Address = "Москва, ул. Победы, 4" },
            new() { Name = "Сидорова Анна Сергеевна", Phone = "+7 910 555-55-55", Email = "sidorova@mail.ru", Address = "Москва, ул. Строителей, 5" }
        };
        context.Customers.AddRange(customers);

        // --- Склад ---
        var warehouse = new Warehouse
        {
            Name = "Основной склад",
            Address = "Москва, Складской проезд, 10"
        };
        context.Warehouses.Add(warehouse);

        // --- Ячейки склада (10 штук: A-01-01 .. A-02-05) ---
        var locations = new List<StorageLocation>();
        for (int row = 1; row <= 2; row++)
        {
            for (int cell = 1; cell <= 5; cell++)
            {
                locations.Add(new StorageLocation
                {
                    Warehouse = warehouse,
                    Code = $"A-{row:D2}-{cell:D2}",
                    Capacity = 100
                });
            }
        }
        context.StorageLocations.AddRange(locations);

        // --- Товары (10 штук) ---
        var products = new List<Product>
        {
            new() { Name = "Ноутбук Lenovo IdeaPad", Article = "ART-0001", Barcode = "4600000000011", Category = categories[0], Unit = "шт" },
            new() { Name = "Ноутбук ASUS VivoBook", Article = "ART-0002", Barcode = "4600000000012", Category = categories[0], Unit = "шт" },
            new() { Name = "Мышь Logitech M185", Article = "ART-0003", Barcode = "4600000000013", Category = categories[1], Unit = "шт" },
            new() { Name = "Клавиатура A4Tech", Article = "ART-0004", Barcode = "4600000000014", Category = categories[1], Unit = "шт" },
            new() { Name = "Монитор Samsung 24\"", Article = "ART-0005", Barcode = "4600000000015", Category = categories[2], Unit = "шт" },
            new() { Name = "Монитор LG 27\"", Article = "ART-0006", Barcode = "4600000000016", Category = categories[2], Unit = "шт" },
            new() { Name = "Оперативная память Kingston 8Gb", Article = "ART-0007", Barcode = "4600000000017", Category = categories[3], Unit = "шт" },
            new() { Name = "SSD Samsung 512Gb", Article = "ART-0008", Barcode = "4600000000018", Category = categories[3], Unit = "шт" },
            new() { Name = "Роутер TP-Link", Article = "ART-0009", Barcode = "4600000000019", Category = categories[4], Unit = "шт" },
            new() { Name = "Патч-корд 2м", Article = "ART-0010", Barcode = "4600000000020", Category = categories[4], Unit = "шт" }
        };
        context.Products.AddRange(products);

        // Сохраняем справочники, чтобы получить их Id для дальнейших связей
        context.SaveChanges();

        // --- Тестовая операция: приёмка ---
        var receipt = new Receipt
        {
            Supplier = suppliers[0],
            Warehouse = warehouse,
            DocumentNumber = "REC-0001",
            Date = DateTime.Now,
            Status = ReceiptStatus.Confirmed,
            Items = new List<ReceiptItem>
            {
                new() { Product = products[0], Quantity = 20 }, // Ноутбук Lenovo - 20 шт
                new() { Product = products[2], Quantity = 50 }  // Мышь Logitech - 50 шт
            }
        };
        context.Receipts.Add(receipt);

        // --- Тестовое размещение: остатки в ячейках ---
        var inventory1 = new Inventory { Product = products[0], StorageLocation = locations[0], Quantity = 20 }; // A-01-01
        var inventory2 = new Inventory { Product = products[2], StorageLocation = locations[1], Quantity = 50 }; // A-01-02
        context.Inventories.AddRange(inventory1, inventory2);

        // --- История движения для этой приёмки и размещения ---
        context.StockMovements.AddRange(
            new StockMovement
            {
                Product = products[0],
                FromLocation = null,
                ToLocation = locations[0],
                Quantity = 20,
                OperationType = OperationType.Receipt,
                Date = DateTime.Now
            },
            new StockMovement
            {
                Product = products[2],
                FromLocation = null,
                ToLocation = locations[1],
                Quantity = 50,
                OperationType = OperationType.Receipt,
                Date = DateTime.Now
            }
        );

        context.SaveChanges();
    }
}