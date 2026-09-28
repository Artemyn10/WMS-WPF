using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class ReceiptsViewModel : ViewModelBase
{
    private readonly ReceiptService _receiptService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Receipt> Receipts { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Receipt? _selectedReceipt;
    public Receipt? SelectedReceipt
    {
        get => _selectedReceipt;
        set => SetField(ref _selectedReceipt, value);
    }

    public ICommand AddCommand { get; }

    public ReceiptsViewModel(ReceiptService receiptService, IServiceProvider serviceProvider)
    {
        _receiptService = receiptService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenCreateDialogAsync());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _receiptService.GetAllAsync(SearchText);
        Receipts.Clear();
        foreach (var item in items) Receipts.Add(item);
    }

    private async Task OpenCreateDialogAsync()
    {
        var window = _serviceProvider.GetRequiredService<ReceiptEditWindow>();
        var viewModel = (ReceiptEditViewModel)window.DataContext;

        await viewModel.InitializeAsync();

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }
}