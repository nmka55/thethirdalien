using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ThirdAlien.Core;
using ThirdAlien.Infrastructure;

namespace ThirdAlien.App;

public sealed partial class MainViewModel : ObservableObject
{
    private const string DefaultStorefront = "US";
    private readonly IAudioTagService audioTagService;
    private readonly ICatalogProvider catalogProvider;

    public MainViewModel(IAudioTagService audioTagService, ICatalogProvider catalogProvider)
    {
        this.audioTagService = audioTagService;
        this.catalogProvider = catalogProvider;
        ApplyTheme(SelectedTheme);
    }

    public ObservableCollection<AudioFileItem> Files { get; } = [];
    public ObservableCollection<CatalogAlbum> SearchResults { get; } = [];
    public IReadOnlyList<string> ThemeOptions { get; } = ["System", "Light", "Dark"];

    [ObservableProperty] private AudioFileItem? selectedFile;
    [ObservableProperty] private CatalogAlbum? selectedAlbum;
    [ObservableProperty] private string statusMessage = "Add audio files to begin.";
    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string artist = string.Empty;
    [ObservableProperty] private string album = string.Empty;
    [ObservableProperty] private string albumArtist = string.Empty;
    [ObservableProperty] private string genre = string.Empty;
    [ObservableProperty] private string composer = string.Empty;
    [ObservableProperty] private string lyrics = string.Empty;
    [ObservableProperty] private string year = string.Empty;
    [ObservableProperty] private string trackNumber = string.Empty;
    [ObservableProperty] private string trackTotal = string.Empty;
    [ObservableProperty] private string discNumber = string.Empty;
    [ObservableProperty] private string discTotal = string.Empty;
    [ObservableProperty] private BitmapImage? frontCoverImage;
    [ObservableProperty] private bool hasFrontCover;
    [ObservableProperty] private bool allFilesSelected;

    public bool HasSelectedFile => SelectedFile is not null;

    public string SelectionSummary =>
        $"{Files.Count(file => file.IsSelectedForAlbum)} of {Files.Count} selected | " +
        $"{Files.Count(file => file.PendingPatch is { Changes.Count: > 0 })} staged | " +
        $"{Files.Count(file => string.Equals(file.State, "Saved", StringComparison.Ordinal) || string.Equals(file.State, "Album tags saved", StringComparison.Ordinal))} saved";

