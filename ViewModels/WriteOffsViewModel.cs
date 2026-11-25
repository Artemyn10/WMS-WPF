using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class WriteOffsViewModel : ViewModelBase
{
    private readonly WriteOffService _writeOffService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<WriteOff> WriteOffs { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private WriteOff? _selectedWriteOff;
    public WriteOff? SelectedWriteOff
    {
        get => _selectedWriteOff;
        set => SetField(ref _selectedWriteOff, value);
    }

    public ICommand AddCommand { get; }

    public WriteOffsViewModel(WriteOffService writeOffService, IServiceProvider serviceProvider)
    {
        _writeOffService = writeOffService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenCreateDialogAsync());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _writeOffService.GetAllAsync(SearchText);
        WriteOffs.Clear();
        foreach (var item in items) WriteOffs.Add(item);
    }

    private async Task OpenCreateDialogAsync()
    {
        var window = _serviceProvider.GetRequiredService<WriteOffEditWindow>();
        var viewModel = (WriteOffEditViewModel)window.DataContext;

        await viewModel.InitializeAsync();

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }
}