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

        // Chong ../ hoac path traversal - kiem tra duong dan duoc gioi han trong thu muc Root
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(dir, safeFileName);
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("File path vuot qua gioi han thu muc upload.");

        await using (var file = File.Create(normalizedPath))
            await content.CopyToAsync(file, ct);

        return $"{BaseUrl()}{RequestPath}/{folder}/{safeFileName}";
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
            var request = http.HttpContext?.Request;
            if (request != null) return $"{request.Scheme}://{request.Host}";
            // Fallback khi chay trong test/unit: tra ve duong dan relative
            return "/uploads";
        }
}
