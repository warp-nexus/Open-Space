using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class MothAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerBuzz = new Regex("з{1,3}"); // OpenSpace-Edit
    private static readonly Regex RegexUpperBuzz = new Regex("З{1,3}"); // OpenSpace-Edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // buzzz
        message = RegexLowerBuzz.Replace(message, "ззз"); // OpenSpace-Edit
        // buZZZ
        message = RegexUpperBuzz.Replace(message, "ЗЗЗ"); // OpenSpace-Edit

        args.Message = message;
    }
}
