using System.Text;
using System.Text.RegularExpressions;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Translation;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class NarrationPipelineService : INarrationPipelineService
{
    private const int DailyQuotaLimit = 50_000;      // han muc MyMemory/ngay voi email
    private const double BlockThresholdRatio = 0.9;  // BR-06: chan khi dat 90%
    private const int MaxChunkLength = 450;
    private const string SourceLanguage = "vi";

    private readonly IPlaceRepository _placeRepository;
    private readonly ILanguageRepository _languageRepository;
    private readonly IUsageLogRepository _usageLogRepository;
    private readonly ITranslationProvider _translationProvider;

    public NarrationPipelineService(
        IPlaceRepository placeRepository,
        ILanguageRepository languageRepository,
        IUsageLogRepository usageLogRepository,
        ITranslationProvider translationProvider)
    {
        _placeRepository = placeRepository;
        _languageRepository = languageRepository;
        _usageLogRepository = usageLogRepository;
        _translationProvider = translationProvider;
    }

    public async Task<TranslatePlaceResultDto> TranslatePlaceAsync(int placeId)
    {
        var place = await _placeRepository.GetByIdAsync(placeId)
            ?? throw new PlaceNotFoundException();

        var source = place.Translations.FirstOrDefault(t => t.LanguageCode == SourceLanguage);
        if (source is null || string.IsNullOrWhiteSpace(source.NarrationText))
            throw new MissingSourceNarrationException();

        var targetLanguages = (await _languageRepository.GetEnabledAsync())
            .Where(l => l.Code != SourceLanguage)
            .ToList();

        var chunks = ChunkText(source.NarrationText);
        var estimatedCharacters = source.NarrationText.Length * targetLanguages.Count;

        var usedToday = await _usageLogRepository.GetTodayCharacterTotalAsync(UsageService.Translator);
        var blockThreshold = (int)(DailyQuotaLimit * BlockThresholdRatio);
        if (usedToday + estimatedCharacters > blockThreshold)
            throw new QuotaExceededException();

        var results = new List<LanguageTranslationResultDto>();

        foreach (var language in targetLanguages)
        {
            var translatedParts = new List<string>();
            var charactersUsed = 0;
            var success = true;
            string? errorMessage = null;

            foreach (var chunk in chunks)
            {
                var result = await _translationProvider.TranslateAsync(chunk, SourceLanguage, language.Code);
                charactersUsed += result.CharactersUsed;

                if (!result.Success)
                {
                    success = false;
                    errorMessage = result.ErrorCode;
                    break;
                }
                translatedParts.Add(result.TranslatedText);
            }

            await _usageLogRepository.AddAsync(new UsageLog
            {
                Service = UsageService.Translator,
                Characters = charactersUsed,
                Chunks = chunks.Count,
                Success = success,
                ErrorCode = errorMessage,
                CreatedAt = DateTime.UtcNow
            });

            if (success)
            {
                var translatedText = string.Join(" ", translatedParts);
                var existing = place.Translations.FirstOrDefault(t => t.LanguageCode == language.Code);
                if (existing is null)
                {
                    place.Translations.Add(new PlaceTranslation
                    {
                        PlaceId = place.Id,
                        LanguageCode = language.Code,
                        Name = source.Name,
                        NarrationText = translatedText,
                        Status = TranslationStatus.Draft,
                        Source = TranslationSource.Machine
                    });
                }
                else
                {
                    existing.NarrationText = translatedText;
                    existing.Status = TranslationStatus.Draft;
                    existing.Source = TranslationSource.Machine;
                }
            }

            results.Add(new LanguageTranslationResultDto(language.Code, success, charactersUsed, errorMessage));
        }

        await _placeRepository.SaveChangesAsync();
        return new TranslatePlaceResultDto(results);
    }

    public async Task<bool> ApproveTranslationAsync(int placeId, string languageCode)
    {
        var place = await _placeRepository.GetByIdAsync(placeId);
        if (place is null) return false;

        var translation = place.Translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (translation is null) return false;

        translation.Status = TranslationStatus.Reviewed;
        await _placeRepository.SaveChangesAsync();
        return true;
    }

    private static List<string> ChunkText(string text, int maxChunkLength = MaxChunkLength)
    {
        var sentences = Regex.Split(text.Trim(), @"(?<=[.!?])\s+");
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (current.Length + sentence.Length + 1 > maxChunkLength && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }
            current.Append(sentence).Append(' ');
        }
        if (current.Length > 0) chunks.Add(current.ToString().Trim());

        return chunks.Count > 0 ? chunks : new List<string> { text };
    }
}