    private void TrackFileForStatus(AudioFileItem file)
    {
        file.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(SelectionSummary));
    }

    partial void OnSelectedFileChanged(AudioFileItem? value)
    {
        LoadEditor(value?.Snapshot.Metadata);
        FrontCoverImage = ToBitmapImage(value?.Snapshot.FrontCover);
        HasFrontCover = FrontCoverImage is not null;
        OnPropertyChanged(nameof(HasSelectedFile));
    }

    partial void OnSelectedThemeChanged(string value) => ApplyTheme(value);

    partial void OnAllFilesSelectedChanged(bool value)
    {
        foreach (var file in Files)
        {
            file.IsSelectedForAlbum = value;
        }
    }

    [RelayCommand]
    private async Task ChooseArtworkAsync()
    {
        if (SelectedFile is null)
        {
            StatusMessage = "Select a file before choosing its cover image.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Choose front cover artwork",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var imageBytes = await File.ReadAllBytesAsync(dialog.FileName);
            var result = await audioTagService.ReplaceFrontCoverAsync(SelectedFile.Snapshot.Path, imageBytes);
            SelectedFile.Snapshot = result.Snapshot;
            SelectedFile.PendingPatch = null;
            SelectedFile.State = "Saved";
            FrontCoverImage = ToBitmapImage(result.Snapshot.FrontCover);
            HasFrontCover = FrontCoverImage is not null;
            StatusMessage = $"Saved and verified cover artwork. Backup: {Path.GetFileName(result.BackupPath)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ValidationException or NotSupportedException)
        {
            StatusMessage = $"Artwork was not saved: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenFullTagsAsync()
    {
        if (SelectedFile is null)
        {
            StatusMessage = "Select a file before opening its full tag list.";
            return;
        }

        var dialog = new FullTagsWindow(SelectedFile.Snapshot.Metadata) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var result = await audioTagService.ApplyFullTagsAsync(SelectedFile.Snapshot.Path, dialog.TagsToSave);
            SelectedFile.Snapshot = result.Snapshot;
            SelectedFile.PendingPatch = null;
            SelectedFile.State = "Saved";
            LoadEditor(result.Snapshot.Metadata);
            FrontCoverImage = ToBitmapImage(result.Snapshot.FrontCover);
            HasFrontCover = FrontCoverImage is not null;
            StatusMessage = $"Saved and verified full tags. Backup: {Path.GetFileName(result.BackupPath)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ValidationException or NotSupportedException)
        {
            StatusMessage = $"Full tags were not saved: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add audio files",
            Multiselect = true,
            Filter = "Supported audio|*.mp3;*.m4a;*.aac;*.flac;*.ogg;*.oga;*.opus;*.wav;*.aiff;*.aif;*.wma;*.m4b|All files|*.*"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var knownPaths = new HashSet<string>(Files.Select(file => file.Snapshot.Path), StringComparer.OrdinalIgnoreCase);
        var added = 0;
        foreach (var path in dialog.FileNames.Where(path => knownPaths.Add(Path.GetFullPath(path))))
        {
            try
            {
                var file = new AudioFileItem(await audioTagService.ReadAsync(path))
                {
                    IsSelectedForAlbum = AllFilesSelected
                };
                Files.Add(file);
                TrackFileForStatus(file);
                added++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                StatusMessage = $"Could not add {Path.GetFileName(path)}: {exception.Message}";
            }
        }

        StatusMessage = added > 0 ? $"Added {added} audio file(s)." : "No files were added.";
    }

    [RelayCommand]
    private void StageChanges()
    {
        if (SelectedFile is null)
        {
            StatusMessage = "Select a file before staging changes.";
            return;
        }

        var patch = CreatePatch(SelectedFile.Snapshot.Metadata);
        SelectedFile.PendingPatch = patch;
        SelectedFile.State = patch.Changes.Count == 0 ? "No changes" : $"{patch.Changes.Count} staged";
        StatusMessage = patch.Changes.Count == 0 ? "No changes to stage." : $"Staged {patch.Changes.Count} change(s). Review the fields, then save.";
    }

    [RelayCommand]
    private async Task SaveStagedChangesAsync()
    {
        if (SelectedFile?.PendingPatch is not { Changes.Count: > 0 } patch)
        {
            StatusMessage = "Stage a change for the selected file before saving.";
            return;
        }

        try
        {
            StatusMessage = $"Saving {SelectedFile.FileName}...";
            var result = await audioTagService.ApplyAsync(SelectedFile.Snapshot.Path, patch);
            SelectedFile.Snapshot = result.Snapshot;
            SelectedFile.PendingPatch = null;
            SelectedFile.State = "Saved";
            LoadEditor(result.Snapshot.Metadata);
            FrontCoverImage = ToBitmapImage(result.Snapshot.FrontCover);
            HasFrontCover = FrontCoverImage is not null;
            StatusMessage = $"Saved and verified. Backup: {Path.GetFileName(result.BackupPath)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ValidationException or NotSupportedException)
        {
            SelectedFile.State = "Save failed";
            StatusMessage = $"Nothing was committed: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchAlbumsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SelectedFile?.Snapshot.Metadata.Album ?? SelectedFile?.Snapshot.Metadata.Artist ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            StatusMessage = "Enter an artist or album to search.";
            return;
        }

        try
        {
            StatusMessage = "Searching the iTunes catalog...";
            var albums = await catalogProvider.SearchAlbumsAsync(SearchTerm, DefaultStorefront, includeExplicit: false);
            SearchResults.Clear();
            foreach (var catalogAlbum in albums)
            {
                SearchResults.Add(catalogAlbum);
            }

            StatusMessage = $"Found {albums.Count} album edition(s).";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or CatalogRateLimitedException or CatalogResponseException)
        {
            StatusMessage = $"Catalog search failed: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadAlbumAsync(CatalogAlbum? catalogAlbum)
    {
        if (catalogAlbum is null)
        {
            return;
        }

        try
        {
            var selectedFiles = Files.Where(file => file.IsSelectedForAlbum).ToList();
            if (selectedFiles.Count == 0)
            {
                StatusMessage = "Check one or more files before loading an album.";
                return;
            }

            var detail = await catalogProvider.GetAlbumAsync(catalogAlbum.CollectionId, catalogAlbum.Storefront);
            var dialog = new AlbumMappingWindow(detail.Tracks, selectedFiles) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                await WriteAlbumMappingsAsync(detail.Album, dialog);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or CatalogNotFoundException or CatalogRateLimitedException or CatalogResponseException)
        {
            StatusMessage = $"Album lookup failed: {exception.Message}";
        }
    }

    private async Task WriteAlbumMappingsAsync(CatalogAlbum albumDetail, AlbumMappingWindow dialog)
    {
        byte[]? artwork = null;
        var artworkError = false;
        if (!string.IsNullOrWhiteSpace(albumDetail.ArtworkUrl))
        {
            try
            {
                artwork = await catalogProvider.DownloadArtworkAsync(albumDetail.ArtworkUrl);
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or CatalogResponseException)
            {
                artworkError = true;
                StatusMessage = $"Artwork could not be downloaded; saving text tags only: {exception.Message}";
            }
        }

        var saved = 0;
        var failed = 0;
        var cleanedUrlFields = 0;
        foreach (var (track, file) in dialog.Mappings)
        {
            try
            {
                var patchResult = CreateAlbumPatch(track, file.Snapshot.Metadata);
                cleanedUrlFields += patchResult.CleanedUrlFields;
                var result = await audioTagService.ApplyAsync(file.Snapshot.Path, patchResult.Patch, artwork);
                file.Snapshot = result.Snapshot;
                file.PendingPatch = null;
                file.State = "Album tags saved";
                saved++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ValidationException or NotSupportedException)
            {
                file.State = "Save failed";
                failed++;
                StatusMessage = $"Could not save {file.FileName}: {exception.Message}";
            }
        }

        var unmatched = dialog.UnmatchedFileCount + dialog.UnmatchedTrackCount;
        var artworkText = artworkError ? " Artwork was not saved." : artwork is null ? " No artwork was supplied." : " Artwork saved.";
        StatusMessage = $"Album tagging complete: {saved} saved | {failed} failed | {unmatched} unmatched | {cleanedUrlFields} URL field(s) cleaned.{artworkText}";
    }

    private static (MetadataPatch Patch, int CleanedUrlFields) CreateAlbumPatch(CatalogTrack track, EditableMetadata original)
    {
        var changes = new List<MetadataChange>
        {
            MetadataChange.Set(MetadataField.Title, track.Name),
            MetadataChange.Set(MetadataField.Artist, track.Artist),
            MetadataChange.Set(MetadataField.Album, track.Album),
            MetadataChange.Set(MetadataField.AlbumArtist, track.AlbumArtist ?? track.Artist)
        };
        if (track.TrackNumber is { } trackNumber) changes.Add(MetadataChange.Set(MetadataField.TrackNumber, trackNumber.ToString(CultureInfo.InvariantCulture)));
        if (track.TrackTotal is { } trackTotal) changes.Add(MetadataChange.Set(MetadataField.TrackTotal, trackTotal.ToString(CultureInfo.InvariantCulture)));
        if (track.DiscNumber is { } discNumber) changes.Add(MetadataChange.Set(MetadataField.DiscNumber, discNumber.ToString(CultureInfo.InvariantCulture)));
        if (track.DiscTotal is { } discTotal) changes.Add(MetadataChange.Set(MetadataField.DiscTotal, discTotal.ToString(CultureInfo.InvariantCulture)));
        if (!string.IsNullOrWhiteSpace(track.Genre)) changes.Add(MetadataChange.Set(MetadataField.Genre, track.Genre));
        if (!string.IsNullOrWhiteSpace(track.Copyright)) changes.Add(MetadataChange.Set(MetadataField.Copyright, track.Copyright));
        if (track.ReleaseDate is { } releaseDate) changes.Add(MetadataChange.Set(MetadataField.Year, releaseDate.Year.ToString(CultureInfo.InvariantCulture)));

        var cleaned = 0;
        AddUrlCleanupChange(changes, MetadataField.Composer, original.Composer, ref cleaned);
        AddUrlCleanupChange(changes, MetadataField.Lyrics, original.Lyrics, ref cleaned);
        AddUrlCleanupChange(changes, MetadataField.Website, original.Website, ref cleaned);
        return new(new MetadataPatch(changes), cleaned);
    }

    private static void AddUrlCleanupChange(List<MetadataChange> changes, MetadataField field, string? original, ref int cleanedCount)
    {
        var cleanup = WebsiteUrlCleaner.Clean(original);
        if (!cleanup.Changed)
        {
            return;
        }

        changes.Add(cleanup.Value is null ? MetadataChange.Clear(field) : MetadataChange.Set(field, cleanup.Value));
        cleanedCount += cleanup.RemovedCount;
    }

    private void LoadEditor(EditableMetadata? metadata)
    {
        Title = metadata?.Title ?? string.Empty;
        Artist = metadata?.Artist ?? string.Empty;
        Album = metadata?.Album ?? string.Empty;
        AlbumArtist = metadata?.AlbumArtist ?? string.Empty;
        Genre = metadata?.Genre ?? string.Empty;
        Composer = metadata?.Composer ?? string.Empty;
        Lyrics = metadata?.Lyrics ?? string.Empty;
        Year = metadata?.Year ?? string.Empty;
        TrackNumber = metadata?.TrackNumber ?? string.Empty;
        TrackTotal = metadata?.TrackTotal ?? string.Empty;
        DiscNumber = metadata?.DiscNumber ?? string.Empty;
        DiscTotal = metadata?.DiscTotal ?? string.Empty;
    }

    private MetadataPatch CreatePatch(EditableMetadata original)
    {
        var changes = new List<MetadataChange>();
        AddChange(changes, MetadataField.Title, original.Title, Title);
        AddChange(changes, MetadataField.Artist, original.Artist, Artist);
        AddChange(changes, MetadataField.Album, original.Album, Album);
        AddChange(changes, MetadataField.AlbumArtist, original.AlbumArtist, AlbumArtist);
        AddChange(changes, MetadataField.Genre, original.Genre, Genre);
        AddChange(changes, MetadataField.Composer, original.Composer, Composer);
        AddChange(changes, MetadataField.Lyrics, original.Lyrics, Lyrics);
        AddChange(changes, MetadataField.Year, original.Year, Year);
        AddChange(changes, MetadataField.TrackNumber, original.TrackNumber, TrackNumber);
        AddChange(changes, MetadataField.TrackTotal, original.TrackTotal, TrackTotal);
        AddChange(changes, MetadataField.DiscNumber, original.DiscNumber, DiscNumber);
        AddChange(changes, MetadataField.DiscTotal, original.DiscTotal, DiscTotal);
        return new(changes);
    }

    private static void AddChange(List<MetadataChange> changes, MetadataField field, string? original, string current)
    {
        var normalized = string.IsNullOrWhiteSpace(current) ? null : current.Trim();
        if (!string.Equals(original, normalized, StringComparison.Ordinal))
        {
            changes.Add(normalized is null ? MetadataChange.Clear(field) : MetadataChange.Set(field, normalized));
        }
    }

    private static BitmapImage? ToBitmapImage(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static void ApplyTheme(string theme)
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        if (string.Equals(theme, "System", StringComparison.Ordinal) && SystemParameters.HighContrast)
        {
            application.Resources["AppBackground"] = SystemColors.WindowBrush;
            application.Resources["PanelBackground"] = SystemColors.WindowBrush;
            application.Resources["ForegroundBrush"] = SystemColors.WindowTextBrush;
            application.Resources["MutedForegroundBrush"] = SystemColors.GrayTextBrush;
            application.Resources["InputBackground"] = SystemColors.WindowBrush;
            application.Resources["InputBorderBrush"] = SystemColors.WindowTextBrush;
            application.Resources["TableHeaderBackground"] = SystemColors.ControlBrush;
            application.Resources["AlternateRowBackground"] = SystemColors.WindowBrush;
            application.Resources["SelectedRowBackground"] = SystemColors.HighlightBrush;
            application.Resources["SelectedRowForeground"] = SystemColors.HighlightTextBrush;
            return;
        }

        var dark = string.Equals(theme, "Dark", StringComparison.Ordinal)
            || (string.Equals(theme, "System", StringComparison.Ordinal) && IsSystemDark());
        application.Resources["AppBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF1C1B1F" : "#FFF7F7F9"));
        application.Resources["PanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF252329" : "#FFFFFFFF"));
        application.Resources["ForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FFE6E1E5" : "#FF1A1A1A"));
        application.Resources["MutedForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FFCAC4D0" : "#FF5F6368"));
        application.Resources["InputBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF302D35" : "#FFFFFFFF"));
        application.Resources["InputBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF938F99" : "#FF767680"));
        application.Resources["TableHeaderBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF403D46" : "#FFE8E4EC"));
        application.Resources["AlternateRowBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF302D35" : "#FFF8F6FA"));
        application.Resources["SelectedRowBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FF6750A4" : "#FFD7C7FF"));
        application.Resources["SelectedRowForeground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#FFFFFFFF" : "#FF1A1A1A"));
    }

    private static bool IsSystemDark() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int value
        && value == 0;
}

public sealed partial class AudioFileItem(AudioFileSnapshot snapshot) : ObservableObject
{
    [ObservableProperty] private AudioFileSnapshot snapshot = snapshot;
    [ObservableProperty] private MetadataPatch? pendingPatch;
    [ObservableProperty] private string state = "Ready";
    [ObservableProperty] private bool isSelectedForAlbum;

    public string FileName => Path.GetFileName(Snapshot.Path);
}