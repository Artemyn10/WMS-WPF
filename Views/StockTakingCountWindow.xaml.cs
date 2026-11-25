using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class StockTakingCountWindow : Window
{
    private readonly StockTakingCountViewModel _viewModel;

    public StockTakingCountWindow(StockTakingCountViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.RequestClose += result =>
        {
            DialogResult = result;
            Close();
        };
    }
}