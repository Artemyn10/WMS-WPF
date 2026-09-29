using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class SuppliersViewModel : ViewModelBase
{
    private readonly SupplierService _supplierService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetField(ref _selectedSupplier, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public SuppliersViewModel(SupplierService supplierService, IServiceProvider serviceProvider)
    {
        _supplierService = supplierService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedSupplier), _ => SelectedSupplier != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedSupplier != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _supplierService.GetAllAsync(SearchText);
        Suppliers.Clear();
        foreach (var item in items) Suppliers.Add(item);
    }

    private async Task OpenEditDialogAsync(Supplier? supplier)
    {
        var window = _serviceProvider.GetRequiredService<SupplierEditWindow>();
        var viewModel = (SupplierEditViewModel)window.DataContext;

        if (supplier == null) viewModel.InitializeForAdd();
        else viewModel.InitializeForEdit(supplier);

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedSupplier == null) return;

        var result = await _supplierService.DeleteAsync(SelectedSupplier.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}