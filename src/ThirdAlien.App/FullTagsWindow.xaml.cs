using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using ThirdAlien.Core;

namespace ThirdAlien.App;

public partial class FullTagsWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<FullTagItem> Tags { get; } = [];
    private FullTagItem? selectedTag;

    public FullTagItem? SelectedTag
    {
        get => selectedTag;
        set
        {
            if (ReferenceEquals(selectedTag, value)) return;
            selectedTag = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTag)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelectedTag)));
        }
    }

    public bool HasSelectedTag => SelectedTag is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyDictionary<string, string> TagsToSave { get; private set; } = new Dictionary<string, string>();

    public FullTagsWindow(EditableMetadata metadata)
    {
        AddIfPresent("Title", metadata.Title);
        AddIfPresent("Artist", metadata.Artist);
        AddIfPresent("Album", metadata.Album);
        AddIfPresent("Album artist", metadata.AlbumArtist);
        AddIfPresent("Genre", metadata.Genre);
        AddIfPresent("Composer", metadata.Composer);
        AddIfPresent("Unsynchronized lyrics", metadata.Lyrics);
        AddIfPresent("Copyright", metadata.Copyright);
        AddIfPresent("Year", metadata.Year);
        AddIfPresent("Track number", metadata.TrackNumber);
        AddIfPresent("Track total", metadata.TrackTotal);
        AddIfPresent("Disc number", metadata.DiscNumber);
        AddIfPresent("Disc total", metadata.DiscTotal);
        AddIfPresent("Website", metadata.Website);
        AddIfPresent("ISRC", metadata.Isrc);
        foreach (var (name, value) in (metadata.AdditionalFields ?? new Dictionary<string, string>()).OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!Tags.Any(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                AddIfPresent(name, value);
            }
        }

        InitializeComponent();
        DataContext = this;
    }

    private void AddIfPresent(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            Tags.Add(new FullTagItem(name, value));
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var item = new FullTagItem(string.Empty, string.Empty) { IsEditing = true };
        Tags.Add(item);
        SelectedTag = item;
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTag is not null)
        {
            SelectedTag.IsEditing = true;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var invalid = Tags.FirstOrDefault(tag => string.IsNullOrWhiteSpace(tag.Name));
        if (invalid is not null)
        {
            MessageBox.Show(this, "Every tag needs a field name.", "Full tags", MessageBoxButton.OK, MessageBoxImage.Warning);
            SelectedTag = invalid;
            invalid.IsEditing = true;
            return;
        }

        var duplicate = Tags.GroupBy(tag => tag.Name.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            MessageBox.Show(this, $"The tag '{duplicate.Key}' appears more than once.", "Full tags", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TagsToSave = Tags.ToDictionary(tag => tag.Name.Trim(), tag => tag.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        DialogResult = true;
    }
}

public sealed partial class FullTagItem(string name, string value) : ObservableObject
{
    [ObservableProperty] private string name = name;
    [ObservableProperty] private string value = value;
    [ObservableProperty] private bool isEditing;
}