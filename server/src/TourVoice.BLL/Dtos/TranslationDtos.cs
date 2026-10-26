namespace TourVoice.BLL.Dtos;

public record LanguageTranslationResultDto(string LanguageCode, bool Success, int CharactersUsed, string? ErrorMessage);

public record TranslatePlaceResultDto(List<LanguageTranslationResultDto> Results);