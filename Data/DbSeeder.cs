using WMS.Helpers;
using WMS.Models;

namespace WMS.Data;

public static class DbSeeder
{
    public static void Seed(WmsDbContext context)
    {
        // Если пользователей ещё нет - создаём справочники и первых пользователей
        var usersExist = context.Users.Any();
        if (!usersExist)
        {
            SeedCoreData(context);
        }

        // Тестовые пользователи для остальных ролей (создаются, если ещё нет)
        SeedRoleUsers(context);

        // Производитель, стоимость и характеристики у тестовых товаров (только там, где они пустые)
        SeedNomenclatureDetails(context);

        // Отдельно проверяем остатки - на случай, если в прошлый раз
        // сидер прервался после справочников, но до создания приёмки/остатков
        if (!context.Inventories.Any())
        {
            SeedDemoOperations(context);
        }
    }

    private static void SeedNomenclatureDetails(WmsDbContext context)
    {
        var suppliers = context.Suppliers.ToList();
        int? FindSupplier(string name) => suppliers.FirstOrDefault(s => s.Name == name)?.Id;

        var techno = FindSupplier("ООО \"Техно\"");
        var electro = FindSupplier("ЗАО \"ЭлектроПоставка\"");
        var sidorov = FindSupplier("ИП Сидоров А.А.");

        var details = new (string Article, string Manufacturer, int? SupplierId, decimal Purchase,
            decimal Accounting, string Characteristics, int Year, ProductType Type)[]
        {
            ("ART-0001", "Lenovo", techno, 42000m, 45000m,
                "Экран 15.6\" FHD; процессор Intel Core i5; ОЗУ 8 ГБ; SSD 512 ГБ; Windows 11", 2023, ProductType.Goods),
            ("ART-0002", "ASUS", techno, 38000m, 41000m,
                "Экран 15.6\" FHD; процессор AMD Ryzen 5; ОЗУ 16 ГБ; SSD 512 ГБ", 2023, ProductType.Goods),
            ("ART-0003", "Logitech", electro, 450m, 520m,
                "Беспроводная; 1000 DPI; USB-приёмник; питание от батарейки AA", 2022, ProductType.Goods),
            ("ART-0004", "A4Tech", electro, 600m, 700m,
                "Проводная; USB; 104 клавиши; защита от влаги", 2022, ProductType.Goods),
            ("ART-0005", "Samsung", techno, 9500m, 10500m,
                "24\"; матрица IPS; 1920x1080; 75 Гц; HDMI и VGA", 2023, ProductType.Goods),
            ("ART-0006", "LG", techno, 14500m, 16000m,
                "27\"; матрица IPS; 2560x1440; 75 Гц; HDMI и DisplayPort", 2024, ProductType.Goods),
            ("ART-0007", "Kingston", electro, 2100m, 2400m,
                "DDR4; 8 ГБ; 3200 МГц; CL22", 2022, ProductType.Goods),
            ("ART-0008", "Samsung", electro, 3900m, 4400m,
                "SATA III; 512 ГБ; скорость чтения до 550 МБ/с", 2023, ProductType.Goods),
            ("ART-0009", "TP-Link", sidorov, 2300m, 2600m,
                "Wi-Fi 5; до 1200 Мбит/с; 4 порта LAN; два диапазона", 2023, ProductType.Equipment),
            ("ART-0010", "Hyperline", sidorov, 120m, 150m,
                "Категория 6; UTP; длина 2 м; разъёмы RJ-45", 2022, ProductType.Material)
        };

        foreach (var detail in details)
        {
            var product = context.Products.FirstOrDefault(p => p.Article == detail.Article);
            if (product == null)
            {
                continue;
            }

            // Не трогаем товары, у которых данные уже заполнены
            if (product.Manufacturer != null || product.PurchasePrice != null ||
                product.AccountingPrice != null || product.Characteristics != null)
            {
                continue;
            }

            product.Manufacturer = detail.Manufacturer;
            product.SupplierId = detail.SupplierId;
            product.PurchasePrice = detail.Purchase;
            product.AccountingPrice = detail.Accounting;
            product.Characteristics = detail.Characteristics;
            product.ReleaseYear ??= detail.Year;
            product.ItemType = detail.Type;
        }

        context.SaveChanges();
    }

