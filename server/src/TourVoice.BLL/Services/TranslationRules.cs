using System.Security.Cryptography;
using System.Text;
using TourVoice.DAL.Entities;

namespace TourVoice.BLL.Services;

/// <summary>Quy tac ban dich dung chung (BR-04, BR-05, BR-07).</summary>
internal static class TranslationRules
{
    public const string SourceLanguage = "vi";
    public const int NarrationMaxLength = 4000;   // BR-04

    /// <summary>SourceHash = SHA-256 cua ban tieng Viet (BR-05).</summary>
    public static string HashOf(string text) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static bool HasText(PlaceTranslation? t) => !string.IsNullOrWhiteSpace(t?.NarrationText);

    /// <summary>
    /// "Ban goc da doi" (BR-05): ban dich co hash khac hash cua ban vi hien tai, hoac ban may cu chua co hash.
    /// Ban nhap tay chua tung dich may (khong co hash) khong bi coi la cu.
    /// </summary>
    public static bool IsStale(PlaceTranslation target, PlaceTranslation? vi)
    {
        if (target.LanguageCode == SourceLanguage || !HasText(target) || !HasText(vi)) return false;
        if (target.SourceHash is null) return target.Source == TranslationSource.Machine;
        return target.SourceHash != HashOf(vi!.NarrationText!);
    }
}
