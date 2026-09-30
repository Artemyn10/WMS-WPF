using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WMS.Data;
using WMS.Views;

namespace WMS;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

                services.AddDbContextFactory<WmsDbContext>(options =>
                    options.UseNpgsql(connectionString));

                services.AddTransient<Services.AuthService>();
                services.AddTransient<ViewModels.LoginViewModel>();
                services.AddTransient<Views.LoginWindow>();
                services.AddTransient<MainWindow>();
                services.AddSingleton<Services.NavigationService>();
                services.AddTransient<ViewModels.MainViewModel>();
                services.AddTransient<Services.ProductService>();
                services.AddTransient<Services.CategoryService>();

                services.AddTransient<ViewModels.ProductsViewModel>();
                services.AddTransient<ViewModels.CategoriesViewModel>();
                services.AddTransient<ViewModels.ProductEditViewModel>();
                services.AddTransient<ViewModels.CategoryEditViewModel>();

                services.AddTransient<Views.ProductEditWindow>();
                services.AddTransient<Views.CategoryEditWindow>();
                services.AddTransient<Services.WarehouseService>();
                services.AddTransient<Services.StorageLocationService>();

                services.AddTransient<ViewModels.WarehousesViewModel>();
                services.AddTransient<ViewModels.StorageLocationsViewModel>();
                services.AddTransient<ViewModels.WarehouseEditViewModel>();
                services.AddTransient<ViewModels.StorageLocationEditViewModel>();

                services.AddTransient<Views.WarehouseEditWindow>();
                services.AddTransient<Views.StorageLocationEditWindow>();
                services.AddTransient<Services.InventoryService>();
                services.AddTransient<ViewModels.InventoryViewModel>();
                services.AddTransient<Services.ReceiptService>();
                services.AddTransient<ViewModels.ReceiptsViewModel>();
                services.AddTransient<ViewModels.ReceiptEditViewModel>();
                services.AddTransient<Views.ReceiptEditWindow>();
                services.AddTransient<Services.SupplierService>();
                services.AddTransient<Services.PlacementService>();
                services.AddTransient<ViewModels.PlacementViewModel>();
                services.AddTransient<Services.MovementService>();
                services.AddTransient<ViewModels.MovementViewModel>();
                services.AddTransient<Services.CustomerService>();
                services.AddTransient<Services.OrderService>();
                services.AddTransient<ViewModels.OrdersViewModel>();
                services.AddTransient<ViewModels.OrderEditViewModel>();
                services.AddTransient<Views.OrderEditWindow>();
                services.AddTransient<Services.PickingService>();
                services.AddTransient<ViewModels.PickingViewModel>();
                services.AddTransient<Services.ShipmentService>();
                services.AddTransient<ViewModels.ShipmentViewModel>();
                services.AddTransient<Services.StockMovementService>();
                services.AddTransient<ViewModels.StockMovementHistoryViewModel>();
                services.AddTransient<Services.DashboardService>();
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<Services.UserService>();

                services.AddTransient<ViewModels.SuppliersViewModel>();
                services.AddTransient<ViewModels.SupplierEditViewModel>();
                services.AddTransient<Views.SupplierEditWindow>();

                services.AddTransient<ViewModels.CustomersViewModel>();
                services.AddTransient<ViewModels.CustomerEditViewModel>();
                services.AddTransient<Views.CustomerEditWindow>();

                services.AddTransient<ViewModels.UsersViewModel>();
                services.AddTransient<ViewModels.UserEditViewModel>();
                services.AddTransient<Views.UserEditWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost.StartAsync();

        var contextFactory = AppHost.Services.GetRequiredService<IDbContextFactory<Data.WmsDbContext>>();
        await using (var dbContext = await contextFactory.CreateDbContextAsync())
        {
            dbContext.Database.Migrate();
            Data.DbSeeder.Seed(dbContext);
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        ShowLoginThenMain();

        base.OnStartup(e);
    }

    private void ShowLoginThenMain()
    {
        var loginWindow = AppHost.Services.GetRequiredService<Views.LoginWindow>();
        var loginResult = loginWindow.ShowDialog();

        if (loginResult != true)
        {
            Shutdown();
            return;
        }

        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        mainWindow.Closed += MainWindow_Closed;
        mainWindow.Show();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var mainWindow = (MainWindow)sender!;
        mainWindow.Closed -= MainWindow_Closed;

        if (mainWindow.IsLogout)
        {
            Services.CurrentSession.SignOut();
            ShowLoginThenMain();
        }
        else
        {
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();
        base.OnExit(e);
    }
}