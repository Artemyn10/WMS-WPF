using System.Windows;
using WMS.ViewModels;

namespace WMS.Views;

public partial class CategoryEditWindow : Window
{
    private readonly CategoryEditViewModel _viewModel;

    public CategoryEditWindow(CategoryEditViewModel viewModel)
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