using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// System that gives the speaker a faux-French accent.
/// </summary>
public sealed class FrenchAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexKR = new(@"[кКрР]", RegexOptions.IgnoreCase); // OpenSpace-Edit // Теперь картавые
    private static readonly Regex RegexStartH = new(@"(?<!\w)х", RegexOptions.IgnoreCase); // OpenSpace-Edit
    private static readonly Regex RegexSpacePunctuation = new(@"(?<=\w\w)[!?;:](?!\w)", RegexOptions.IgnoreCase);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FrenchAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, FrenchAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "french");

        // replaces х with ' at the start of words.
        msg = RegexStartH.Replace(msg, "'");

        // spaces out ! ? : and ;.
        msg = RegexSpacePunctuation.Replace(msg, " $&");

        // OpenSpace-Edit Start
        // replaces 'к/К' with 'кх/КХ' and 'р/Р' with 'х/Х' globally (preserves case) in a single pass.
        msg = RegexKR.Replace(msg, static m => m.Value[0] switch
        {
            'К' => "КХ",
            'к' => "кх",
            'Р' => "Х",
            'р' => "х",
            _ => m.Value
        });
        // OpenSpace-Edit End

        return msg;
    }

    private void OnAccentGet(EntityUid uid, FrenchAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
