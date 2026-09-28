namespace WMS.ViewModels;

// Временная заглушка для разделов, которые ещё не реализованы.
// Будет постепенно заменяться на реальные ViewModel'и на следующих этапах.
public class PlaceholderViewModel : ViewModelBase
{
    public string SectionTitle { get; }

    public PlaceholderViewModel(string sectionTitle)
    {
        SectionTitle = sectionTitle;
    }
}