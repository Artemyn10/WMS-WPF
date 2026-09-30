using System.Windows;
using WMS.ViewModels;

namespace WMS;

public partial class MainWindow : Window
{
    // true, если окно закрылось из-за выхода из учётной записи (а не выхода из приложения)
    public bool IsLogout { get; private set; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.LogoutRequested += () =>
        {
            IsLogout = true;
            Close();
        };

        viewModel.ExitRequested += () =>
        {
            IsLogout = false;
            Close();
        };
    }
}