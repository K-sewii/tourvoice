using System.Text;
using System.Text.RegularExpressions;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Translation;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public partial class NarrationPipelineService : INarrationPipelineService
{
    public const int MaxChunkBytes = 500;   // gioi han q cua MyMemory (UTF-8)
    private const string SourceLanguage = TranslationRules.SourceLanguage;

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

        // BR-04: chi dich tu ban vi da duyet.
        var source = place.Translations.FirstOrDefault(t => t.LanguageCode == SourceLanguage);
        if (!TranslationRules.HasText(source) || source!.Status != TranslationStatus.Reviewed)
            throw new MissingSourceNarrationException();

        var sourceText = source.NarrationText!;
        var hash = TranslationRules.HashOf(sourceText);
        var targets = (await _languageRepository.GetEnabledAsync()).Where(l => l.Code != SourceLanguage).ToList();

        var results = new List<LanguageTranslationResultDto>();
        var pending = new List<Language>();
        foreach (var language in targets)
        {
            var existing = place.Translations.FirstOrDefault(t => t.LanguageCode == language.Code);
            // BR-05: thieu hoac ban goc da doi thi dich; con lai bo qua, khong ton han muc.
            if (NeedsTranslation(existing, hash)) pending.Add(language);
            else results.Add(new LanguageTranslationResultDto(language.Code, true, 0, null, Skipped: true));
        }
        if (pending.Count == 0) return new TranslatePlaceResultDto(results);

        // BR-06: cong phan sap gui vao so da dung trong ngay.
        var usedToday = await _usageLogRepository.GetTodayCharacterTotalAsync(UsageService.MyMemory);
        if (usedToday + sourceText.Length * pending.Count > QuotaPolicy.BlockThreshold)
            throw new QuotaExceededException();

        var chunks = ChunkText(sourceText);
        DomainException? failure = null;

        foreach (var language in pending)
        {
            var parts = new List<string>();
            var matches = new List<decimal>();
            var characters = 0;
            var sentChunks = 0;
            string? errorCode = null;

            foreach (var chunk in chunks)
            {
                var result = await _translationProvider.TranslateAsync(chunk, SourceLanguage, language.MtCode);
                characters += result.CharactersUsed;
                sentChunks++;
                if (!result.Success)
                {
                    errorCode = result.ErrorCode ?? "unknown";
                    break;
                }
                parts.Add(result.TranslatedText);
                if (result.Match is { } m) matches.Add(m);
            }

            _usageLogRepository.Add(new UsageLog
            {
                Service = UsageService.MyMemory,
                Characters = characters,
                Chunks = sentChunks,
                Success = errorCode is null,
                ErrorCode = errorCode,
                CreatedAt = DateTime.UtcNow,
            });

            if (errorCode is not null)
            {
                results.Add(new LanguageTranslationResultDto(language.Code, false, characters, errorCode));
                failure = errorCode == TranslationChunkResult.QuotaExceededCode
                    ? new QuotaExceededException("MyMemory báo đã hết hạn mức hôm nay.")
                    : new ExternalServiceException("MyMemory", errorCode);
                break;   // dung ngay de khong dot them han muc; ngon ngu da xong van duoc luu ben duoi
            }

            Apply(place, source, language.Code, Clip(string.Join(" ", parts)), hash,
                matches.Count > 0 ? Math.Round(matches.Average(), 4) : null);
            results.Add(new LanguageTranslationResultDto(language.Code, true, characters, null));
        }

        // Mot SaveChanges = mot transaction: cac ban dich va moi dong UsageLogs cung vao hoac cung khong.
        await _placeRepository.SaveChangesAsync();

        if (failure is not null) throw failure;
        return new TranslatePlaceResultDto(results);
    }

    public async Task<bool> ApproveTranslationAsync(int placeId, string languageCode)
    {
        var place = await _placeRepository.GetByIdAsync(placeId);
        if (place is null) return false;

        var translation = place.Translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (translation is null) return false;
        if (!TranslationRules.HasText(translation))
            throw new BusinessRuleException("empty_narration", "Chưa có nội dung để duyệt.");

        translation.Status = TranslationStatus.Reviewed;
        await _placeRepository.SaveChangesAsync();
        return true;
    }

    public async Task<AdminTranslationDto?> UpdateTranslationAsync(
        int placeId, string languageCode, UpdateTranslationRequestDto request)
    {
        var place = await _placeRepository.GetByIdAsync(placeId);
        if (place is null) return null;
        _ = await _languageRepository.GetByCodeAsync(languageCode) ?? throw new LanguageNotFoundException();

        var name = (request.Name ?? "").Trim();
        if (name.Length is 0 or > 200)
            throw new ValidationException("invalid_name", "Tên bắt buộc, tối đa 200 ký tự.");
        var text = string.IsNullOrWhiteSpace(request.NarrationText) ? null : request.NarrationText.Trim();
        if (text is { Length: > TranslationRules.NarrationMaxLength })
            throw new ValidationException("narration_too_long", $"Bài thuyết minh tối đa {TranslationRules.NarrationMaxLength} ký tự.");

        var translation = place.Translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (translation is null)
        {
            translation = new PlaceTranslation { PlaceId = place.Id, LanguageCode = languageCode, Name = name, NarrationText = text };
            place.Translations.Add(translation);
        }
        else
        {
            var changed = translation.Name != name || translation.NarrationText != text;
            if (translation.NarrationText != text)
            {
                translation.Source = TranslationSource.Manual;
                translation.MachineMatch = null;
            }
            translation.Name = name;
            translation.NarrationText = text;
            // BR-07: sua ban da duyet thi ve Draft va phai duyet lai.
            if (changed) translation.Status = TranslationStatus.Draft;
        }

        await _placeRepository.SaveChangesAsync();
        var vi = place.Translations.FirstOrDefault(t => t.LanguageCode == SourceLanguage);
        return AdminPlaceService.ToTranslationDto(translation, vi);
    }

    public async Task<NarrationMatrixDto> GetMatrixAsync(string? filter)
    {
        filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant();
        if (filter is not ("all" or "draft" or "stale" or "missing"))
            throw new ValidationException("invalid_filter", "Bộ lọc phải là all, draft, stale hoặc missing.");

        var languages = await _languageRepository.GetEnabledAsync();
        var places = await _placeRepository.GetAllWithTranslationsAsync();
        var names = places.ToDictionary(p => p.Id, p => NameOf(p));
        var rows = new List<MatrixRowDto>();

        foreach (var p in places)
        {
            var vi = p.Translations.FirstOrDefault(t => t.LanguageCode == SourceLanguage);
            var cells = new Dictionary<string, MatrixCellDto>();
            var hash = TranslationRules.HasText(vi) ? TranslationRules.HashOf(vi!.NarrationText!) : null;
            var needsTranslation = false;

            foreach (var l in languages)
            {
                var t = p.Translations.FirstOrDefault(x => x.LanguageCode == l.Code);
                var has = TranslationRules.HasText(t);
                cells[l.Code] = new MatrixCellDto(
                    !has ? "None" : t!.Status.ToString(), has && TranslationRules.IsStale(t!, vi));
                if (l.Code != SourceLanguage && hash is not null && NeedsTranslation(t, hash)) needsTranslation = true;
            }

            var keep = filter switch
            {
                "draft" => cells.Values.Any(c => c.Status == "Draft"),
                "stale" => cells.Values.Any(c => c.Stale),
                "missing" => languages.Any(l => cells[l.Code].Status == "None"),
                _ => true,
            };
            if (!keep) continue;

            rows.Add(new MatrixRowDto(
                p.Id, p.Slug, NameOf(p), p.Kind.ToString(), p.Status.ToString(),
                p.ParentId is { } pid && names.TryGetValue(pid, out var parentName) ? parentName : null,
                cells,
                CanTranslate: vi is { Status: TranslationStatus.Reviewed } && needsTranslation));
        }

        return new NarrationMatrixDto(
            languages.Select(l => new LanguageDto(l.Code, l.MtCode, l.SpeechLang, l.IsEnabled)).ToList(), rows);
    }

    // ---- helpers ----

    private static string NameOf(Place p) =>
        p.Translations.FirstOrDefault(t => t.LanguageCode == SourceLanguage)?.Name ?? p.Slug;

    private static bool NeedsTranslation(PlaceTranslation? existing, string sourceHash) =>
        !TranslationRules.HasText(existing)
        || existing!.SourceHash != sourceHash && (existing.SourceHash is not null || existing.Source == TranslationSource.Machine);

    private static void Apply(Place place, PlaceTranslation source, string code, string text, string hash, decimal? match)
    {
        var existing = place.Translations.FirstOrDefault(t => t.LanguageCode == code);
        if (existing is null)
        {
            existing = new PlaceTranslation { PlaceId = place.Id, LanguageCode = code, Name = source.Name };
            place.Translations.Add(existing);
        }
        existing.NarrationText = text;
        existing.Status = TranslationStatus.Draft;
        existing.Source = TranslationSource.Machine;
        existing.SourceHash = hash;
        existing.MachineMatch = match;
    }

    /// <summary>Ban dich co the dai hon ban goc; cat o ranh gioi cau de khong vuot gioi han 4.000 ky tu (BR-04).</summary>
    private static string Clip(string text)
    {
        if (text.Length <= TranslationRules.NarrationMaxLength) return text;
        var cut = text[..TranslationRules.NarrationMaxLength];
        var end = cut.LastIndexOfAny(['.', '!', '?', '。', '！', '？']);
        return end > 0 ? cut[..(end + 1)] : cut;
    }

    /// <summary>Cat theo cau thanh cac doan &lt;= <paramref name="maxBytes"/> byte UTF-8; cau qua dai thi cat theo tu, cuoi cung theo ky tu.</summary>
    internal static List<string> ChunkText(string text, int maxBytes = MaxChunkBytes)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0) chunks.Add(current.ToString());
            current.Clear();
        }

        foreach (var sentence in SentenceBreak().Split(text.Trim()).Where(s => s.Length > 0))
        foreach (var piece in SplitLong(sentence, maxBytes))
        {
            var joined = current.Length == 0 ? piece : current + " " + piece;
            if (Encoding.UTF8.GetByteCount(joined) > maxBytes) Flush();
            if (current.Length > 0) current.Append(' ');
            current.Append(piece);
        }
        Flush();

        return chunks.Count > 0 ? chunks : [text];
    }

    private static IEnumerable<string> SplitLong(string sentence, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(sentence) <= maxBytes)
        {
            yield return sentence;
            yield break;
        }

        var line = new StringBuilder();
        foreach (var word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var part in HardSplit(word, maxBytes))
            {
                var joined = line.Length == 0 ? part : line + " " + part;
                if (Encoding.UTF8.GetByteCount(joined) > maxBytes && line.Length > 0)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(part);
            }
        }
        if (line.Length > 0) yield return line.ToString();
    }

    /// <summary>Tu don le dai hon gioi han (vd. van ban CJK khong co dau cach): cat theo ky tu, khong tach cap thay the.</summary>
    private static IEnumerable<string> HardSplit(string word, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(word) <= maxBytes)
        {
            yield return word;
            yield break;
        }

        var piece = new StringBuilder();
        var bytes = 0;
        foreach (var rune in word.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (bytes + size > maxBytes)
            {
                yield return piece.ToString();
                piece.Clear();
                bytes = 0;
            }
            piece.Append(rune.ToString());
            bytes += size;
        }
        if (piece.Length > 0) yield return piece.ToString();
    }

    [GeneratedRegex(@"(?<=[.!?…。！？])\s+")]
    private static partial Regex SentenceBreak();
}
