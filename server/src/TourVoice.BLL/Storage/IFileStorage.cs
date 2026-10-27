namespace TourVoice.BLL.Storage;

/// <summary>Luu file upload (anh bia). Cai dat o Presentation de BLL khong phu thuoc ha tang/web.</summary>
public interface IFileStorage
{
    /// <summary>Luu <paramref name="content"/> vao thu muc con, tra URL cong khai (tuyet doi) cua file.</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default);

    /// <summary>Xoa file theo URL da tra o SaveAsync; URL la (nguon ngoai) thi bo qua.</summary>
    Task DeleteAsync(string url);
}
