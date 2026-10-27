using TourVoice.BLL.Storage;

namespace TourVoice.API.Infrastructure;

/// <summary>
/// Luu anh len dia (mount volume khi chay docker) va phuc vu o /uploads.
/// URL tra ve la tuyet doi de web khach dung thang lam src ma khong can biet domain API;
/// dat <c>Uploads:PublicBaseUrl</c> khi API nam sau reverse proxy.
/// </summary>
public sealed class LocalFileStorage(IConfiguration configuration, IWebHostEnvironment env, IHttpContextAccessor http)
    : IFileStorage
{
    public const string RequestPath = "/uploads";

    public static string ResolveRoot(IConfiguration configuration, string contentRoot) =>
        Path.GetFullPath(configuration["Uploads:Path"] ?? Path.Combine(contentRoot, "uploads"));

    private string Root => ResolveRoot(configuration, env.ContentRootPath);

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
    {
        var dir = Path.Combine(Root, folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var file = File.Create(Path.Combine(dir, fileName)))
            await content.CopyToAsync(file, ct);

        return $"{BaseUrl()}{RequestPath}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !uri.AbsolutePath.StartsWith(RequestPath + "/", StringComparison.Ordinal))
            return Task.CompletedTask;   // anh ngoai (CDN...) khong phai cua minh

        var relative = Uri.UnescapeDataString(uri.AbsolutePath[(RequestPath.Length + 1)..]);
        var path = Path.GetFullPath(Path.Combine(Root, relative));
        // Chong ../ thoat khoi thu muc upload.
        if (path.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string BaseUrl()
    {
        var configured = configuration["Uploads:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.TrimEnd('/');
        var request = http.HttpContext?.Request
            ?? throw new InvalidOperationException("Can HttpContext hoac Uploads:PublicBaseUrl de tao URL anh.");
        return $"{request.Scheme}://{request.Host}";
    }
}
