using System.Linq;
using Content.Server.Medical.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Body.Systems;

namespace Content.Server.Medical;

public sealed class HealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    // open-space edit start
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    // open-space edit end

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<HealthAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
        // open-space edit start
        SubscribeLocalEvent<HealthAnalyzerComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerClearMessage>(OnClearMessage);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerPrintMessage>(OnPrintMessage);
        // open-space edit end
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not {} patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between health analyzer and the scanned entity
            //null is infinite range
            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                //Range too far, disable updates until they are back in range
                PauseAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.IsAnalyzerActive = true;
            // open-space edit start
            PushCurrentState((uid, component), patient, true);
            // open-space edit end
        }
    }

    // open-space edit start
    private void OnBoundUiOpened(Entity<HealthAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        SendUiState(ent, ent.Comp.LastState);
    }

    private void OnClearMessage(Entity<HealthAnalyzerComponent> ent, ref HealthAnalyzerClearMessage args)
    {
        ent.Comp.ScannedEntity = null;
        ent.Comp.IsAnalyzerActive = false;
        ent.Comp.LastState = new HealthAnalyzerUiState();
        _toggle.TryDeactivate(ent.Owner);
        SendUiState(ent, ent.Comp.LastState);
    }

    private void OnPrintMessage(Entity<HealthAnalyzerComponent> ent, ref HealthAnalyzerPrintMessage args)
    {
        if (!ent.Comp.LastState.HasData)
            return;

        PrintReport(ent.Comp.LastState, args.Actor);
    }
    // open-space edit end

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HealthAnalyzerComponent> uid, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<HealthAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<HealthAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    private void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Link the health analyzer to the scanned entity
        healthAnalyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(healthAnalyzer.Owner);

        // open-space edit start
        PushCurrentState(healthAnalyzer, target, true);
        // open-space edit end
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;
        healthAnalyzer.Comp.IsAnalyzerActive = false;

        _toggle.TryDeactivate(healthAnalyzer.Owner);

        // open-space edit start
        PushStoredState(healthAnalyzer, false);
        // open-space edit end
    }


    /// <summary>
    /// If the scanner is active, sends one last update and sets it to inactive.
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void PauseAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        if (!healthAnalyzer.Comp.IsAnalyzerActive)
            return;

        // open-space edit start
        PushStoredState(healthAnalyzer, false);
        // open-space edit end
        healthAnalyzer.Comp.IsAnalyzerActive = false;
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    public void UpdateScannedUser(EntityUid healthAnalyzer, EntityUid target, bool scanMode)
    {
        // open-space edit start
        if (!TryComp<HealthAnalyzerComponent>(healthAnalyzer, out var component))
            return;

        PushCurrentState((healthAnalyzer, component), target, scanMode);
        // open-space edit end
    }

    /// <summary>
    /// Creates a HealthAnalyzerState based on the current state of an entity.
    /// </summary>
    /// <param name="target">The entity being scanned</param>
    /// <returns></returns>
    public HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target)
    {
        if (!target.HasValue || !HasComp<DamageableComponent>(target))
            return new HealthAnalyzerUiState();

        var entity = target.Value;
        var damageable = Comp<DamageableComponent>(entity);
        var bodyTemperature = float.NaN;

        if (TryComp<TemperatureComponent>(entity, out var temp))
            bodyTemperature = temp.CurrentTemperature;

        var bloodAmount = float.NaN;
        var bloodVolume = FixedPoint2.Zero;
        var bleeding = false;
        var unrevivable = false;
        // open-space edit start
        var reagents = new List<HealthAnalyzerReagentEntry>();
        // open-space edit end

        if (TryComp<BloodstreamComponent>(entity, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(entity, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
        {
            bloodAmount = _bloodstreamSystem.GetBloodLevel(entity);
            bloodVolume = bloodSolution.Volume;
            bleeding = bloodstream.BleedAmount > 0;
            // open-space edit start
            var bloodReference = bloodstream.BloodReferenceSolution;
            foreach (var (reagentId, quantity) in bloodSolution.Contents)
            {
                if (bloodReference.ContainsPrototype(reagentId.Prototype))
                    continue;

                reagents.Add(new HealthAnalyzerReagentEntry(reagentId.Prototype, quantity));
            }
            // open-space edit end
        }

        if (TryComp<UnrevivableComponent>(entity, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        // open-space edit start
        var totalDamage = _damageableSystem.GetTotalDamage((entity, damageable));
        var damageGroups = _damageableSystem.GetDamagePerGroup((entity, damageable));
        damageGroups.TryGetValue("Airloss", out var airlossDamage);
        damageGroups.TryGetValue("Toxin", out var toxinDamage);
        damageGroups.TryGetValue("Burn", out var burnDamage);
        damageGroups.TryGetValue("Brute", out var bruteDamage);

        var condition = 100f;
        if (_mobThreshold.TryGetIncapPercentage(entity, totalDamage, out var incapPercentage) &&
            incapPercentage.HasValue)
        {
            condition = Math.Clamp(100f - incapPercentage.Value.Float() * 100f, 0f, 100f);
        }

        var genesStable = TryComp<DnaComponent>(entity, out var dna) && dna.DNA != null;

        return new HealthAnalyzerUiState(
            true,
            GetNetEntity(entity),
            Name(entity),
            string.Empty,
            TryComp<MobStateComponent>(entity, out var mobState) ? mobState.CurrentState : null,
            condition,
            bodyTemperature,
            bloodAmount,
            bloodVolume,
            totalDamage,
            airlossDamage,
            toxinDamage,
            burnDamage,
            bruteDamage,
            null,
            genesStable,
            reagents,
            new List<string>(),
            null,
            bleeding,
            unrevivable
        );
        // open-space edit end
    }

    // open-space edit start
    private void PushCurrentState(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target, bool scanMode)
    {
        if (!HasComp<DamageableComponent>(target))
            return;

        var uiState = GetHealthAnalyzerUiState(target);
        uiState.ScanMode = scanMode;
        healthAnalyzer.Comp.LastState = uiState;
        SendUiState(healthAnalyzer, uiState);
    }

    private void PushStoredState(Entity<HealthAnalyzerComponent> healthAnalyzer, bool scanMode)
    {
        if (!healthAnalyzer.Comp.LastState.HasData)
        {
            SendUiState(healthAnalyzer, healthAnalyzer.Comp.LastState);
            return;
        }

        var uiState = healthAnalyzer.Comp.LastState;
        uiState.ScanMode = scanMode;
        healthAnalyzer.Comp.LastState = uiState;
        SendUiState(healthAnalyzer, uiState);
    }

    private void SendUiState(Entity<HealthAnalyzerComponent> healthAnalyzer, HealthAnalyzerUiState state)
    {
        if (!_uiSystem.HasUi(healthAnalyzer.Owner, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.ServerSendUiMessage(
            healthAnalyzer.Owner,
            HealthAnalyzerUiKey.Key,
            new HealthAnalyzerScannedUserMessage(state));
    }

    private void PrintReport(HealthAnalyzerUiState state, EntityUid user)
    {
        var printed = Spawn("Paper", Transform(user).Coordinates);
        _hands.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
            return;

        _metaData.SetEntityName(printed,
            Loc.GetString("health-analyzer-report-paper-name", ("patient", state.PatientName)));
        _paper.SetContent((printed, paperComp), BuildPrintedReport(state));
    }

    private string BuildPrintedReport(HealthAnalyzerUiState state)
    {
        var bodyTemperatureC = float.IsNaN(state.Temperature) ? float.NaN : state.Temperature - 273.15f;
        var bodyTemperatureF = float.IsNaN(bodyTemperatureC) ? float.NaN : bodyTemperatureC * 9f / 5f + 32f;

        var reagents = state.Reagents.Count > 0
            ? string.Join("\n", state.Reagents.Select(reagent =>
                Loc.GetString(
                    "health-analyzer-report-reagent-entry",
                    ("quantity", reagent.Quantity),
                    ("reagent", GetReagentName(reagent.ReagentPrototype)))))
            : Loc.GetString("health-analyzer-report-reagents-none");

        var dependencies = state.Dependencies.Count > 0
            ? string.Join("\n", state.Dependencies)
            : Loc.GetString("health-analyzer-report-dependencies-none");

        return
            $"{Loc.GetString("health-analyzer-report-title", ("patient", state.PatientName))}\n\n" +
            $"{Loc.GetString("health-analyzer-report-time", ("time", _timing.CurTime.ToString(@"hh\:mm\:ss")))}\n\n" +
            $"{Loc.GetString("health-analyzer-report-condition", ("condition", $"{state.Condition:F0}"))}\n" +
            $"{Loc.GetString("health-analyzer-report-damage-types", ("airloss", state.AirlossDamage), ("toxin", state.ToxinDamage), ("burn", state.BurnDamage), ("brute", state.BruteDamage))}\n" +
            $"{Loc.GetString("health-analyzer-report-status", ("status", state.MobState.HasValue ? GetStatusText(state.MobState.Value) : Loc.GetString("health-analyzer-window-entity-unknown-text")))}\n" +
            $"{Loc.GetString("health-analyzer-report-temperature", ("tempC", float.IsNaN(bodyTemperatureC) ? Loc.GetString("health-analyzer-window-entity-unknown-value-text") : $"{bodyTemperatureC:F2}"), ("tempF", float.IsNaN(bodyTemperatureF) ? Loc.GetString("health-analyzer-window-entity-unknown-value-text") : $"{bodyTemperatureF:F2}"))}\n" +
            $"{Loc.GetString("health-analyzer-report-blood", ("blood", float.IsNaN(state.BloodLevel) ? Loc.GetString("health-analyzer-window-entity-unknown-value-text") : $"{state.BloodLevel * 100f:F1}%"))}\n" +
            $"{Loc.GetString("health-analyzer-report-genes", ("genes", state.GenesStable == true ? Loc.GetString("health-analyzer-window-genes-stable") : Loc.GetString("health-analyzer-window-entity-unknown-value-text")))}\n\n" +
            $"{Loc.GetString("health-analyzer-report-reagents")}\n{reagents}\n\n" +
            $"{Loc.GetString("health-analyzer-report-dependencies")}\n{dependencies}\n\n" +
            $"{Loc.GetString("health-analyzer-report-notes")}";
    }

    private string GetStatusText(MobState mobState)
    {
        return mobState switch
        {
            MobState.Alive => Loc.GetString("health-analyzer-window-entity-alive-text"),
            MobState.Critical => Loc.GetString("health-analyzer-window-entity-critical-text"),
            MobState.Dead => Loc.GetString("health-analyzer-window-entity-dead-text"),
            _ => Loc.GetString("health-analyzer-window-entity-unknown-text"),
        };
    }

    private string GetReagentName(string reagentPrototype)
    {
        return _prototypeManager.TryIndex<ReagentPrototype>(reagentPrototype, out var reagent)
            ? reagent.LocalizedName
            : reagentPrototype;
    }
    // open-space edit end
}
