using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class LizardAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerS = new("с+");
    private static readonly Regex RegexUpperS = new("С+");
    // private static readonly Regex RegexInternalX = new(@"(\w)x");
    // private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    // private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LizardAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, LizardAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // hissss
        message = RegexLowerS.Replace(message, "ссс"); // OpenSpace-Edit
        // hiSSS
        message = RegexUpperS.Replace(message, "ССС"); // OpenSpace-Edit
        // ekssit
        // message = RegexInternalX.Replace(message, "$1ксс"); // OpenSpace-Edit
        // // ecks
        // message = RegexLowerEndX.Replace(message, "екс$1"); // OpenSpace-Edit
        // // eckS
        // message = RegexUpperEndX.Replace(message, "ЕКС$1"); // OpenSpace-Edit

        args.Message = message;
    }
}
