using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface INarrationPipelineService
{
    /// <summary>"Dich phan thieu" (BR-04..06): chi dich ngon ngu con thieu hoac da cu; ket qua vao Draft.</summary>
    /// <exception cref="Exceptions.QuotaExceededException">BR-06 (429).</exception>
    /// <exception cref="Exceptions.ExternalServiceException">MyMemory loi (502); ngon ngu dich xong truoc do van duoc luu.</exception>
    Task<TranslatePlaceResultDto> TranslatePlaceAsync(int placeId, CancellationToken ct = default);

    /// <summary>Draft -> Reviewed. False neu khong co Place/ban dich.</summary>
    Task<bool> ApproveTranslationAsync(int placeId, string languageCode);

    /// <summary>Sua ten/bai (BR-07: sua ban da duyet thi ve Draft). Null neu khong co Place.</summary>
    Task<AdminTranslationDto?> UpdateTranslationAsync(int placeId, string languageCode, UpdateTranslationRequestDto request);

    /// <param name="filter">all | draft | stale | missing</param>
    Task<NarrationMatrixDto> GetMatrixAsync(string? filter);
}
