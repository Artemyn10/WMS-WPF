using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class WriteOffEditWindow : Window
{
    private readonly WriteOffEditViewModel _viewModel;

    public WriteOffEditWindow(WriteOffEditViewModel viewModel)
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