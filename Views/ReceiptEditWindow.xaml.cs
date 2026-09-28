using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class ReceiptEditWindow : Window
{
    private readonly ReceiptEditViewModel _viewModel;

    public ReceiptEditWindow(ReceiptEditViewModel viewModel)
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