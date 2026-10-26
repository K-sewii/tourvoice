using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface INarrationPipelineService
{
    Task<TranslatePlaceResultDto> TranslatePlaceAsync(int placeId);
    Task<bool> ApproveTranslationAsync(int placeId, string languageCode);
}

public class QuotaExceededException : Exception
{
    public QuotaExceededException() : base("Da dat gioi han 90% han muc MyMemory hom nay.") { }
}

public class PlaceNotFoundException : Exception
{
    public PlaceNotFoundException() : base("Khong tim thay Place.") { }
}

public class MissingSourceNarrationException : Exception
{
    public MissingSourceNarrationException() : base("Chua co noi dung tieng Viet de dich.") { }
}