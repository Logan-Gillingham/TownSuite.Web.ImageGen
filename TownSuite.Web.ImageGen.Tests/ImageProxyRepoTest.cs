using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;

namespace TownSuite.Web.ImageGen.Tests;

[TestFixture]
public class ImageProxyRepoTest
{
    [SetUp]
    public void Setup()
    {
    }

    private static string[] ImageFormatCases = new string[] { "jpeg", "png", "gif", "webp" };
    
    [Test, TestCaseSource("ImageFormatCases")]
    public async Task Test1(string imageformat)
    {
        var origImage = await Image.LoadAsync("assets/facility.jpg");
        Assert.That(origImage.Height, Is.EqualTo(365));
        Assert.That(origImage.Width, Is.EqualTo(800));
        
        var downloader = new DownloaderFake("image/jpeg");
        var repo = new ImageProxyRepo(downloader, new Settings()
        {
            HttpCacheControlMaxAgeInMinutes = 5
        });
        var request = new ImageProxyRequestMetaData();
        var query = CreateContext(333, 333, imageformat);
        
        request.GetRequestMetaData(new Settings()
            {
                CacheFolder = "cache/folder/",
                MaxHeight = 1000,
                MaxWidth = 1000
            },
            query.query,
            query.rawQuery,
            "/proxy/assets%2Ffacility.jpg",
            "test_output");
        var results = await repo.Get(request);

        using var ms = new MemoryStream(results.imageData);
        var newImage = await Image.LoadAsync(ms);
        Assert.That(results.metadata.ContentType, Is.EqualTo($"image/{imageformat}"));
        Assert.That(newImage.Height, Is.EqualTo(333));
        Assert.That(newImage.Width, Is.EqualTo(333));
    }

    [Test, TestCaseSource("ImageFormatCases")]
    public async Task ImageMaxResizeCanOnlyBeEqualOrLessThanImageOriginalSizeTest(string imageformat)
    {
        var origImage = await Image.LoadAsync("assets/facility.jpg");
        Assert.That(origImage.Height, Is.EqualTo(365));
        Assert.That(origImage.Width, Is.EqualTo(800));
        
        var downloader = new DownloaderFake("image/jpeg");
        var repo = new ImageProxyRepo(downloader, new Settings()
        {
            HttpCacheControlMaxAgeInMinutes = 5
        });
        var request = new ImageProxyRequestMetaData();
        var query = CreateContext(888, 888, imageformat);
        
        request.GetRequestMetaData(new Settings()
            {
                CacheFolder = "cache/folder/",
                MaxHeight = 1000,
                MaxWidth = 1000
            },
            query.query,
            query.rawQuery,
            "/proxy/assets%2Ffacility.jpg",
            "test_output");
        var results = await repo.Get(request);

        using var ms = new MemoryStream(results.imageData);
        var newImage = await Image.LoadAsync(ms);
        Assert.That(results.metadata.ContentType, Is.EqualTo($"image/{imageformat}"));
        Assert.That(newImage.Height, Is.EqualTo(365));
        Assert.That(newImage.Width, Is.EqualTo(800));
    }
    
    private (QueryCollection query, string rawQuery) CreateContext(int h, int w,string imgformat)
    {
        var queries = new Dictionary<string, StringValues>
        {
            { "h", h.ToString() },
            { "w", w.ToString() },
            { "imgformat", imgformat }
        };

        
        return (new QueryCollection(queries), $"?h={h}&w={w}&imgformat={imgformat}");
    }

    [Test()]
    public async Task SVGTEST()
    {
        var downloader = new DownloaderFake("image/svg+xml");
        var repo = new ImageProxyRepo(downloader, new Settings()
        {
            HttpCacheControlMaxAgeInMinutes = 5
        });
        var request = new ImageProxyRequestMetaData();
        var query = CreateContext(333, 333, "svg");

        request.GetRequestMetaData(new Settings()
        {
            CacheFolder = "cache/folder/",
            MaxHeight = 1000,
            MaxWidth = 1000
        },
            query.query,
            query.rawQuery,
            "/proxy/assets%2Flogo.svg",
            "test_output");
        var results = await repo.Get(request);

        using var ms = new MemoryStream(results.imageData);
        string image = Encoding.UTF8.GetString(ms.ToArray());
        Assert.That(results.metadata.ContentType, Is.EqualTo($"image/svg+xml"));
        Assert.That(image.Contains("svg version=\"1.1\""), Is.EqualTo(true));
    }
}