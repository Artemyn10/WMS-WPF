using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Product> Products { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                _ = LoadAsync();
            }
        }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetField(ref _selectedProduct, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public ProductsViewModel(ProductService productService, IServiceProvider serviceProvider)
    {
        _productService = productService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedProduct), _ => SelectedProduct != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedProduct != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _productService.GetAllAsync(SearchText);
        Products.Clear();
        foreach (var item in items)
        {
            Products.Add(item);
        }
    }

    private async Task OpenEditDialogAsync(Product? product)
    {
        var window = _serviceProvider.GetRequiredService<ProductEditWindow>();
        var viewModel = (ProductEditViewModel)window.DataContext;

        if (product == null)
        {
            await viewModel.InitializeForAddAsync();
        }
        else
        {
            await viewModel.InitializeForEditAsync(product);
        }

        var result = window.ShowDialog();
        if (result == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedProduct == null)
        {
            return;
        }

        var result = await _productService.DeleteAsync(SelectedProduct.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success)
        {
            await LoadAsync();
        }
    }
}