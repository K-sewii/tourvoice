namespace TourVoice.BLL.Translation;

public record TranslationChunkResult(string TranslatedText, int CharactersUsed, bool Success, string? ErrorCode);

public interface ITranslationProvider
{
    Task<TranslationChunkResult> TranslateAsync(
        string text, string sourceLang, string targetLang, CancellationToken ct = default);
}