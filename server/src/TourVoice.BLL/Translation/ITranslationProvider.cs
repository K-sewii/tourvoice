namespace TourVoice.BLL.Translation;

/// <param name="ErrorCode"><see cref="QuotaExceededCode"/> khi het han muc, nguoc lai ma loi cua nha cung cap.</param>
/// <param name="Match">Diem match 0..1 MyMemory tra ve (neu co).</param>
public record TranslationChunkResult(
    string TranslatedText, int CharactersUsed, bool Success, string? ErrorCode, decimal? Match = null)
{
    public const string QuotaExceededCode = "quota_exceeded";
}

public interface ITranslationProvider
{
    Task<TranslationChunkResult> TranslateAsync(
        string text, string sourceLang, string targetLang, CancellationToken ct = default);
}
