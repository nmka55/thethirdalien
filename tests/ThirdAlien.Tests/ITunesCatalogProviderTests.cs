using System.Net;
using System.Net.Http.Headers;
using ThirdAlien.Infrastructure;

namespace ThirdAlien.Tests;

public sealed class ITunesCatalogProviderTests
{
    [Fact]
    public async Task SearchAlbumsAsyncUsesStrictAlbumQueryAndDoesNotRequestSongsWhenExactAlbumExists()
    {
        var handler = new QueueHandler(
            Json("""
            {"results":[{"wrapperType":"collection","collectionType":"Album","collectionId":1440818584,"collectionName":"Pure Heroine","artistName":"Lorde","trackCount":10,"discCount":1}]}
            """));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var provider = new ITunesCatalogProvider(client);

        var results = await provider.SearchAlbumsAsync("Pure Heroine", "US", includeExplicit: false);

        var album = Assert.Single(results);
        Assert.Equal("Pure Heroine", album.Name);
        Assert.Single(handler.Requests);
        var query = handler.Requests[0].RequestUri!.Query;
        Assert.Contains("media=music", query);
        Assert.Contains("entity=album", query);
        Assert.Contains("limit=70", query);
        Assert.Contains("country=US", query);
        Assert.Contains("explicit=no", query);
    }

    [Fact]
    public async Task SearchAlbumsAsyncUsesSongFallbackOnlyForAnExactCollectionTitle()
    {
        var handler = new QueueHandler(
            Json("""
            {"results":[{"wrapperType":"collection","collectionType":"Album","collectionId":1,"collectionName":"Pure Heroine Tribute","artistName":"Other"}]}
            """),
            Json("""
            {"results":[
              {"wrapperType":"track","kind":"song","collectionId":1440818584,"collectionName":"Pure Heroine","artistName":"Lorde","trackName":"Royals"},
              {"wrapperType":"track","kind":"song","collectionId":2,"collectionName":"Pure Heroine Tribute","artistName":"Other","trackName":"Cover"}
            ]}
            """));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var provider = new ITunesCatalogProvider(client);

        var results = await provider.SearchAlbumsAsync("Pure Heroine", "US", includeExplicit: false);

        Assert.Collection(results,
            album => Assert.Equal("Pure Heroine", album.Name),
            album => Assert.Equal("Pure Heroine Tribute", album.Name));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("entity=song", handler.Requests[1].RequestUri!.Query);
    }

    [Fact]
    public async Task DownloadArtworkAsyncRejectsNonImageResponses()
    {
        var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not image", new MediaTypeHeaderValue("text/plain"))
        });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var provider = new ITunesCatalogProvider(client);

        await Assert.ThrowsAsync<CatalogResponseException>(() => provider.DownloadArtworkAsync("https://example.test/cover"));
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, new MediaTypeHeaderValue("application/json"))
    };

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(this.responses.Dequeue());
        }
    }
}