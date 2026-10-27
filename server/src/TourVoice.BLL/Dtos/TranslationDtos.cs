namespace TourVoice.BLL.Dtos;

/// <param name="Skipped">BR-05: ban dich da khop SourceHash nen khong goi MyMemory.</param>
public record LanguageTranslationResultDto(
    string LanguageCode, bool Success, int CharactersUsed, string? ErrorMessage, bool Skipped = false);

public record TranslatePlaceResultDto(List<LanguageTranslationResultDto> Results);

public record UpdateTranslationRequestDto(string Name, string? NarrationText);

/// <param name="Status">None (chua co noi dung), Draft hoac Reviewed.</param>
public record MatrixCellDto(string Status, bool Stale);

public record MatrixRowDto(
    int PlaceId, string Slug, string Name, string Kind, string Status, string? ParentName,
    Dictionary<string, MatrixCellDto> Cells, bool CanTranslate);

public record NarrationMatrixDto(List<LanguageDto> Languages, List<MatrixRowDto> Rows);
