using Content.Shared.Humanoid;

namespace Content.Shared._Art.TTS;

public sealed class TTSConfig
{
    public const string DefaultVoice = "xrenoid";
    public static readonly Dictionary<Sex, string> DefaultSexVoice = new()
    {
        {Sex.Male, "xrenoid"},
        {Sex.Female, "lina_dota_2"},
        {Sex.Unsexed, "gman"}
    };
    public const int VoiceRange = 10; // how far voice goes in world units
    public const int WhisperClearRange = 2; // how far whisper goes while still being understandable, in world units
    public const int WhisperMuffledRange = 5; // how far whisper goes at all, in world units
}
