using System.Collections.ObjectModel;
using WMS.Services;

namespace WMS.ViewModels;

public class WriteOffItemRowViewModel : ViewModelBase
{
    public ObservableCollection<StockOption> AvailableStock { get; }

    private StockOption? _selectedStock;
    public StockOption? SelectedStock
    {
        get => _selectedStock;
        set => SetField(ref _selectedStock, value);
    }

    private int _quantity = 1;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    public WriteOffItemRowViewModel(ObservableCollection<StockOption> availableStock)
    {
        AvailableStock = availableStock;
    }
}