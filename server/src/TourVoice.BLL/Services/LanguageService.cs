using System.Text.RegularExpressions;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public partial class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _repository;
    public LanguageService(ILanguageRepository repository) => _repository = repository;

    public async Task<List<LanguageDto>> GetLanguagesAsync() =>
        (await _repository.GetEnabledAsync()).Select(ToDto).ToList();

    public async Task<List<LanguageDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<LanguageDto?> UpdateAsync(string code, UpdateLanguageRequestDto request)
    {
        var language = await _repository.GetByCodeAsync(code);
        if (language is null) return null;

        var mtCode = (request.MtCode ?? "").Trim();
        var speechLang = (request.SpeechLang ?? "").Trim();
        if (!MtCodePattern().IsMatch(mtCode))
            throw new ValidationException("invalid_mt_code", "Mã MyMemory không hợp lệ (vd. en, zh-CN).");
        if (!SpeechLangPattern().IsMatch(speechLang))
            throw new ValidationException("invalid_speech_lang", "Mã giọng đọc phải theo BCP-47 (vd. vi-VN).");
        if (!request.IsEnabled && code == "vi")
            throw new BusinessRuleException("source_language_required", "Không thể tắt tiếng Việt vì là ngôn ngữ nguồn.");

        language.MtCode = mtCode;
        language.SpeechLang = speechLang;
        language.IsEnabled = request.IsEnabled;
        await _repository.SaveChangesAsync();
        return ToDto(language);
    }

    private static LanguageDto ToDto(Language l) => new(l.Code, l.MtCode, l.SpeechLang, l.IsEnabled);

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z]{2,4})?$")]
    private static partial Regex MtCodePattern();

    [GeneratedRegex("^[a-z]{2,3}-[A-Za-z0-9]{2,6}$")]
    private static partial Regex SpeechLangPattern();
}
