namespace TourVoice.DAL.Entities;

public class Language
{
    public string Code { get; set; } = null!;             // vi, en, zh, ko, ja
    public string TranslatorCode { get; set; } = null!;    // ma dung cho Azure Translator
    public string VoiceName { get; set; } = null!;         // vd: vi-VN-HoaiMyNeural
    public bool IsEnabled { get; set; } = true;
}