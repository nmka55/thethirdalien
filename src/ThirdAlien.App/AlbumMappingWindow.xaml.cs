using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ThirdAlien.Core;

namespace ThirdAlien.App;

public partial class AlbumMappingWindow : Window, INotifyPropertyChanged
{
    private FileMappingSlot? selectedSlot;

    public ObservableCollection<AlbumTrackSlot> TrackSlots { get; }
    public ObservableCollection<FileMappingSlot> FileSlots { get; }

    public FileMappingSlot? SelectedSlot
    {
        get => selectedSlot;
        set
        {
            if (ReferenceEquals(selectedSlot, value))
            {
                return;
            }

            selectedSlot = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSlot)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AlbumMappingWindow(IReadOnlyList<CatalogTrack> tracks, IEnumerable<AudioFileItem> files)
    {
        var orderedTracks = tracks.OrderBy(track => track.DiscNumber).ThenBy(track => track.TrackNumber).ToList();
        var selectedFiles = files.ToList();
        var rowCount = Math.Max(orderedTracks.Count, selectedFiles.Count);

        TrackSlots = new ObservableCollection<AlbumTrackSlot>(Enumerable.Range(0, rowCount)
            .Select(index => new AlbumTrackSlot(index + 1, index < orderedTracks.Count ? orderedTracks[index] : null)));
        FileSlots = new ObservableCollection<FileMappingSlot>(Enumerable.Range(0, rowCount)
            .Select(index => new FileMappingSlot(index + 1, index < selectedFiles.Count ? selectedFiles[index] : null)));

        InitializeComponent();
        DataContext = this;
    }

    public IReadOnlyList<(CatalogTrack Track, AudioFileItem File)> Mappings =>
        TrackSlots.Zip(FileSlots, (trackSlot, fileSlot) => (trackSlot.Track, fileSlot.File))
            .Where(pair => pair.Track is not null && pair.File is not null)
            .Select(pair => (pair.Track!, pair.File!))
            .ToList();

    public int UnmatchedTrackCount => TrackSlots.Zip(FileSlots, (trackSlot, fileSlot) => (trackSlot.Track, fileSlot.File))
        .Count(pair => pair.Track is not null && pair.File is null);

    public int UnmatchedFileCount => TrackSlots.Zip(FileSlots, (trackSlot, fileSlot) => (trackSlot.Track, fileSlot.File))
        .Count(pair => pair.Track is null && pair.File is not null);

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void MoveSelected(int offset)
    {
        if (SelectedSlot?.File is null)
        {
            return;
        }

        var from = FileSlots.IndexOf(SelectedSlot);
        var to = from + offset;
        if (to < 0 || to >= FileSlots.Count)
        {
            return;
        }

        (FileSlots[from].File, FileSlots[to].File) = (FileSlots[to].File, FileSlots[from].File);
        SelectedSlot = FileSlots[to];
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Mappings.Count == 0)
        {
            MessageBox.Show(this, "Place at least one selected file beside an album track before saving.", "Match album tracks", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (UnmatchedTrackCount > 0 || UnmatchedFileCount > 0)
        {
            var decision = MessageBox.Show(
                this,
                $"{UnmatchedFileCount} selected file(s) and {UnmatchedTrackCount} album track(s) are unmatched and will not be written. Continue with {Mappings.Count} matched pair(s)?",
                "Review unmatched rows",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (decision != MessageBoxResult.Yes)
            {
                return;
            }
        }

        DialogResult = true;
    }
}

public sealed class AlbumTrackSlot(int position, CatalogTrack? track)
{
    public int Position { get; } = position;
    public CatalogTrack? Track { get; } = track;
}

public sealed partial class FileMappingSlot(int position, AudioFileItem? file) : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public int Position { get; } = position;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private AudioFileItem? file = file;
}