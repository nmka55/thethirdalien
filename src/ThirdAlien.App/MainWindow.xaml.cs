using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using ThirdAlien.Infrastructure;

namespace ThirdAlien.App;

public partial class MainWindow : Window
{
    private void Files_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var item in e.AddedItems.OfType<AudioFileItem>()) item.IsSelectedForAlbum = true;
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new AtlAudioTagService(),
            new ITunesCatalogProvider(new HttpClient { BaseAddress = new Uri("https://itunes.apple.com/") }));
    }
}