    private static void SeedRoleUsers(WmsDbContext context)
    {
        var mainWarehouse = context.Warehouses.FirstOrDefault(w => w.Name == "Основной склад")
                            ?? context.Warehouses.OrderBy(w => w.Id).FirstOrDefault();

        // Для специалиста по инвентаризации берём второй склад, если он есть
        var secondWarehouse = context.Warehouses.FirstOrDefault(w => w.Name == "Склад в ПМР")
                              ?? mainWarehouse;

        var testUsers = new List<User>
        {
            new()
            {
                Login = "director",
                PasswordHash = PasswordHasher.Hash("director123"),
                FullName = "Смирнов Алексей Петрович",
                Role = UserRole.Director
            },
            new()
            {
                Login = "manager",
                PasswordHash = PasswordHasher.Hash("manager123"),
                FullName = "Козлов Дмитрий Андреевич",
                Role = UserRole.WarehouseManager,
                WarehouseId = mainWarehouse?.Id
            },
            new()
            {
                Login = "buyer",
                PasswordHash = PasswordHasher.Hash("buyer123"),
                FullName = "Новикова Марина Олеговна",
                Role = UserRole.PurchasingManager
            },
            new()
            {
                Login = "seller",
                PasswordHash = PasswordHasher.Hash("seller123"),
                FullName = "Волков Игорь Сергеевич",
                Role = UserRole.SalesManager
            },
            new()
            {
                Login = "auditor",
                PasswordHash = PasswordHasher.Hash("auditor123"),
                FullName = "Лебедева Анна Викторовна",
                Role = UserRole.InventorySpecialist,
                WarehouseId = secondWarehouse?.Id
            }
        };

        foreach (var user in testUsers)
        {
            if (!context.Users.Any(u => u.Login == user.Login))
            {
                context.Users.Add(user);
            }
        }

        context.SaveChanges();
    }

    private static void SeedCoreData(WmsDbContext context)
    {
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

        context.SaveChanges();
    }

    private static void SeedDemoOperations(WmsDbContext context)
    {
        var warehouse = context.Warehouses.First();
        var location1 = context.StorageLocations.OrderBy(l => l.Code).First();
        var location2 = context.StorageLocations.OrderBy(l => l.Code).Skip(1).First();
        var supplier = context.Suppliers.First();
        var product1 = context.Products.First(p => p.Article == "ART-0001");
        var product2 = context.Products.First(p => p.Article == "ART-0003");

        // --- Тестовая операция: приёмка (уже размещённая) ---
        var receipt = new Receipt
        {
            Supplier = supplier,
            Warehouse = warehouse,
            DocumentNumber = "REC-0001",
            Date = DateTime.UtcNow,
            ArrivalDate = DateOnly.FromDateTime(DateTime.Now),
            Status = ReceiptStatus.Confirmed,
            Items = new List<ReceiptItem>
        {
            new() { Product = product1, Quantity = 20, PlacedQuantity = 20 },
            new() { Product = product2, Quantity = 50, PlacedQuantity = 50 }
        }
        };
        context.Receipts.Add(receipt);

        // --- Тестовое размещение: остатки в ячейках ---
        var inventory1 = new Inventory { Product = product1, StorageLocation = location1, Quantity = 20 };
        var inventory2 = new Inventory { Product = product2, StorageLocation = location2, Quantity = 50 };
        context.Inventories.AddRange(inventory1, inventory2);

        // --- История движения для этой приёмки и размещения ---
        context.StockMovements.AddRange(
            new StockMovement
            {
                Product = product1,
                FromLocation = null,
                ToLocation = location1,
                Quantity = 20,
                WarehouseId = warehouse.Id,
                OperationType = OperationType.Receipt,
                Date = DateTime.UtcNow
            },
            new StockMovement
            {
                Product = product2,
                FromLocation = null,
                ToLocation = location2,
                Quantity = 50,
                WarehouseId = warehouse.Id,
                OperationType = OperationType.Receipt,
                Date = DateTime.UtcNow
            }
        );

        context.SaveChanges();
    }
}