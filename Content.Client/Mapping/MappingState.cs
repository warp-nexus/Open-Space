using System.Linq;
// open-space edit start
using System.Globalization;
// open-space edit end
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.ContextMenu.UI;
using Content.Client.Decals;
using Content.Client.Gameplay;
// open-space edit start
using Content.Client.Markers;
using Content.Client.Movement.Systems;
using Content.Client.SubFloor;
// open-space edit end
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.Verbs;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Content.Shared.Input;
using Content.Shared.Maps;
// open-space edit start
using Content.Shared.Shuttles.Components;
// open-space edit end
using Robust.Client.GameObjects;
// open-space edit start
using Robust.Client.Console;
// open-space edit end
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
// open-space edit start
using Robust.Client.Player;
// open-space edit end
using Robust.Client.ResourceManagement;
// open-space edit start
using Robust.Client.State;
// open-space edit end
using Robust.Client.UserInterface;
// open-space edit start
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables;
// open-space edit end
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
// open-space edit start
using Robust.Shared.Maths;
// open-space edit end
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static System.StringComparison;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using static Robust.Client.UserInterface.Controls.OptionButton;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;

namespace Content.Client.Mapping;

public sealed class MappingState : GameplayStateBase
{
    #if !FULL_RELEASE
    [Dependency] private readonly IClientAdminManager _admin = default!;
    #endif

    // open-space edit start
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IClientViewVariablesManager _viewVariables = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly MarkerSystem _marker = default!;
    // open-space edit end
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntityNetworkManager _entityNetwork = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MappingManager _mapping = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityMenuUIController _entityMenuController = default!;

    private DecalPlacementSystem _decal = default!;
    // open-space edit start
    private ContentEyeSystem _contentEye = default!;
    private SubFloorHideSystem _subfloor = default!;
    // open-space edit end
    private SpriteSystem _sprite = default!;
    private TransformSystem _transform = default!;
    private VerbSystem _verbs = default!;

    private readonly ISawmill _sawmill;
    private readonly GameplayStateLoadController _loadController;
    private bool _setup;
    private readonly List<MappingPrototype> _allPrototypes = new();
    private readonly Dictionary<IPrototype, MappingPrototype> _allPrototypesDict = new();
    private readonly Dictionary<Type, Dictionary<string, MappingPrototype>> _idDict = new();
    private readonly List<MappingPrototype> _prototypes = new();
    // open-space edit start
    private readonly List<MappingPrototype> _paletteRoots = new();
    private MappingPrototype? _entitiesRoot;
    private MappingPrototype? _tilesRoot;
    private MappingPaletteMode _paletteMode = MappingPaletteMode.Tiles;
    private bool _paletteGrouped = true;
    private EntityUid? _inspectedEntity;
    private EntityUid? _selectedGrid;
    private TimeSpan _nextGridRefresh;
    private MappingPaletteFilter _paletteFilter = MappingPaletteFilter.None;
    // open-space edit end
    private (TimeSpan At, MappingSpawnButton Button)? _lastClicked;
    private Control? _scrollTo;
    private bool _updatePlacement;
    private bool _updateEraseDecal;

    private MappingScreen Screen => (MappingScreen) UserInterfaceManager.ActiveScreen!;
    private MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

    // open-space edit start
    private static readonly QuickPaletteEntry[] QuickPaletteEntries =
    {
        QuickPaletteEntry.Tile("Space"),
        QuickPaletteEntry.Delete("ActionMappingEraser"),
        QuickPaletteEntry.Tile("Plating"),
        QuickPaletteEntry.Tile("FloorSteel"),
        QuickPaletteEntry.Entity("Firelock"),
        QuickPaletteEntry.Entity("Grille"),
        QuickPaletteEntry.Entity("Window"),
        QuickPaletteEntry.Entity("ReinforcedWindow"),
        QuickPaletteEntry.Entity("WallReinforced"),
        QuickPaletteEntry.Entity("WallSolid"),
        QuickPaletteEntry.Entity("GasPipeStraight"),
        QuickPaletteEntry.Entity("GasPipeBend"),
        QuickPaletteEntry.Entity("GasPipeTJunction"),
        QuickPaletteEntry.Entity("GasPipeFourway"),
        QuickPaletteEntry.Entity("GasVentScrubber"),
        QuickPaletteEntry.Entity("GasVentPump"),
        QuickPaletteEntry.Entity("AirAlarm"),
        QuickPaletteEntry.Entity("FireAlarm"),
        QuickPaletteEntry.Entity("APCBasic"),
        QuickPaletteEntry.Entity("CableApcExtension"),
        QuickPaletteEntry.Entity("CableMV"),
        QuickPaletteEntry.Entity("CableHV"),
        QuickPaletteEntry.Entity("SubstationBasic"),
        QuickPaletteEntry.Entity("Poweredlight"),
        QuickPaletteEntry.Entity("PoweredSmallLight"),
        QuickPaletteEntry.Entity("EmergencyLight"),
        QuickPaletteEntry.Entity("SMESBasic"),
        QuickPaletteEntry.Entity("TableWood"),
        QuickPaletteEntry.Entity("Table"),
        QuickPaletteEntry.Entity("TableCounterWood"),
        QuickPaletteEntry.Entity("TableCounterMetal"),
        QuickPaletteEntry.Entity("ChairWood"),
        QuickPaletteEntry.Entity("Chair"),
        QuickPaletteEntry.Entity("ChairOfficeLight"),
        QuickPaletteEntry.Entity("ChairOfficeDark"),
        QuickPaletteEntry.Entity("Stool"),
        QuickPaletteEntry.Entity("StoolBar"),
        QuickPaletteEntry.Entity("Rack"),
        QuickPaletteEntry.Entity("LampGold"),
        QuickPaletteEntry.Entity("DisposalPipe"),
        QuickPaletteEntry.Entity("DisposalBend"),
        QuickPaletteEntry.Entity("DisposalJunction"),
        QuickPaletteEntry.Entity("DisposalJunctionFlipped"),
        QuickPaletteEntry.Entity("DisposalRouter"),
        QuickPaletteEntry.Entity("DisposalRouterFlipped"),
        QuickPaletteEntry.Entity("DisposalUnit"),
        QuickPaletteEntry.Entity("DisposalTrunk"),
        QuickPaletteEntry.Entity("SignDisposalSpace"),
        QuickPaletteEntry.Entity("Windoor"),
        QuickPaletteEntry.Entity("WindowDirectional"),
        QuickPaletteEntry.Entity("WindowReinforcedDirectional"),
        QuickPaletteEntry.Entity("PlasmaWindowDirectional"),
        QuickPaletteEntry.Entity("Railing"),
        QuickPaletteEntry.Entity("RailingCorner"),
        QuickPaletteEntry.Entity("RailingCornerSmall"),
        QuickPaletteEntry.Entity("RailingRound"),
        QuickPaletteEntry.Entity("AirlockMaintLocked"),
        QuickPaletteEntry.Entity("AirlockGlass"),
        QuickPaletteEntry.Entity("AirlockServiceLocked"),
        QuickPaletteEntry.Entity("AirlockSecurityLocked"),
        QuickPaletteEntry.Entity("AirlockCommand"),
        QuickPaletteEntry.Entity("AirlockScience"),
        QuickPaletteEntry.Entity("AirlockMedical"),
        QuickPaletteEntry.Entity("AirlockEngineering"),
        QuickPaletteEntry.Entity("AirlockCargo"),
    };
    // open-space edit end

