using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class OrderEditWindow : Window
{
    private readonly OrderEditViewModel _viewModel;

    public OrderEditWindow(OrderEditViewModel viewModel)
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