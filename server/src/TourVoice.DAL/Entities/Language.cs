namespace TourVoice.DAL.Entities;

public class Language
{
    public string Code { get; set; } = null!;             // vi, en, zh, ko, ja
    public string MtCode { get; set; } = null!;           // ma MyMemory: vi, en, zh-CN, ko, ja
    public string SpeechLang { get; set; } = null!;       // ma giong BCP-47 cho Web Speech: vi-VN, en-US...
    public bool IsEnabled { get; set; } = true;
}