    public CursorState State { get; set; }

    public MappingState()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("mapping");
        _loadController = UserInterfaceManager.GetUIController<GameplayStateLoadController>();
    }

    protected override void Startup()
    {
        EnsureSetup();
        base.Startup();

        UserInterfaceManager.LoadScreen<MappingScreen>();
        _loadController.LoadScreen();

        var context = _input.Contexts.GetContext("common");
        context.AddFunction(ContentKeyFunctions.MappingUnselect);
        context.AddFunction(ContentKeyFunctions.SaveMap);
        context.AddFunction(ContentKeyFunctions.MappingEnablePick);
        context.AddFunction(ContentKeyFunctions.MappingEnableDelete);
        context.AddFunction(ContentKeyFunctions.MappingPick);
        context.AddFunction(ContentKeyFunctions.MappingRemoveDecal);
        context.AddFunction(ContentKeyFunctions.MappingCancelEraseDecal);
        context.AddFunction(ContentKeyFunctions.MappingOpenContextMenu);

        Screen.DecalSystem = _decal;
        Screen.Prototypes.SearchBar.OnTextChanged += OnSearch;
        Screen.Prototypes.CollapseAllButton.OnPressed += OnCollapseAll;
        Screen.Prototypes.ClearSearchButton.OnPressed += OnClearSearch;
        Screen.Prototypes.GetPrototypeData += OnGetData;
        Screen.Prototypes.SelectionChanged += OnSelected;
        Screen.Prototypes.CollapseToggled += OnCollapseToggled;
        Screen.Pick.OnPressed += OnPickPressed;
        Screen.Delete.OnPressed += OnDeletePressed;
        Screen.EntityReplaceButton.OnToggled += OnEntityReplacePressed;
        Screen.EntityPlacementMode.OnItemSelected += OnEntityPlacementSelected;
        Screen.EraseEntityButton.OnToggled += OnEraseEntityPressed;
        Screen.EraseDecalButton.OnToggled += OnEraseDecalPressed;
        // open-space edit start
        Screen.CcwButton.OnPressed += OnRotateCcwPressed;
        Screen.CwButton.OnPressed += OnRotateCwPressed;
        Screen.DeleteEntityButton.OnPressed += OnDeleteEntityPressed;
        Screen.DeselectButton.OnPressed += OnDeselectPressed;
        Screen.DeleteToolButton.OnPressed += OnDeletePressed;
        Screen.CableToolButton.OnPressed += OnCableToolPressed;
        Screen.PipeToolButton.OnPressed += OnPipeToolPressed;
        Screen.QuickFilterAllButton.OnPressed += OnQuickFilterAllPressed;
        Screen.QuickFilterCablesButton.OnPressed += OnQuickFilterCablesPressed;
        Screen.QuickFilterAirlocksButton.OnPressed += OnQuickFilterAirlocksPressed;
        Screen.QuickFilterPipesButton.OnPressed += OnQuickFilterPipesPressed;
        Screen.QuickFilterDisposalButton.OnPressed += OnQuickFilterDisposalPressed;
        Screen.ApplyPipeColorButton.OnPressed += OnApplyPipeColorPressed;
        Screen.OpenGridButton.OnPressed += OnOpenGridPressed;
        Screen.ApplyGridButton.OnPressed += OnApplyGridPressed;
        Screen.ExitMappingButton.OnPressed += OnExitMappingPressed;
        Screen.TilesPaletteButton.OnPressed += OnTilesPalettePressed;
        Screen.EntitiesPaletteButton.OnPressed += OnEntitiesPalettePressed;
        Screen.GroupPaletteButton.OnPressed += OnGroupPalettePressed;
        // open-space edit end
        _placement.PlacementChanged += OnPlacementChanged;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MappingUnselect, new PointerInputCmdHandler(HandleMappingUnselect, outsidePrediction: true))
            .Bind(ContentKeyFunctions.SaveMap, new PointerInputCmdHandler(HandleSaveMap, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingEnablePick, new PointerStateInputCmdHandler(HandleEnablePick, HandleDisablePick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingEnableDelete, new PointerStateInputCmdHandler(HandleEnableDelete, HandleDisableDelete, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingPick, new PointerInputCmdHandler(HandlePick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingRemoveDecal, new PointerInputCmdHandler(HandleEditorCancelPlace, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingCancelEraseDecal, new PointerInputCmdHandler(HandleCancelEraseDecal, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingOpenContextMenu, new PointerInputCmdHandler(HandleOpenContextMenu, outsidePrediction: true))
            .Register<MappingState>();

        _overlays.AddOverlay(new MappingOverlay(this));

        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;

        // open-space edit start
        PopulateQuickPalette();
        RefreshPalette();
        RefreshGridList(true);
        RefreshGridInfo();
        // open-space edit end
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<EntityPrototype>() &&
            !obj.WasModified<ContentTileDefinition>() &&
            !obj.WasModified<DecalPrototype>())
        {
            return;
        }

        ReloadPrototypes();
        // open-space edit start
        PopulateQuickPalette();
        RefreshPalette();
        // open-space edit end
    }

    private bool HandleOpenContextMenu(in PointerInputCmdArgs args)
    {
        Deselect();

        var coords = _transform.ToMapCoordinates(args.Coordinates);
        if (_verbs.TryGetEntityMenuEntities(coords, out var entities))
            _entityMenuController.OpenRootMenu(entities);

        return true;
    }

    protected override void Shutdown()
    {
        CommandBinds.Unregister<MappingState>();

        Screen.Prototypes.SearchBar.OnTextChanged -= OnSearch;
        Screen.Prototypes.CollapseAllButton.OnPressed -= OnCollapseAll;
        Screen.Prototypes.ClearSearchButton.OnPressed -= OnClearSearch;
        Screen.Prototypes.GetPrototypeData -= OnGetData;
        Screen.Prototypes.SelectionChanged -= OnSelected;
        Screen.Prototypes.CollapseToggled -= OnCollapseToggled;
        Screen.Pick.OnPressed -= OnPickPressed;
        Screen.Delete.OnPressed -= OnDeletePressed;
        Screen.EntityReplaceButton.OnToggled -= OnEntityReplacePressed;
        Screen.EntityPlacementMode.OnItemSelected -= OnEntityPlacementSelected;
        Screen.EraseEntityButton.OnToggled -= OnEraseEntityPressed;
        Screen.EraseDecalButton.OnToggled -= OnEraseDecalPressed;
        // open-space edit start
        Screen.CcwButton.OnPressed -= OnRotateCcwPressed;
        Screen.CwButton.OnPressed -= OnRotateCwPressed;
        Screen.DeleteEntityButton.OnPressed -= OnDeleteEntityPressed;
        Screen.DeselectButton.OnPressed -= OnDeselectPressed;
        Screen.DeleteToolButton.OnPressed -= OnDeletePressed;
        Screen.CableToolButton.OnPressed -= OnCableToolPressed;
        Screen.PipeToolButton.OnPressed -= OnPipeToolPressed;
        Screen.QuickFilterAllButton.OnPressed -= OnQuickFilterAllPressed;
        Screen.QuickFilterCablesButton.OnPressed -= OnQuickFilterCablesPressed;
        Screen.QuickFilterAirlocksButton.OnPressed -= OnQuickFilterAirlocksPressed;
        Screen.QuickFilterPipesButton.OnPressed -= OnQuickFilterPipesPressed;
        Screen.QuickFilterDisposalButton.OnPressed -= OnQuickFilterDisposalPressed;
        Screen.ApplyPipeColorButton.OnPressed -= OnApplyPipeColorPressed;
        Screen.OpenGridButton.OnPressed -= OnOpenGridPressed;
        Screen.ApplyGridButton.OnPressed -= OnApplyGridPressed;
        Screen.ExitMappingButton.OnPressed -= OnExitMappingPressed;
        Screen.TilesPaletteButton.OnPressed -= OnTilesPalettePressed;
        Screen.EntitiesPaletteButton.OnPressed -= OnEntitiesPalettePressed;
        Screen.GroupPaletteButton.OnPressed -= OnGroupPalettePressed;
        // open-space edit end
        _placement.PlacementChanged -= OnPlacementChanged;
        _prototypeManager.PrototypesReloaded -= OnPrototypesReloaded;

        UserInterfaceManager.ClearWindows();
        _loadController.UnloadScreen();
        UserInterfaceManager.UnloadScreen();

        var context = _input.Contexts.GetContext("common");
        context.RemoveFunction(ContentKeyFunctions.MappingUnselect);
        context.RemoveFunction(ContentKeyFunctions.SaveMap);
        context.RemoveFunction(ContentKeyFunctions.MappingEnablePick);
        context.RemoveFunction(ContentKeyFunctions.MappingEnableDelete);
        context.RemoveFunction(ContentKeyFunctions.MappingPick);
        context.RemoveFunction(ContentKeyFunctions.MappingRemoveDecal);
        context.RemoveFunction(ContentKeyFunctions.MappingCancelEraseDecal);
        context.RemoveFunction(ContentKeyFunctions.MappingOpenContextMenu);

        _overlays.RemoveOverlay<MappingOverlay>();

        base.Shutdown();
    }

    private void EnsureSetup()
    {
        if (_setup)
            return;

        _setup = true;

        _entityMenuController = UserInterfaceManager.GetUIController<EntityMenuUIController>();

        _decal = _entityManager.System<DecalPlacementSystem>();
        // open-space edit start
        _contentEye = _entityManager.System<ContentEyeSystem>();
        _subfloor = _entityManager.System<SubFloorHideSystem>();
        // open-space edit end
        _sprite = _entityManager.System<SpriteSystem>();
        _transform = _entityManager.System<TransformSystem>();
        _verbs = _entityManager.System<VerbSystem>();
        ReloadPrototypes();
    }

    private void ReloadPrototypes()
    {
        // open-space edit start
        _allPrototypes.Clear();
        _allPrototypesDict.Clear();
        _idDict.Clear();
        _prototypes.Clear();
        _paletteRoots.Clear();
        _entitiesRoot = null;
        _tilesRoot = null;
        // open-space edit end

        var entities = new MappingPrototype(null, Loc.GetString("mapping-entities")) { Children = new List<MappingPrototype>() };
        _prototypes.Add(entities);
        // open-space edit start
        _entitiesRoot = entities;
        // open-space edit end

        var mappings = new Dictionary<string, MappingPrototype>();
        foreach (var entity in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            Register(entity, entity.ID, entities);
        }

        Sort(mappings, entities);
        mappings.Clear();

        var tiles = new MappingPrototype(null, Loc.GetString("mapping-tiles")) { Children = new List<MappingPrototype>() };
        _prototypes.Add(tiles);
        // open-space edit start
        _tilesRoot = tiles;
        // open-space edit end

        foreach (var tile in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            Register(tile, tile.ID, tiles);
        }

        Sort(mappings, tiles);
        mappings.Clear();

        var decals = new MappingPrototype(null, Loc.GetString("mapping-decals")) { Children = new List<MappingPrototype>() };
        _prototypes.Add(decals);

        foreach (var decal in _prototypeManager.EnumeratePrototypes<DecalPrototype>())
        {
            Register(decal, decal.ID, decals);
        }

        Sort(mappings, decals);
        mappings.Clear();
    }

    private void Sort(Dictionary<string, MappingPrototype> prototypes, MappingPrototype topLevel)
    {
        static int Compare(MappingPrototype a, MappingPrototype b)
        {
            return string.Compare(a.Name, b.Name, OrdinalIgnoreCase);
        }

        topLevel.Children ??= new List<MappingPrototype>();

        foreach (var prototype in prototypes.Values)
        {
            if (prototype.Parents == null && prototype != topLevel)
            {
                prototype.Parents = new List<MappingPrototype> { topLevel };
                topLevel.Children.Add(prototype);
            }

            prototype.Parents?.Sort(Compare);
            prototype.Children?.Sort(Compare);
        }

        topLevel.Children.Sort(Compare);
    }

    private MappingPrototype? Register<T>(T? prototype, string id, MappingPrototype topLevel) where T : class, IPrototype, IInheritingPrototype
    {
        {
            if (prototype == null &&
                _prototypeManager.TryIndex(id, out prototype) &&
                prototype is EntityPrototype entity)
            {
                if (entity.HideSpawnMenu || entity.Abstract)
                    prototype = null;
            }
        }

        if (prototype == null)
        {
            if (!_prototypeManager.TryGetMapping(typeof(T), id, out var node))
            {
                _sawmill.Error($"No {nameof(T)} found with id {id}");
                return null;
            }

            var ids = _idDict.GetOrNew(typeof(T));
            if (ids.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var name = node.TryGet("name", out ValueDataNode? nameNode)
                    ? nameNode.Value
                    : id;

                if (node.TryGet("suffix", out ValueDataNode? suffix))
                    name = $"{name} [{suffix.Value}]";

                // open-space edit start
                mapping = new MappingPrototype(prototype, name, BuildPrototypeSearchText(id, name));
                // open-space edit end
                _allPrototypes.Add(mapping);
                ids.Add(id, mapping);

                if (node.TryGet("parent", out ValueDataNode? parentValue))
                {
                    var parent = Register<T>(null, parentValue.Value, topLevel);

                    if (parent != null)
                    {
                        mapping.Parents ??= new List<MappingPrototype>();
                        mapping.Parents.Add(parent);
                        parent.Children ??= new List<MappingPrototype>();
                        parent.Children.Add(mapping);
                    }
                }
                else if (node.TryGet("parent", out SequenceDataNode? parentSequence))
                {
                    foreach (var parentNode in parentSequence.Cast<ValueDataNode>())
                    {
                        var parent = Register<T>(null, parentNode.Value, topLevel);

                        if (parent != null)
                        {
                            mapping.Parents ??= new List<MappingPrototype>();
                            mapping.Parents.Add(parent);
                            parent.Children ??= new List<MappingPrototype>();
                            parent.Children.Add(mapping);
                        }
                    }
                }
                else
                {
                    topLevel.Children ??= new List<MappingPrototype>();
                    topLevel.Children.Add(mapping);
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(topLevel);
                }

                return mapping;
            }
        }
        else
        {
            var ids = _idDict.GetOrNew(typeof(T));
            if (ids.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var entity = prototype as EntityPrototype;
                // open-space edit start
                var name = GetMappingPrototypeName(prototype);
                // open-space edit end

                if (!string.IsNullOrWhiteSpace(entity?.EditorSuffix))
                    name = $"{name} [{entity.EditorSuffix}]";

                // open-space edit start
                mapping = new MappingPrototype(prototype, name, GetMappingPrototypeSearchText(prototype, name));
                // open-space edit end
                _allPrototypes.Add(mapping);
                _allPrototypesDict.Add(prototype, mapping);
                ids.Add(prototype.ID, mapping);
            }

            if (prototype.Parents == null)
            {
                topLevel.Children ??= new List<MappingPrototype>();
                topLevel.Children.Add(mapping);
                mapping.Parents ??= new List<MappingPrototype>();
                mapping.Parents.Add(topLevel);
                return mapping;
            }

            foreach (var parentId in prototype.Parents)
            {
                var parent = Register<T>(null, parentId, topLevel);

                if (parent != null)
                {
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(parent);
                    parent.Children ??= new List<MappingPrototype>();
                    parent.Children.Add(mapping);
                }
            }

            return mapping;
        }
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        _updatePlacement = true;
    }

    protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
    {
        if (args.Viewport == null)
            base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
        else
            base.OnKeyBindStateChanged(args);
    }

    private void OnSearch(LineEditEventArgs args)
    {
        if (string.IsNullOrEmpty(args.Text))
        {
            Screen.Prototypes.PrototypeList.Visible = true;
            Screen.Prototypes.SearchList.Visible = false;
            return;
        }

        var matches = new List<MappingPrototype>();
        // open-space edit start
        foreach (var prototype in GetPaletteSearchPrototypes())
        // open-space edit end
        {
            if (prototype.SearchText.Contains(args.Text, OrdinalIgnoreCase))
                matches.Add(prototype);
        }

        matches.Sort(static (a, b) => string.Compare(a.Name, b.Name, OrdinalIgnoreCase));

        Screen.Prototypes.PrototypeList.Visible = false;
        Screen.Prototypes.SearchList.Visible = true;
        Screen.Prototypes.Search(matches);
    }

    private void OnCollapseAll(ButtonEventArgs args)
    {
        foreach (var child in Screen.Prototypes.PrototypeList.Children)
        {
            if (child is not MappingSpawnButton button)
                continue;

            Collapse(button);
        }

        Screen.Prototypes.ScrollContainer.SetScrollValue(new Vector2(0, 0));
    }

    private void OnClearSearch(ButtonEventArgs obj)
    {
        Screen.Prototypes.SearchBar.Text = string.Empty;
        OnSearch(new LineEditEventArgs(Screen.Prototypes.SearchBar, string.Empty));
    }

    // open-space edit start
    private void PopulateQuickPalette()
    {
        Screen.QuickPaletteList.RemoveAllChildren();

        foreach (var entry in QuickPaletteEntries)
        {
            var label = GetQuickPaletteLabel(entry);
            if (label == null)
                continue;

            var button = new Button
            {
                Text = label,
                ToolTip = entry.Id,
                HorizontalExpand = true,
                StyleClasses = { "ButtonSquare" },
            };

            button.OnPressed += _ => OnQuickPalettePressed(entry);
            Screen.QuickPaletteList.AddChild(button);
        }
    }

    private string? GetQuickPaletteLabel(QuickPaletteEntry entry)
    {
        return entry.Kind switch
        {
            MappingQuickPaletteKind.Delete => Loc.GetString("mappingui-quick-delete-objects"),
            MappingQuickPaletteKind.Entity => GetMappingById(typeof(EntityPrototype), entry.Id)?.Name,
            MappingQuickPaletteKind.Tile => GetMappingById(typeof(ContentTileDefinition), entry.Id)?.Name,
            _ => entry.Id
        };
    }

    private void OnQuickPalettePressed(QuickPaletteEntry entry)
    {
        switch (entry.Kind)
        {
            case MappingQuickPaletteKind.Delete:
                EnableDelete();
                break;
            case MappingQuickPaletteKind.Entity:
                SelectEntityPrototype(entry.Id);
                break;
            case MappingQuickPaletteKind.Tile:
                SelectTilePrototype(entry.Id);
                break;
        }
    }

    private void RefreshPalette()
    {
        _paletteRoots.Clear();
        foreach (var root in GetPaletteRoots())
        {
            _paletteRoots.Add(root);
        }

        Screen.TilesPaletteButton.Pressed = _paletteMode == MappingPaletteMode.Tiles;
        Screen.EntitiesPaletteButton.Pressed = _paletteMode == MappingPaletteMode.Entities;
        Screen.GroupPaletteButton.Pressed = _paletteGrouped;
        Screen.GroupPaletteButton.Disabled = _paletteFilter != MappingPaletteFilter.None;
        Screen.QuickFilterAllButton.Pressed = _paletteMode == MappingPaletteMode.Entities &&
                                             _paletteFilter == MappingPaletteFilter.None;
        Screen.QuickFilterCablesButton.Pressed = _paletteFilter == MappingPaletteFilter.Cables;
        Screen.QuickFilterAirlocksButton.Pressed = _paletteFilter == MappingPaletteFilter.Airlocks;
        Screen.QuickFilterPipesButton.Pressed = _paletteFilter == MappingPaletteFilter.Pipes;
        Screen.QuickFilterDisposalButton.Pressed = _paletteFilter == MappingPaletteFilter.Disposal;
        Screen.Prototypes.UpdateVisible(_paletteRoots);
        OnSearch(new LineEditEventArgs(Screen.Prototypes.SearchBar, Screen.Prototypes.SearchBar.Text));
    }

    private IEnumerable<MappingPrototype> GetPaletteRoots()
    {
        if (_paletteMode == MappingPaletteMode.Tiles)
        {
            if (_tilesRoot != null)
            {
                if (_paletteGrouped)
                {
                    yield return _tilesRoot;
                }
                else
                {
                    foreach (var child in EnumeratePaletteChildren(_tilesRoot))
                    {
                        yield return child;
                    }
                }
            }

            yield break;
        }

        if (_paletteFilter != MappingPaletteFilter.None)
        {
            foreach (var child in GetFilteredEntityPalette(_paletteFilter))
            {
                yield return child;
            }

            yield break;
        }

        if (_entitiesRoot != null)
            yield return _entitiesRoot;
    }

    private IEnumerable<MappingPrototype> GetFilteredEntityPalette(MappingPaletteFilter filter)
    {
        if (_entitiesRoot == null)
            yield break;

        foreach (var child in EnumeratePaletteChildren(_entitiesRoot)
                     .Where(child => child.Prototype is EntityPrototype entity &&
                                     PaletteFilterMatches(filter, entity.ID))
                     .OrderBy(child => child.Name, StringComparer.OrdinalIgnoreCase))
        {
            yield return child;
        }
    }

    private static bool PaletteFilterMatches(MappingPaletteFilter filter, string id)
    {
        return filter switch
        {
            MappingPaletteFilter.Cables => id.Equals("CableApcExtension", OrdinalIgnoreCase) ||
                                           id.Equals("CableMV", OrdinalIgnoreCase) ||
                                           id.Equals("CableHV", OrdinalIgnoreCase),
            MappingPaletteFilter.Airlocks => id.Contains("Airlock", OrdinalIgnoreCase) ||
                                             id.Contains("Windoor", OrdinalIgnoreCase),
            MappingPaletteFilter.Pipes => id.Contains("GasPipe", OrdinalIgnoreCase) ||
                                          id.Contains("GasVent", OrdinalIgnoreCase),
            MappingPaletteFilter.Disposal => id.Contains("Disposal", OrdinalIgnoreCase),
            _ => true
        };
    }

    private IEnumerable<MappingPrototype> GetPaletteSearchPrototypes()
    {
        foreach (var root in GetPaletteRoots())
        {
            foreach (var child in EnumeratePaletteChildren(root))
            {
                yield return child;
            }
        }
    }

    private IEnumerable<MappingPrototype> EnumeratePaletteChildren(MappingPrototype prototype)
    {
        if (prototype.Prototype != null)
            yield return prototype;

        if (prototype.Children == null)
            yield break;

        foreach (var child in prototype.Children)
        {
            foreach (var nested in EnumeratePaletteChildren(child))
            {
                yield return nested;
            }
        }
    }

    private void SetPaletteMode(MappingPaletteMode mode)
    {
        var previousFilter = _paletteFilter;
        _paletteFilter = MappingPaletteFilter.None;

        if (_paletteMode == mode && previousFilter == _paletteFilter)
            return;

        _paletteMode = mode;
        RefreshPalette();
    }

    private void SetPaletteFilter(MappingPaletteFilter filter)
    {
        _paletteMode = MappingPaletteMode.Entities;
        _paletteFilter = filter;
        Screen.Prototypes.SearchBar.Text = string.Empty;
        RefreshPalette();
    }

    private MappingPrototype? GetMappingById(Type prototypeType, string prototypeId)
    {
        if (!_idDict.TryGetValue(prototypeType, out var ids) ||
            !ids.TryGetValue(prototypeId, out var mapping))
        {
            return null;
        }

        return mapping;
    }

    private string GetMappingPrototypeName(IPrototype prototype)
    {
        if (prototype is ContentTileDefinition tile)
            return GetLocalizedTileName(tile);

        return prototype is EntityPrototype entity
            ? entity.Name
            : prototype.ID;
    }

    private string GetMappingPrototypeSearchText(IPrototype prototype, string displayName)
    {
        if (prototype is ContentTileDefinition tile)
            return BuildPrototypeSearchText(tile.ID, displayName, tile.Name, SplitPrototypeId(tile.ID));

        if (prototype is EntityPrototype entity)
            return BuildPrototypeSearchText(entity.ID, displayName, entity.EditorSuffix, SplitPrototypeId(entity.ID));

        return BuildPrototypeSearchText(prototype.ID, displayName, SplitPrototypeId(prototype.ID));
    }

    private string GetLocalizedTileName(ContentTileDefinition tile)
    {
        if (string.IsNullOrWhiteSpace(tile.Name))
            return tile.ID;

        return Loc.GetString(tile.Name);
    }

    private static string BuildPrototypeSearchText(params string?[] values)
    {
        return string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string SplitPrototypeId(string id)
    {
        var chars = new List<char>(id.Length * 2);

        for (var i = 0; i < id.Length; i++)
        {
            var current = id[i];
            if (i > 0 &&
                char.IsUpper(current) &&
                (char.IsLower(id[i - 1]) || i + 1 < id.Length && char.IsLower(id[i + 1])))
            {
                chars.Add(' ');
            }

            chars.Add(current);
        }

        return new string(chars.ToArray());
    }
    // open-space edit end

    private void OnGetData(IPrototype prototype, List<Texture> textures)
    {
        switch (prototype)
        {
            case EntityPrototype entity:
                textures.AddRange(_sprite.GetPrototypeTextures(entity).Select(t => t.Default));
                break;
            case DecalPrototype decal:
                textures.Add(_sprite.Frame0(decal.Sprite));
                break;
            case ContentTileDefinition tile:
                if (tile.Sprite?.ToString() is { } sprite)
                    textures.Add(_resources.GetResource<TextureResource>(sprite).Texture);
                break;
        }
    }

    private void OnSelected(MappingPrototype mapping)
    {
        if (mapping.Prototype == null)
            return;

        var chain = new Stack<MappingPrototype>();
        chain.Push(mapping);

        var parent = mapping.Parents?.FirstOrDefault();
        while (parent != null)
        {
            chain.Push(parent);
            parent = parent.Parents?.FirstOrDefault();
        }

        _lastClicked = null;

        Control? last = null;
        var children = Screen.Prototypes.PrototypeList.Children;
        foreach (var prototype in chain)
        {
            foreach (var child in children)
            {
                if (child is MappingSpawnButton button &&
                    button.Prototype == prototype)
                {
                    UnCollapse(button);
                    OnSelected(button, prototype.Prototype);
                    children = button.ChildrenPrototypes.Children;
                    last = child;
                    break;
                }
            }
        }

        if (last != null && Screen.Prototypes.PrototypeList.Visible)
            _scrollTo = last;
    }

    private void OnSelected(MappingSpawnButton button, IPrototype? prototype)
    {
        var time = _timing.CurTime;
        if (prototype is DecalPrototype)
            Screen.SelectDecal(prototype.ID);

        // Double-click functionality if it's collapsible.
        if (_lastClicked is { } lastClicked &&
            lastClicked.Button == button &&
            lastClicked.At > time - TimeSpan.FromSeconds(0.333) &&
            string.IsNullOrEmpty(Screen.Prototypes.SearchBar.Text) &&
            button.CollapseButton.Visible)
        {
            button.CollapseButton.Pressed = !button.CollapseButton.Pressed;
            ToggleCollapse(button);
            button.Button.Pressed = true;
            Screen.Prototypes.Selected = button;
            _lastClicked = null;
            return;
        }

        // Toggle if it's the same button (at least if we just unclicked it).
        if (!button.Button.Pressed && button.Prototype?.Prototype != null && _lastClicked?.Button == button)
        {
            _lastClicked = null;
            Deselect();
            return;
        }

        _lastClicked = (time, button);

        if (button.Prototype == null)
            return;

        if (Screen.Prototypes.Selected is { } oldButton &&
            oldButton != button)
        {
            Deselect();
        }

        Screen.EntityContainer.Visible = false;
        Screen.DecalContainer.Visible = false;

        switch (prototype)
        {
            case EntityPrototype entity:
            {
                var placementId = Screen.EntityPlacementMode.SelectedId;

                var placement = new PlacementInformation
                {
                    PlacementOption = placementId > 0 ? EntitySpawnWindow.InitOpts[placementId] : entity.PlacementMode,
                    EntityType = entity.ID,
                    IsTile = false
                };

                Screen.EntityContainer.Visible = true;
                _decal.SetActive(false);
                _placement.BeginPlacing(placement);
                break;
            }
            case DecalPrototype decal:
                _placement.Clear();

                _decal.SetActive(true);
                _decal.UpdateDecalInfo(decal.ID, Color.White, 0, true, 0, false);
                Screen.DecalContainer.Visible = true;
                break;
            case ContentTileDefinition tile:
            {
                var placement = new PlacementInformation
                {
                    PlacementOption = "AlignTileAny",
                    TileType = tile.TileId,
                    IsTile = true
                };

                _decal.SetActive(false);
                _placement.BeginPlacing(placement);
                break;
            }
            default:
                _placement.Clear();
                break;
        }

        Screen.Prototypes.Selected = button;

        button.Button.Pressed = true;
    }

    private void Deselect()
    {
        if (Screen.Prototypes.Selected is { } selected)
        {
            selected.Button.Pressed = false;
            Screen.Prototypes.Selected = null;

            if (selected.Prototype?.Prototype is DecalPrototype)
            {
                _decal.SetActive(false);
                Screen.DecalContainer.Visible = false;
            }

            if (selected.Prototype?.Prototype is EntityPrototype)
            {
                _placement.Clear();
            }

            if (selected.Prototype?.Prototype is ContentTileDefinition)
            {
                _placement.Clear();
            }
        }
    }

    private void OnCollapseToggled(MappingSpawnButton button, ButtonToggledEventArgs args)
    {
        ToggleCollapse(button);
    }

    private void OnPickPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
            EnablePick();
        else
            DisablePick();
    }

    private void OnDeletePressed(ButtonEventArgs obj)
    {
        if (obj.Button.Pressed)
            EnableDelete();
        else
            DisableDelete();
    }

    // open-space edit start
    private void OnRotateCcwPressed(ButtonEventArgs args)
    {
        RotatePlacement(clockwise: false);
    }

    private void OnRotateCwPressed(ButtonEventArgs args)
    {
        RotatePlacement(clockwise: true);
    }

    private void OnDeleteEntityPressed(ButtonEventArgs args)
    {
        EnableDelete();
    }

    private void OnDeselectPressed(ButtonEventArgs args)
    {
        Deselect();
    }

    private void OnCableToolPressed(ButtonEventArgs args)
    {
        SelectEntityPrototype("CableMV");
    }

    private void OnPipeToolPressed(ButtonEventArgs args)
    {
        SelectEntityPrototype("GasPipeStraight");
    }

    private void OnQuickFilterAllPressed(ButtonEventArgs args)
    {
        SetPaletteFilter(MappingPaletteFilter.None);
    }

    private void OnQuickFilterCablesPressed(ButtonEventArgs args)
    {
        SetPaletteFilter(MappingPaletteFilter.Cables);
    }

    private void OnQuickFilterAirlocksPressed(ButtonEventArgs args)
    {
        SetPaletteFilter(MappingPaletteFilter.Airlocks);
    }

    private void OnQuickFilterPipesPressed(ButtonEventArgs args)
    {
        SetPaletteFilter(MappingPaletteFilter.Pipes);
    }

    private void OnQuickFilterDisposalPressed(ButtonEventArgs args)
    {
        SetPaletteFilter(MappingPaletteFilter.Disposal);
    }

    private void OnApplyPipeColorPressed(ButtonEventArgs args)
    {
        ApplyPipeColor();
    }

    private void OnTilesPalettePressed(ButtonEventArgs args)
    {
        SetPaletteMode(MappingPaletteMode.Tiles);
    }

    private void OnEntitiesPalettePressed(ButtonEventArgs args)
    {
        SetPaletteMode(MappingPaletteMode.Entities);
    }

    private void OnGroupPalettePressed(ButtonEventArgs args)
    {
        _paletteGrouped = args.Button.Pressed;
        RefreshPalette();
    }

    private void OnOpenGridPressed(ButtonEventArgs args)
    {
        if (_selectedGrid is not { } grid ||
            _entityManager.Deleted(grid) ||
            !_entityManager.TryGetNetEntity(grid, out var netEntity))
        {
            return;
        }

        _viewVariables.OpenVV(netEntity.Value);
    }

    private void OnApplyGridPressed(ButtonEventArgs args)
    {
        ApplyGridInfo();
    }

    private void OnExitMappingPressed(ButtonEventArgs args)
    {
        ExitMappingUi();
    }

    private void RotatePlacement(bool clockwise)
    {
        var direction = _placement.Direction == Direction.Invalid
            ? Direction.South
            : _placement.Direction;

        _placement.Direction = RotateDirection(direction, clockwise);
    }

    private static Direction RotateDirection(Direction direction, bool clockwise)
    {
        if (clockwise)
            return direction.GetClockwise90Degrees();

        var rotated = direction;
        for (var i = 0; i < 3; i++)
        {
            rotated = rotated.GetClockwise90Degrees();
        }

        return rotated;
    }

    private void SelectEntityPrototype(string prototypeId)
    {
        if (GetMappingById(typeof(EntityPrototype), prototypeId) is not { } mapping)
        {
            return;
        }

        SetPaletteMode(MappingPaletteMode.Entities);
        OnSelected(mapping);
    }

    private void SelectTilePrototype(string prototypeId)
    {
        if (GetMappingById(typeof(ContentTileDefinition), prototypeId) is not { } mapping)
        {
            return;
        }

        SetPaletteMode(MappingPaletteMode.Tiles);
        OnSelected(mapping);
    }

    private void ApplyPipeColor()
    {
        if (_inspectedEntity is not { } entity ||
            !_entityManager.TryGetNetEntity(entity, out var netEntity))
        {
            return;
        }

        var group = Screen.PipeTypeLineEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(group))
            group = "Pipe";

        var color = Screen.PipeColorLineEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(color))
            return;

        if (!color.StartsWith('#'))
            color = $"#{color}";

        _console.RemoteExecuteCommand(null, $"colornetwork {netEntity.Value.Id} {QuoteCommandArgument(group)} {QuoteCommandArgument(color)}");
    }

    private void ApplyGridInfo()
    {
        if (_selectedGrid is not { } grid ||
            _entityManager.Deleted(grid) ||
            !_entityManager.TryGetNetEntity(grid, out var netEntity))
        {
            return;
        }

        var color = Screen.GridRadarColorLineEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(color))
            return;

        if (!color.StartsWith('#'))
            color = $"#{color}";

        var name = Screen.GridNameLineEdit.Text.Trim();
        _console.RemoteExecuteCommand(null, $"mappinguigridset {netEntity.Value.Id} {QuoteCommandArgument(color)} {QuoteCommandArgument(name)}");
    }

    private void ExitMappingUi()
    {
        if (!_light.LockConsoleAccess)
            _light.Enabled = true;

        _subfloor.ShowAll = false;
        _marker.MarkersVisible = false;

        if (_player.LocalEntity is { } player && _entityManager.HasComponent<EyeComponent>(player))
        {
            _contentEye.RequestEye(true, true);
        }
        else
        {
            _eye.CurrentEye.DrawFov = true;
            _eye.CurrentEye.DrawLight = true;
        }

        _state.RequestStateChange<GameplayState>();
    }

    private static string QuoteCommandArgument(string argument)
    {
        return $"\"{argument.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }
    // open-space edit end

    private void OnEntityReplacePressed(ButtonToggledEventArgs args)
    {
        _placement.Replacement = args.Pressed;
    }

    private void OnEntityPlacementSelected(ItemSelectedEventArgs args)
    {
        Screen.EntityPlacementMode.SelectId(args.Id);

        if (_placement.CurrentMode != null)
        {
            var placement = new PlacementInformation
            {
                PlacementOption = EntitySpawnWindow.InitOpts[args.Id],
                EntityType = _placement.CurrentPermission!.EntityType,
                TileType = _placement.CurrentPermission.TileType,
                Range = 2,
                IsTile = _placement.CurrentPermission.IsTile,
            };

            _placement.BeginPlacing(placement);
        }
    }

    private void OnEraseEntityPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed == _placement.Eraser)
            return;

        if (args.Button.Pressed)
            EnableEraser();
        else
            DisableEraser();
    }

    private void OnEraseDecalPressed(ButtonToggledEventArgs args)
    {
        _placement.Clear();
        Deselect();
        Screen.EraseEntityButton.Pressed = false;
        _updatePlacement = true;
        _updateEraseDecal = args.Pressed;
    }

    private void EnableEraser()
    {
        if (_placement.Eraser)
            return;

        _placement.Clear();
        _placement.ToggleEraser();
        Screen.EntityPlacementMode.Disabled = true;
        Screen.EraseDecalButton.Pressed = false;
        Deselect();
    }

    private void DisableEraser()
    {
        if (!_placement.Eraser)
            return;

        _placement.ToggleEraser();
        Screen.EntityPlacementMode.Disabled = false;
    }

    private void EnablePick()
    {
        Screen.UnPressActionsExcept(Screen.Pick);
        State = CursorState.Pick;
    }

    private void DisablePick()
    {
        Screen.Pick.Pressed = false;
        State = CursorState.None;
    }

    private void EnableDelete()
    {
        Screen.UnPressActionsExcept(Screen.Delete);
        State = CursorState.Delete;
        EnableEraser();
    }

    private void DisableDelete()
    {
        Screen.Delete.Pressed = false;
        // open-space edit start
        Screen.DeleteToolButton.Pressed = false;
        // open-space edit end
        State = CursorState.None;
        DisableEraser();
    }

    private bool HandleMappingUnselect(in PointerInputCmdArgs args)
    {
        if (Screen.Prototypes.Selected is not { Prototype.Prototype: DecalPrototype })
            return false;

        Deselect();
        return true;
    }

    private bool HandleSaveMap(in PointerInputCmdArgs args)
    {
#if FULL_RELEASE
        return false;
#else
        if (!_admin.IsAdmin(true) || !_admin.HasFlag(AdminFlags.Host))
            return false;

        SaveMap();
        return true;
#endif
    }

    private bool HandleEnablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        EnablePick();
        return true;
    }

    private bool HandleDisablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        DisablePick();
        return true;
    }

    private bool HandleEnableDelete(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        EnableDelete();
        return true;
    }

    private bool HandleDisableDelete(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        DisableDelete();
        return true;
    }

    private bool HandlePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (State != CursorState.Pick)
            return false;

        MappingPrototype? button = null;

        // Try and get tile under it
        // TODO: Separate mode for decals.
        if (!uid.IsValid())
        {
            var mapPos = _transform.ToMapCoordinates(coords);

            if (_mapMan.TryFindGridAt(mapPos, out var gridUid, out var grid) &&
                _entityManager.System<SharedMapSystem>().TryGetTileRef(gridUid, grid, coords, out var tileRef) &&
                _allPrototypesDict.TryGetValue(_entityManager.System<TurfSystem>().GetContentTileDefinition(tileRef), out button))
            {
                OnSelected(button);
                return true;
            }
        }

        if (button == null)
        {
            if (uid == EntityUid.Invalid ||
                _entityManager.GetComponentOrNull<MetaDataComponent>(uid) is not { EntityPrototype: { } prototype } ||
                !_allPrototypesDict.TryGetValue(prototype, out button))
            {
                // we always block other input handlers if pick mode is enabled
                // this makes you not accidentally place something in space because you
                // miss-clicked while holding down the pick hotkey
                return true;
            }

            // Selected an entity
            OnSelected(button);

            // Match rotation
            _placement.Direction = _entityManager.GetComponent<TransformComponent>(uid).LocalRotation.GetDir();
        }

        return true;
    }

    private bool HandleEditorCancelPlace(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Screen.EraseDecalButton.Pressed)
            return false;

        _entityNetwork.SendSystemNetworkMessage(new RequestDecalRemovalEvent(_entityManager.GetNetCoordinates(coords)));
        return true;
    }

    private bool HandleCancelEraseDecal(in PointerInputCmdArgs args)
    {
        if (!Screen.EraseDecalButton.Pressed)
            return false;

        Screen.EraseDecalButton.Pressed = false;
        return true;
    }

    private async void SaveMap()
    {
        await _mapping.SaveMap();
    }

    private void ToggleCollapse(MappingSpawnButton button)
    {
        if (button.CollapseButton.Pressed)
        {
            if (button.Prototype?.Children != null)
            {
                foreach (var child in button.Prototype.Children)
                {
                    Screen.Prototypes.Insert(button.ChildrenPrototypes, child, true);
                }
            }

            button.CollapseButton.Label.Text = "▼";
        }
        else
        {
            button.ChildrenPrototypes.RemoveAllChildren();
            button.CollapseButton.Label.Text = "▶";
        }
    }

    private void Collapse(MappingSpawnButton button)
    {
        if (!button.CollapseButton.Pressed)
            return;

        button.CollapseButton.Pressed = false;
        ToggleCollapse(button);
    }


    private void UnCollapse(MappingSpawnButton button)
    {
        if (button.CollapseButton.Pressed)
            return;

        button.CollapseButton.Pressed = true;
        ToggleCollapse(button);
    }

    // open-space edit start
    private void RefreshHoveredEntityInfo()
    {
        if (GetHoveredEntity() is { } hovered)
            _inspectedEntity = hovered;
        else if (_inspectedEntity != null && _entityManager.Deleted(_inspectedEntity))
            _inspectedEntity = null;

    }

    private void RefreshEntityInfo()
    {
        var none = Loc.GetString("mappingui-entity-none");
        if (_inspectedEntity is not { } entity || _entityManager.Deleted(entity))
        {
            Screen.EntityIdLabel.Text = none;
            Screen.EntityNameLabel.Text = none;
            Screen.EntityPositionLabel.Text = none;
            Screen.EntityRotationLabel.Text = none;
            return;
        }

        Screen.EntityIdLabel.Text = _entityManager.TryGetNetEntity(entity, out var netEntity)
            ? netEntity.Value.Id.ToString(CultureInfo.InvariantCulture)
            : entity.ToString();
        Screen.EntityNameLabel.Text = _entityManager.TryGetComponent(entity, out MetaDataComponent? meta)
            ? meta.EntityName
            : none;

        if (!_entityManager.TryGetComponent(entity, out TransformComponent? xform))
        {
            Screen.EntityPositionLabel.Text = none;
            Screen.EntityRotationLabel.Text = none;
            return;
        }

        var mapPos = _transform.ToMapCoordinates(xform.Coordinates);
        Screen.EntityPositionLabel.Text = $"{mapPos.X:0.##}, {mapPos.Y:0.##} [{(int) mapPos.MapId}]";
        Screen.EntityRotationLabel.Text = $"{xform.LocalRotation.Degrees:0.#}°";
    }

    private void RefreshGridInfo()
    {
        var none = Loc.GetString("mappingui-grid-none");
        if (_selectedGrid is not { } grid || _entityManager.Deleted(grid))
        {
            Screen.GridIdLabel.Text = none;
            Screen.GridMapLabel.Text = none;
            Screen.GridPositionLabel.Text = none;
            if (!Screen.GridNameLineEdit.HasKeyboardFocus())
                Screen.GridNameLineEdit.Text = string.Empty;
            if (!Screen.GridRadarColorLineEdit.HasKeyboardFocus())
                Screen.GridRadarColorLineEdit.Text = Color.Gold.ToHexNoAlpha();
            Screen.OpenGridButton.Disabled = true;
            Screen.ApplyGridButton.Disabled = true;
            return;
        }

        Screen.OpenGridButton.Disabled = false;
        Screen.ApplyGridButton.Disabled = false;
        Screen.GridIdLabel.Text = _entityManager.TryGetNetEntity(grid, out var netEntity)
            ? netEntity.Value.Id.ToString(CultureInfo.InvariantCulture)
            : grid.ToString();

        if (!_entityManager.TryGetComponent(grid, out TransformComponent? xform))
        {
            Screen.GridMapLabel.Text = none;
            Screen.GridPositionLabel.Text = none;
            return;
        }

        var mapPos = _transform.ToMapCoordinates(xform.Coordinates);
        Screen.GridMapLabel.Text = ((int) mapPos.MapId).ToString(CultureInfo.InvariantCulture);
        Screen.GridPositionLabel.Text = $"{mapPos.X:0.##}, {mapPos.Y:0.##}";

        if (_entityManager.TryGetComponent(grid, out MetaDataComponent? meta) &&
            !Screen.GridNameLineEdit.HasKeyboardFocus())
        {
            Screen.GridNameLineEdit.Text = meta.EntityName;
        }

        if (!Screen.GridRadarColorLineEdit.HasKeyboardFocus())
        {
            var color = _entityManager.TryGetComponent(grid, out IFFComponent? iff)
                ? iff.Color
                : Color.Gold;
            Screen.GridRadarColorLineEdit.Text = color.ToHexNoAlpha();
        }
    }

    private void RefreshGridList(bool force = false)
    {
        if (!force && _timing.CurTime < _nextGridRefresh)
            return;

        _nextGridRefresh = _timing.CurTime + TimeSpan.FromSeconds(1);
        Screen.GridList.RemoveAllChildren();

        var mapId = _eye.CurrentEye.Position.MapId;
        var grids = _mapMan.GetAllGrids(mapId).ToList();
        if (grids.Count == 0)
        {
            _selectedGrid = null;
            Screen.GridList.AddChild(new Label
            {
                Text = Loc.GetString("mappingui-no-grids"),
                ClipText = true
            });
            RefreshGridInfo();
            return;
        }

        if (_selectedGrid is not { } selectedGrid ||
            _entityManager.Deleted(selectedGrid) ||
            grids.All(grid => grid.Owner != selectedGrid))
        {
            _selectedGrid = grids.OrderBy(grid => grid.Owner.Id).First().Owner;
        }

        var map = _entityManager.System<SharedMapSystem>();
        foreach (var grid in grids.OrderBy(grid => grid.Owner.Id))
        {
            var center = map.LocalToWorld(grid.Owner, grid.Comp, grid.Comp.LocalAABB.Center);
            var netEntity = _entityManager.GetNetEntity(grid.Owner);
            var gridId = netEntity is { } net
                ? net.Id.ToString(CultureInfo.InvariantCulture)
                : grid.Owner.ToString();
            var name = _entityManager.TryGetComponent(grid.Owner, out MetaDataComponent? meta) &&
                       !string.IsNullOrWhiteSpace(meta.EntityName)
                ? meta.EntityName
                : Loc.GetString("mappingui-grid-unnamed");
            var button = new Button
            {
                Text = $"{gridId} {name}",
                ClipText = true,
                StyleClasses = { "ButtonSquare" },
                ToolTip = $"{center.X:0.##}, {center.Y:0.##}",
                ToggleMode = true,
                Pressed = _selectedGrid == grid.Owner
            };
            var gridUid = grid.Owner;
            button.OnPressed += _ =>
            {
                _selectedGrid = gridUid;
                RefreshGridInfo();
                TeleportToGridCenter(gridUid);
                RefreshGridList(true);
            };
            Screen.GridList.AddChild(button);
        }

        RefreshGridInfo();
    }

    private void TeleportToGridCenter(EntityUid grid)
    {
        if (!_entityManager.TryGetNetEntity(grid, out var netEntity))
            return;

        _console.RemoteExecuteCommand(null, $"mappinguigridtp {netEntity.Value.Id}");
    }
    // open-space edit end

    public EntityUid? GetHoveredEntity()
    {
        if (UserInterfaceManager.CurrentlyHovered is not IViewportControl viewport ||
            _input.MouseScreenPosition is not { IsValid: true } position)
        {
            return null;
        }

        var mapPos = viewport.PixelToMap(position.Position);
        return GetClickedEntity(mapPos);
    }

    public override void FrameUpdate(FrameEventArgs e)
    {
        // open-space edit start
        RefreshHoveredEntityInfo();
        RefreshGridList();
        RefreshGridInfo();
        // open-space edit end

        if (_updatePlacement)
        {
            _updatePlacement = false;

            if (!_placement.IsActive && _decal.GetActiveDecal().Decal == null)
                Deselect();

            Screen.EraseEntityButton.Pressed = _placement.Eraser;
            Screen.EraseDecalButton.Pressed = _updateEraseDecal;
            Screen.EntityPlacementMode.Disabled = _placement.Eraser;
        }

        if (_scrollTo is not { } scrollTo)
            return;

        // this is not ideal but we wait until the control's height is computed to use
        // its position to scroll to
        if (scrollTo.Height > 0 && Screen.Prototypes.PrototypeList.Visible)
        {
            var y = scrollTo.GlobalPosition.Y - Screen.Prototypes.ScrollContainer.Height / 2 + scrollTo.Height;
            var scroll = Screen.Prototypes.ScrollContainer;
            scroll.SetScrollValue(scroll.GetScrollValue() + new Vector2(0, y));
            _scrollTo = null;
        }
    }


    // TODO this doesn't handle pressing down multiple state hotkeys at the moment
    public enum CursorState
    {
        None,
        Pick,
        Delete
    }

    // open-space edit start
    private enum MappingPaletteMode
    {
        Tiles,
        Entities
    }

    private enum MappingPaletteFilter
    {
        None,
        Cables,
        Airlocks,
        Pipes,
        Disposal
    }

    private enum MappingQuickPaletteKind
    {
        Entity,
        Tile,
        Delete
    }

    private readonly record struct QuickPaletteEntry(MappingQuickPaletteKind Kind, string Id)
    {
        public static QuickPaletteEntry Entity(EntProtoId id) => new(MappingQuickPaletteKind.Entity, id);
        public static QuickPaletteEntry Tile(ProtoId<ContentTileDefinition> id) => new(MappingQuickPaletteKind.Tile, id);
        public static QuickPaletteEntry Delete(string id) => new(MappingQuickPaletteKind.Delete, id);
    }
    // open-space edit end
}