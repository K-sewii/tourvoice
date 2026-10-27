using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface ILanguageService
{
    /// <summary>Ngon ngu dang bat (cong khai).</summary>
    Task<List<LanguageDto>> GetLanguagesAsync();

    /// <summary>Ca ngon ngu dang tat (admin, SCR-A6).</summary>
    Task<List<LanguageDto>> GetAllAsync();

    /// <summary>Null neu khong co ngon ngu. Khong tat duoc tieng Viet (ngon ngu nguon).</summary>
    Task<LanguageDto?> UpdateAsync(string code, UpdateLanguageRequestDto request);
}
