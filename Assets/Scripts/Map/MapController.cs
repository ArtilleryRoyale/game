using UnityEngine;
using Jrmgx.Helpers;
using UnityEngine.U2D;
using System;
using CC;
using System.Collections.Generic;
using CC.StreamPlay;
using Cysharp.Threading.Tasks;
using System.Linq;
using PathBerserker2d;
using FMODUnity;

public partial class MapController : NetworkObject, ExplosionReceiver, RoundLockIdentifier
{
    #region Fields

    public static int CurrentSortingLayer = 1000;
    [Flags] public enum MapStyle { None = 0, Jungle = 1, Rocks = 2, Fire = 4, Ice = 8, Mossy = 16 }

    [Header("Style")]
    public MapStyle Style = MapStyle.Jungle;

    [Header("Prefabs")]
    [SerializeField] private TerrainController terrainPrefab = default;
    [SerializeField] private StampController explosionStampPrefab = default;
    [SerializeField] private StampController dashStampPrefab = default;
    [SerializeField] private MapPlatformController mapPlatformPrefab = default;
    [SerializeField] private MapThroneController mapThronePrefab = default;
    [SerializeField] private List<MapDestructibleItemController> mapDestructibleItemPrefabsAll = default;
    private readonly List<MapDestructibleItemController> mapDestructibleItemPrefabs = new List<MapDestructibleItemController>();
    [SerializeField] private MapItemMineController mapItemMinePrefab = default;
    [SerializeField] private MapBeamController mapBeamPrefab = default;
    [SerializeField] private List<LiquidController> liquidPrefabs = default;
    [SerializeField] private List<BackgroundController> backgroundPrefabs = default;

    [Header("References")]
    [SerializeField] private GameObject optionUIContainer = default;
    [SerializeField] private GameObject loadingMapText = default;
    [SerializeField] private GameObject nextMapButton = default;
    [SerializeField] private GameObject validateButton = default;
    [SerializeField] private GameObject duplicableContainer = default;
    [SerializeField] private GameObject terrainsContainer = default;
    [SerializeField] private GameObject objectsContainer = default;
    [SerializeField] private GameObject platformsContainer = default;
    [SerializeField] private GameObject testerContainer = default;
    [SerializeField] private GameObject duplicatedContainer = default;
    [SerializeField] private ParticleSystemForceField particleSystemForceField = default;
    [SerializeField] private LineRenderer dottedLineRendererPrefab = default;
    [SerializeField] private GameObject lineRendererContainer = default;
    public LiquidController LiquidController { get; private set; }
    public BackgroundController BackgroundController { get; private set; }

    [Header("Map Option")]
    [SerializeField] private MultiChoiceEntryUIController gameOptionPlatformCountChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController gameOptionDestructibleObjectCountChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController mapOptionSmoothnessChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController gameOptionWithFloorChoiceEntry = default;
    [SerializeField] private MultiChoiceEntryUIController mapOptionAlgoTypeChoiceEntry = default;
    [SerializeField] private Camera optionCameraMapPreview = default;

    [Header("Sounds")]
    [SerializeField] private StudioEventEmitter soundEventNext = default;
    [SerializeField] private StudioEventEmitter soundEventSelect = default;

    [Header("Config")]
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 20;
    public const float AIJumpDistance = 9f;

    [Header("Random Walk")]
    [SerializeField] private bool algoRandomWalkActive = true;
    [SerializeField] private int randomWalkMinSectionWidth = 4;
    [SerializeField] private int randomWalkMTrimBottom = 4;

    [Header("Cellular")]
    [SerializeField] private bool algoCellularActive = true;
    [Range(0, 100)] [SerializeField] private int cellularFillPercent = 50;
    [SerializeField] private int cellularSmoothCount = 5;

    [Header("Perlin")]
    [SerializeField] private bool algoPerlinActive = false;
    [SerializeField] private int perlinMinSectionWidth = 10;

    [Header("Combinatory")]
    [SerializeField] private bool algoCombinatory3Active = false;
    [SerializeField] private bool algoCombinatory5Active = false;

    [Header("Common")]
    [SerializeField] private bool smoothEdge = true;
    [SerializeField] private int globalMultiplyer = 3;

    [Header("Predefined")]
    [SerializeField] private bool isPredefined = false;

    // Events
    public bool IsStarted { get; private set; } = false;
    public bool IsReady { get; private set; } = false;
    public bool IsBuildFirstPassDone { get; private set; } = false;

    public int RoundLockIdentifier => RoundLock.IdentifierMapController;

    // State
    public int CalculatedWidth => width * globalMultiplyer;
    public int CalculatedHeight => height * globalMultiplyer;
    private enum State { None, GenerateCandidate, BuildingFirstPass, BuildFirstPassDone, BuildingSecondPass, Ready }
    private State state = State.None;
    private List<PolygonCollider2D> currentColliders = new List<PolygonCollider2D>();
    readonly private List<int> mapDestructibleItemPrefabsSelectedIndexes = new List<int>();
    readonly private List<int> mapDecorativeItemPrefabsSelectedIndexes = new List<int>();
    private int globalTries;
    private int terrainCount;
    private List<Graph> currentGraphs = new List<Graph>(); // For AI pathfinding

    // Config
    private const int ExplosionSegments = 24;
    private const float ExplosionColliderPercent = 0.7f;
    private const float ExplosionStampPercent = 0.75f;
    private const float DashStampPercent = 0.6f;

    protected GameOption options => GameManager.Instance.CurrentGameOption;

    #endregion

    #region Init

    protected override void Awake()
    {
        base.Awake();
        // Log.Message("InitDance", "Map Awake");
        CurrentSortingLayer = 1000;
        NetworkIdentifier = NETWORK_IDENTIFIER_MAP_CONTROLLER;

        if (isPredefined) {
            DestroyOptionUI();
        } else {
            optionUIContainer.gameObject.SetActive(true);
            optionCameraMapPreview.gameObject.SetActive(true);
        }
    }

    protected override void Start()
    {
        base.Start();

        // Log.Message("InitDance", "Map Start");
        SetOptionToUserInterface();
        optionCameraMapPreview.aspect = 16f / 9f;
        IsStarted = true;
    }

    #endregion

    #region Options

    protected void SetOptionToUserInterface()
    {
        if (isPredefined) return;
        gameOptionPlatformCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(options.PlatformCount));
        gameOptionDestructibleObjectCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(options.DestructibleObjectCount));
        gameOptionWithFloorChoiceEntry.SetValue(options.WithFloor ? "Yes" : "No");
        ApplyOptions();

        if (IsNetwork && !IsNetworkOwner) { // Set User Interface to read only for Network Guest
            nextMapButton.SetActive(false);
            validateButton.SetActive(false);
            gameOptionPlatformCountChoiceEntry.SetReadOnly(true);
            gameOptionDestructibleObjectCountChoiceEntry.SetReadOnly(true);
            gameOptionWithFloorChoiceEntry.SetReadOnly(true);
            mapOptionAlgoTypeChoiceEntry.SetReadOnly(true);
            mapOptionSmoothnessChoiceEntry.SetReadOnly(true);
        }
    }

    protected void ApplyOptions()
    {
        // Get UI values and put them in CurrentGameOption / variables
        options.PlatformCount = GameOption.ParseIntAndSpecific(gameOptionPlatformCountChoiceEntry.GetValue());
        options.DestructibleObjectCount = GameOption.ParseIntAndSpecific(gameOptionDestructibleObjectCountChoiceEntry.GetValue());
        options.WithFloor = gameOptionWithFloorChoiceEntry.GetValue() == "Yes";
        var smoothness = mapOptionSmoothnessChoiceEntry.GetValue();

        // Apply to actual objects
        algoRandomWalkActive = options.WithFloor;

        // Send over the network
        NetworkRecordSnapshotInstant(Method_UpdateOption, options, smoothness);
    }

    [StreamPlay(Method_UpdateOption)]
    protected void UpdateOption(SzGameOptionRef gameOptionRef, string smoothness)
    {
        // Similar to ApplyOptions()
        gameOptionRef.InjectIn(options);

        // Similar to SetOptionToUserInterface()
        gameOptionPlatformCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(options.PlatformCount));
        gameOptionDestructibleObjectCountChoiceEntry.SetValue(GameOption.ToStringAndSpecific(options.DestructibleObjectCount));
        gameOptionWithFloorChoiceEntry.SetValue(options.WithFloor ? "Yes" : "No");
        mapOptionSmoothnessChoiceEntry.SetValue(smoothness);
    }

    public void OptionSmoothnessAction(string value)
    {
        // Smooth / Medium / Rough
        switch (value) {
            case "Smooth":
                randomWalkMinSectionWidth = 5;
                cellularSmoothCount = 3;
                break;
            case "Medium":
                randomWalkMinSectionWidth = 3;
                cellularSmoothCount = 2;
                break;
            case "Rough":
                randomWalkMinSectionWidth = 1;
                cellularSmoothCount = 1;
                break;
        }
        UpdateOptionAndGenerateAction();
    }

    public void OptionAlgoTypeAction(string value)
    {
        // Noise / Geometric
        UpdateOptionAndGenerateAction();
    }

    public void UpdateOptionAction()
    {
        ApplyOptions();
    }

    public void UpdateOptionAndGenerateAction()
    {
        ApplyOptions();
        GenerateCandidate();
    }

    [StreamPlay(Method_LoadStyle)]
    protected void LoadStyle(int mapStyle)
    {
        Style = (MapController.MapStyle) mapStyle;

        foreach (var itemPrefab in mapDestructibleItemPrefabsAll) {
            if (!itemPrefab.Style.HasFlag(Style)) continue;
            mapDestructibleItemPrefabs.Add(itemPrefab);
        }

        foreach (var liquidPrefab in liquidPrefabs) {
            if (liquidPrefab.Style.HasFlag(Style)) {
                LiquidController = Instantiate(liquidPrefab);
                break;
            }
        }

        foreach (var backgroundPrefab in backgroundPrefabs) {
            if (backgroundPrefab.Style.HasFlag(Style)) {
                BackgroundController = Instantiate(backgroundPrefab);
                break;
            }
        }
    }

    #endregion

    #region State

    public void StateGenerateCandidate()
    {
        // Log.Message("InitDance", "Map StateGenerateCandidate");
        LoadStyle((int) options.MapStyle);
        NetworkRecordSnapshot(Method_LoadStyle, (int) options.MapStyle);

        if (isPredefined) return;

        state = State.GenerateCandidate;
        GenerateCandidate();
    }

    public async UniTask StateFirstPass()
    {
        // Log.Message("InitDance", "Map StateFirstPass");
        state = State.BuildingFirstPass;
#if CC_EXTRA_CARE
try {
#endif
        await BuildFirstPass().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    public async UniTask StateSecondPass()
    {
        // Log.Message("InitDance", "Map StateSecondPass");
        state = State.BuildingSecondPass;
#if CC_EXTRA_CARE
try {
#endif
        await BuildSecondPass().CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    #endregion

    #region Actions

    public void NextAction()
    {
        if (PopupManager.HasFocus) return;
        if (state != State.GenerateCandidate) return;

        soundEventNext.Play();
        GenerateCandidate();
    }

    public async void SelectAction() // Action from button, not using UniTask here
    {
        if (PopupManager.HasFocus) return;
        if (state != State.GenerateCandidate) return;

        //soundEventSelect.Play();
#if CC_EXTRA_CARE
try {
#endif
        try {
            await StateFirstPass().CancelOnDestroy(this);
        } catch (System.OperationCanceledException) { /* We must handle manually the cancellation because we are not into a UniTask method */ return; }
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    #endregion

    #region Generator

    private void GenerateCandidate()
    {
        // Log.Message("InitDance", "Map GenerateCandidate");
        state = State.GenerateCandidate;

        int tries = 0;
        int maxTries = 100;
        while (++tries < maxTries) {

            RemoveTerrain();
            RemoveTester();

            Paths currentPaths = new Paths();
            try {
                if (algoCombinatory3Active) {
                    currentPaths = currentPaths.AddPaths(MapCombineGeneration.GenerateMapSqrt3Circle(
                        width * globalMultiplyer,
                        height * globalMultiplyer
                    ));
                }

                if (algoCombinatory5Active) {
                    currentPaths = currentPaths.AddPaths(MapCombineGeneration.GenerateMapSqrt5Circle2(
                        width * globalMultiplyer,
                        height * globalMultiplyer
                    ));
                }

                if (algoRandomWalkActive) {
                    currentPaths = currentPaths.AddPaths(Paths.GetPaths(
                        BitmapGeneration.Trim(
                            BitmapGeneration.RandomWalk(
                                width, height,
                                randomWalkMinSectionWidth),
                            randomWalkMTrimBottom),
                        globalMultiplyer)
                    );
                }

                if (algoCellularActive) {
                    currentPaths = currentPaths.AddPaths(Paths.GetPaths(
                        BitmapGeneration.Cellular(
                            width, height,
                            cellularFillPercent,
                            cellularSmoothCount),
                        globalMultiplyer
                    ));
                }

                if (algoPerlinActive) {
                    currentPaths = currentPaths.AddPaths(Paths.GetPaths(
                        BitmapGeneration.Perlin(
                            width, height,
                            perlinMinSectionWidth),
                        globalMultiplyer
                    ));
                }
            } catch (PathsOperationException e) {
                Log.Error("MapController", e.Message);
                continue;
            }

            RemoveInsidePaths(ref currentPaths);
            if (!IsCandidateCrash(ref currentPaths)) {
                BuildFromData(currentPaths, (int) Style);
                NetworkRecordSnapshotInstant(Method_BuildFromData, currentPaths, (int) Style);
                return; // Success
            }
        }

        Log.Critical("MapController", "Impossible to generate a map with those settings.");

        state = State.GenerateCandidate;
    }

    /// <summary>
    /// We need to try if our edges won't crash the Sprite Shape Controller
    /// </summary>
    private bool IsCandidateCrash(ref Paths paths)
    {
        bool crash = false;
        try {
            BuildTerrain(paths, Style, tester: true).Forget();
        } catch (Exception e) {
            Log.Error("MapController", "Unvalid map when given to terrain tester: " + e);
            crash = true;
        }

        RemoveTester();
        return crash;
    }

    private void RemoveInsidePaths(ref Paths paths)
    {
        foreach (var pathOuter in paths) {
            foreach (var pathInner in paths) {
                if (pathInner.Count == 0 || pathOuter.Count == 0 || pathInner == pathOuter) continue;
                if (pathInner.Contains(pathOuter)) {
                    // Log.Message("MapController", "Some path contains a smaller path, removing");
                    pathOuter.Clear();
                } else if (pathOuter.Contains(pathInner)) {
                    // Log.Message("MapController", "Some path contains a smaller path, removing");
                    pathInner.Clear();
                }
            }
        }
    }

    #endregion

    #region Builder

    [StreamPlay(Method_BuildFromData)]
    protected void BuildFromData(Paths paths, int mapStyle)
    {
        // Log.Message("InitDance", "Map BuildFromData");
        loadingMapText.SetActive(false);
        RemoveTerrain();
        RemoveTester();
        BuildTerrain(paths, (MapStyle) mapStyle).Forget();
    }

    private async UniTask BuildTerrain(Paths paths, MapStyle mapStyle, bool tester = false)
    {
        foreach (Path path in paths) {

            if (path.Count == 0) continue;

            terrainCount++;
            TerrainController terrain = Instantiate(
                terrainPrefab,
                tester ? testerContainer.transform : terrainsContainer.transform
            );
            terrain.name += " " + terrainCount;

            terrain.frontTerrain.spline.Clear();
            terrain.backTerrain.spline.Clear();
            terrain.SetStyle(mapStyle);

            var points = path.ToListVector2();
            for (int i = 0, max = points.Count; i < max; i++) {
                var point = points[max - i - 1];
                // TODO prio 3 use the 3 different border sprites
                terrain.frontTerrain.spline.InsertPointAt(i, new Vector2(point.x, point.y));
                terrain.backTerrain.spline.InsertPointAt(i, new Vector2(point.x, point.y));
            }
            if (smoothEdge) {
                for (int i = 0, max = terrain.frontTerrain.spline.GetPointCount(); i < max; i++) {
                    terrain.frontTerrain.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                    terrain.backTerrain.spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                    MapHelpers.Smoothen(terrain.frontTerrain, i);
                    MapHelpers.Smoothen(terrain.backTerrain, i);
                }
            }
        }
        // Wait a frame so polygonColliders attached to spriteShapeController can update
        await UniTask.WaitForEndOfFrame();
    }

    public GameObject GetDuplicable()
    {
        return duplicableContainer;
    }

    private void MakeDuplicate()
    {
        var duplicated = Instantiate(duplicableContainer, duplicatedContainer.transform);

        // Change mask interaction mode
        foreach (SpriteRenderer spriteRenderer in duplicated.GetComponentsInChildren<SpriteRenderer>()) {
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
            spriteRenderer.color = Config.MapOutlineColor;
        }

        foreach (SpriteShapeRenderer spriteShapeRenderer in duplicated.GetComponentsInChildren<SpriteShapeRenderer>()) {
            spriteShapeRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            spriteShapeRenderer.color = Config.MapOutlineColor;
            var terrain = spriteShapeRenderer.gameObject.GetComponent<TerrainController>();
            if (terrain != null) {
                terrain.SetDuplicate(Style);
            }
        }

        // Deactivate colliders
        foreach (PolygonCollider2D polygonCollider in duplicated.GetComponentsInChildren<PolygonCollider2D>()) {
            polygonCollider.enabled = false;
        }
    }

    private void DestroyOptionUI()
    {
        Destroy(optionUIContainer.gameObject);
        Destroy(optionCameraMapPreview.gameObject);
    }

    /// <summary>
    /// First pass: building the map basic elements
    /// This comes before character positioning
    /// </summary>
    private async UniTask BuildFirstPass()
    {
        // Log.Message("InitDance", "Map BuildFirstPass Owner");
        DestroyOptionUI();

        RemoveTester();
        CurrentCollidersAdd(terrainsContainer.GetComponentsInChildren<PolygonCollider2D>());

        if (!isPredefined) {
            UserInterfaceManager.Instance.UpdateInfoText("Positioning Platforms...");
#if CC_EXTRA_CARE
try {
#endif
            await PositionPlatforms(GameManager.Instance.CurrentGameOption.PlatformCount).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            PositionThrones();

            UserInterfaceManager.Instance.UpdateInfoText("Positioning Objects...");
#if CC_EXTRA_CARE
try {
#endif
            await PositionDestructibleObjects(GameManager.Instance.CurrentGameOption.DestructibleObjectCount).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
        }

        UserInterfaceManager.Instance.HideInfoText();
        UpdatePlatformsAndThrones();
        UpdateObjects();
        UpdateTerrains();

        NetworkRecordSnapshotInstant(Method_BuildFirstPass);

        MakeDuplicate();

        state = State.BuildFirstPassDone;
        // Log.Message("InitDance", "Map IsBuildFirstPassDone");
        IsBuildFirstPassDone = true;
    }

    [StreamPlay(Method_BuildFirstPass)]
    protected void BuildFirstPass_Guest()
    {
        // Log.Message("InitDance", "Map BuildFirstPass Guest");
        DestroyOptionUI();

        RemoveTester();
        CurrentCollidersAdd(terrainsContainer.GetComponentsInChildren<PolygonCollider2D>());

        UpdatePlatformsAndThrones();
        UpdateObjects();
        UpdateTerrains();

        MakeDuplicate();

        StreamPlayPlayer.Refresh();

        state = State.BuildFirstPassDone;
        // Log.Message("InitDance", "Map IsBuildFirstPassDone");
        IsBuildFirstPassDone = true;
    }

    /// <summary>
    /// Second pass: adding optional map elements
    /// This comes after character positioning
    /// </summary>
    /// <returns></returns>
    private async UniTask BuildSecondPass()
    {
        // Log.Message("InitDance", "Map BuildSecondPass Owner");
        if (!isPredefined) {
#if CC_EXTRA_CARE
try {
#endif
            await PositionMine(GameManager.Instance.CurrentGameOption.MineCount).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            // Log.Message("MapController", "Positioning global tries: " + globalTries);
        }
        BuildSecondPass_Common();
        NetworkRecordSnapshotInstant(Method_BuildSecondPass);
    }

    [StreamPlay(Method_BuildSecondPass)]
    protected void BuildSecondPass_Common()
    {
        // Log.Message("InitDance", "Map BuildSecondPass Common");
        RemovePositioning();
        if (!isPredefined) {
            CameraManager.Instance.ResetCamera(); // was zoomed out to see the map generation
        }
        state = State.Ready;
        // Log.Message("InitDance", "Map IsReady");
        IsReady = true;
    }

    #endregion

    #region Map Items

    private void PositionThrones()
    {
        if (!GameManager.Instance.CurrentGameOption.PlayingCaptureTheKing) return;

        var thrones = new List<MapItemObjectSerializable>();

        if (GameManager.Instance.CurrentGameOption.StartWithOrChallenger == GameOption.TeamEnum.Blue_Team) {
            // Red
            MapThroneController throne = Instantiate(mapThronePrefab, platformsContainer.transform);
            bool redThrone = true;
            throne.SetPlayerOneThrone(redThrone);
            Vector2 position = new Vector2(0 + PlayerController.ThronePlateformOffset.x, CalculatedHeight + PlayerController.ThronePlateformOffset.y);
            throne.SetPosition(position);
            thrones.Add(new MapItemObjectSerializable(0, position, redThrone));
            CurrentCollidersAdd(throne.GetComponentsInChildren<PolygonCollider2D>());
        } else {
            // Blue
            MapThroneController throne = Instantiate(mapThronePrefab, platformsContainer.transform);
            bool redThrone = false;
            throne.SetPlayerOneThrone(redThrone);
            Vector2 position = new Vector2(CalculatedWidth - PlayerController.ThronePlateformOffset.x, CalculatedHeight + PlayerController.ThronePlateformOffset.y);
            throne.SetPosition(position);
            thrones.Add(new MapItemObjectSerializable(0, position, redThrone));
            CurrentCollidersAdd(throne.GetComponentsInChildren<PolygonCollider2D>());
        }

        NetworkRecordSnapshotInstant(Method_PositionThrones, thrones);
    }

    [StreamPlay(Method_PositionThrones)]
    protected void PositionThrones_Guest(List<MapItemObjectSerializable> items)
    {
        foreach (MapItemObjectSerializable item in items) {
            MapThroneController throne = Instantiate(mapThronePrefab, platformsContainer.transform);
            throne.SetPlayerOneThrone(item.IsPlayerOne());
            throne.SetPosition(item.Position());
            CurrentCollidersAdd(throne.GetComponentsInChildren<PolygonCollider2D>());
        }
    }

    private async UniTask PositionPlatforms(int number)
    {
        var platforms = new List<MapItemObjectSerializable>();
        for (int i = 0; i < number; i++) {
            MapPlatformController platform = Instantiate(mapPlatformPrefab, platformsContainer.transform);
            bool symmetry = RandomNum.RandomOneOutOf(2);
            platform.SetSymmetry(symmetry);
            Vector2? position = null;
#if CC_EXTRA_CARE
try {
#endif
            position = await PositionPlatform(platform, inPartHorizontal: (i % number) + 1, ofPartsHorizontal: number).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            if (position.HasValue) {
                platform.SetPosition(position.Value);
                platforms.Add(new MapItemObjectSerializable(0, position.Value, symmetry));
                CurrentCollidersAdd(platform.GetComponentsInChildren<PolygonCollider2D>());
            }
        }

        NetworkRecordSnapshotInstant(Method_PositionPlatforms, platforms);
    }

    [StreamPlay(Method_PositionPlatforms)]
    protected void PositionPlatforms_Guest(List<MapItemObjectSerializable> items)
    {
        foreach (MapItemObjectSerializable item in items) {
            MapPlatformController platform = Instantiate(mapPlatformPrefab, platformsContainer.transform);
            platform.SetSymmetry(item.Symmetry());
            platform.SetPosition(item.Position());
            CurrentCollidersAdd(platform.GetComponentsInChildren<PolygonCollider2D>());
        }
    }

    private async UniTask PositionDestructibleObjects(int number)
    {
        if (mapDestructibleItemPrefabs.Count <= 0) return;
        var destructibleObjects = new List<MapItemObjectSerializable>();
        for (int i = 0; i < number; i++) {
            int index = UnityEngine.Random.Range(0, mapDestructibleItemPrefabs.Count);
            /* Try to get different object type */ {
                if (mapDestructibleItemPrefabsSelectedIndexes.Count < mapDestructibleItemPrefabs.Count) {
                    while (mapDestructibleItemPrefabsSelectedIndexes.Contains(index)) {
                        index = UnityEngine.Random.Range(0, mapDestructibleItemPrefabs.Count);
                    }
                }
                mapDestructibleItemPrefabsSelectedIndexes.Add(index);
            }
            MapDestructibleItemController destructible = Instantiate(mapDestructibleItemPrefabs[index], objectsContainer.transform);
            bool symmetry = RandomNum.RandomOneOutOf(2);
            destructible.SetSymmetry(symmetry);
            Vector2? position = null;
#if CC_EXTRA_CARE
try {
#endif
            position = await PositionObject(destructible, (i % number) + 1, ofPartsHorizontal: number).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            if (position.HasValue) {
                destructible.SetPosition(position.Value);
                destructibleObjects.Add(new MapItemObjectSerializable(index, position.Value, symmetry));
                CurrentCollidersAdd(destructible.GetComponentsInChildren<PolygonCollider2D>());
            }
        }

        NetworkRecordSnapshotInstant(Method_PositionDestructibleObjects, destructibleObjects);
    }

    [StreamPlay(Method_PositionDestructibleObjects)]
    protected void PositionDestructibleObjects_Guest(List<MapItemObjectSerializable> items)
    {
        foreach (MapItemObjectSerializable item in items) {
            MapDestructibleItemController destructible = Instantiate(mapDestructibleItemPrefabs[item.Index()], objectsContainer.transform);
            destructible.SetSymmetry(item.Symmetry());
            destructible.SetPosition(item.Position());
            CurrentCollidersAdd(destructible.GetComponentsInChildren<PolygonCollider2D>());
        }
    }

    private async UniTask PositionMine(int number)
    {
        var mines = new List<MapItemObjectSerializable>();
        var identifiers = new List<int>();
        for (int i = 0; i < number; i++) {
            MapItemMineController mine = NetworkInstantiate(mapItemMinePrefab);
            mine.transform.SetParent(transform);
            Vector2? position = null;
            var positioningConstraints = PositioningConstraints.InOfConstraint((i % number) + 1, number);
#if CC_EXTRA_CARE
try {
#endif
            position = await PositionOnMap(mine, 5, 55f, positioningConstraints, canFail: true).CancelOnDestroy(this);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
            if (position.HasValue) {
                mine.SetPosition(position.Value);
                mines.Add(new MapItemObjectSerializable(0, position.Value, false));
                identifiers.Add(mine.NetworkIdentifier);
            }
        }

        NetworkRecordSnapshotInstant(Method_PositionMine, identifiers, mines);
    }

    [StreamPlay(Method_PositionMine)]
    protected void PositionMine_Guest(List<int> ownerNetworkIdentifiers, List<MapItemObjectSerializable> items)
    {
        // for loop used to get index in ownerNetworkIdentifiers
        for (int i = 0, max = items.Count; i < max; i++) {
            MapItemObjectSerializable item = items[i];
            int ownerNetworkIdentifier = ownerNetworkIdentifiers[i];
            MapItemMineController mine = NetworkInstantiate(mapItemMinePrefab, ownerNetworkIdentifier);
            mine.transform.SetParent(transform);
            mine.SetPosition(item.Position());
        }
    }

    /// <summary>
    /// Upgrade platform layer from "Platform" to "Terrain"
    /// It was on a specific layer to prevent object to find it
    /// but now we want it to be part of the whole map
    /// </summary>
    private void UpdatePlatformsAndThrones()
    {
        foreach (PolygonCollider2D polygonCollider in platformsContainer.GetComponentsInChildren<PolygonCollider2D>()) {
            polygonCollider.gameObject.layer = Layer.Terrain.Index;
        }
    }

    private void UpdateObjects()
    {
        foreach (PolygonCollider2D polygonCollider in objectsContainer.GetComponentsInChildren<PolygonCollider2D>()) {
            polygonCollider.gameObject.layer = Layer.Terrain.Index;
        }
    }

    private void UpdateTerrains()
    {
        foreach (
            SpriteShapeController spriteShapeController in
            duplicableContainer.GetComponentsInChildren<SpriteShapeController>())
        {
            spriteShapeController.autoUpdateCollider = false;
        }
    }

    public void RemovePositioning()
    {
        foreach (var o in GameObject.FindGameObjectsWithTag(Config.TagPositioning)) {
            Destroy(o);
        }
    }

    private void RemovePlatforms()
    {
        platformsContainer.transform.RemoveAllChildren(immediate: true);
    }

    private void RemoveTerrain()
    {
        terrainsContainer.transform.RemoveAllChildren(immediate: true);
    }

    private void RemoveTester()
    {
        testerContainer.transform.RemoveAllChildren(immediate: true);
    }

    #endregion

    #region Destructible

    public void OnReceiveExplosion(ExplosionController explosion, Vector2 force, int damages)
    {
        if (!GameManager.Instance.CurrentGameOption.MapDestructible) return;
        Vector2 origin = explosion.transform.position;
        float radius = explosion.RadiusMax;

#if CC_EXTRA_CARE
try {
#endif
        Explosion(origin, radius).Forget();
        NetworkRecordSnapshot(Method_Explosion, origin, radius);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    public void OnReceiveDash(Vector2 origin, Vector2 direction, float distance, float weight, float duration)
    {
#if CC_EXTRA_CARE
try {
#endif
        Dash(origin, direction, distance, weight, duration).Forget();
        NetworkRecordSnapshot(Method_Dash, origin, direction, distance, weight, duration);
#if CC_EXTRA_CARE
} catch (System.Exception cc_exception) when (!(cc_exception is System.OperationCanceledException)) { Log.Critical("CC_EXTRA_CARE", "CC_EXTRA_CARE Exception: " + cc_exception); return; }
#endif
    }

    [StreamPlay(Method_Explosion)]
    protected async UniTask Explosion(Vector2 origin, float radius)
    {
        foreach (PolygonCollider2D currentCollider in currentColliders) {

            // Get values for collider space
            Vector2 localOrigin = currentCollider.transform.InverseTransformPoint(origin);
            float localRadius = radius * ExplosionColliderPercent / currentCollider.transform.localScale.x;

            Paths resultPaths;
            Paths explosionPaths = Paths.GetPaths(Path.MakeCirclePath(localOrigin, localRadius, ExplosionSegments).Simplify());

            Paths colliderPaths = Paths.GetPaths(currentCollider).Simplify();
            try {

                // Work with shields
                foreach (var shieldPosition in ShieldPositions()) {
                    Vector2 shieldLocalOrigin = currentCollider.transform.InverseTransformPoint(shieldPosition + Vector2.up * ShieldAmmoController.SHIELD_OFFSET);
                    float shieldLocalRadius = ShieldAmmoController.SHIELD_SIZE / currentCollider.transform.localScale.x;
                    Path shieldPath = Path.MakeCirclePath(shieldLocalOrigin, shieldLocalRadius, ExplosionSegments).Simplify();
                    explosionPaths = explosionPaths.SubstractPath(shieldPath);
                }

                resultPaths = colliderPaths.SubstractPaths(explosionPaths).Simplify();
                resultPaths.ApplyToCollider(currentCollider);
            } catch {
                Log.Critical("MapController", "Path clipping between " + currentCollider.name + " and explosion failed!");
            }
        }

        // Wait for the explosion animation before stamping
        await UniTask.Delay(200 /* ms */, cancellationToken: this.GetCancellationTokenOnDestroy());

        var networkId = NetworkIdentifierNew();
        ExplosionStamp(networkId, origin, radius);
        NetworkRecordSnapshot(Method_ExplosionStamp, networkId, origin, radius);
    }

    [StreamPlay(Method_ExplosionStamp)]
    protected void ExplosionStamp(int networkId, Vector2 origin, float radius)
    {
        StampController explosionStamp = NetworkInstantiate(explosionStampPrefab, networkId);
        explosionStamp.Sort(CurrentSortingLayer);
        explosionStamp.transform.SetParent(transform);
        explosionStamp.transform.position = origin;

        float frontRadius = radius * ExplosionStampPercent;
        float backRadius = frontRadius * Config.MapOutlineInPercent;

        // Work with shields
        foreach (var shieldPosition in ShieldPositions()) {
            var vb = Basics.VectorBetween(origin, shieldPosition);

            explosionStamp.frontMask.sprite = ExplosionManager.Instance.StampMakerController.ShieldStampedStampSprite(explosionStamp.frontMask.sprite, vb, frontRadius, 1f);

            // TODO prio 3 (quite hard) show a border on the back stamp part
            var ratio = backRadius / frontRadius;

            explosionStamp.backMask.sprite = ExplosionManager.Instance.StampMakerController.ShieldStampedStampSprite(explosionStamp.frontMask.sprite, vb, backRadius, 1f);
        }

        explosionStamp.frontMask.transform.localScale = Vector3.one * frontRadius;
        explosionStamp.backMask.transform.localScale = Vector3.one * backRadius;
    }

    [StreamPlay(Method_Dash)]
    protected async UniTask Dash(Vector2 origin, Vector2 direction, float distance, float weight, float duration)
    {
        direction = direction.normalized;
        origin -= direction * (Vector2.one * 1f);

        // This code will make a rectangle of width = 1 and height = distance from origin, pointing in direction
        // Note: the origin with be in the middle of the bottom of the rectangle
        var normal = new Vector2(-direction.y , direction.x);
        var halfWeight = weight / 2f;
        var bottomLeft = normal * halfWeight;
        var bottomRight = -normal * halfWeight;
        var topLeft = bottomLeft + direction * distance;
        var topRight = bottomRight + direction * distance;
        var p1 = bottomLeft + origin;
        var p2 = topLeft + origin;
        var p3 = topRight + origin;
        var p4 = bottomRight + origin;
        var final = ((topLeft + topRight) / 2f) + origin;

        foreach (PolygonCollider2D currentCollider in currentColliders) {

            // Get values for collider space
            var localPoints = new List<Vector2>{
                currentCollider.transform.InverseTransformPoint(p1),
                currentCollider.transform.InverseTransformPoint(p2),
                currentCollider.transform.InverseTransformPoint(p3),
                currentCollider.transform.InverseTransformPoint(p4)
            };

            Paths resultPaths;
            Path dashPath = Path.GetPath(localPoints).Simplify();
            Paths colliderPaths = Paths.GetPaths(currentCollider).Simplify();
            try {
                resultPaths = colliderPaths.SubstractPath(dashPath).Simplify();
                resultPaths.ApplyToCollider(currentCollider);
            } catch {
                Log.Critical("MapController", "Path clipping between " + currentCollider.name + " and dash failed!");
            }
        }

        // Stamping is done step by step through duration
        int steps = 5;
        float distanceStep = distance / steps;
        Vector2 originNext = origin;
        for (var i = 0; i < steps; i++) {
            await UniTask.Delay((int)(duration / (float)(steps * /* speed */ 1.5f) * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());
            var networkId = NetworkIdentifierNew();
            DashStamp(networkId, originNext, direction, distanceStep, weight);
            NetworkRecordSnapshot(Method_DashStamp, networkId, originNext, direction, distanceStep, weight);
            originNext += direction * distanceStep;
        }
    }

    [StreamPlay(Method_DashStamp)]
    protected void DashStamp(int networkId, Vector2 origin, Vector2 direction, float distance, float weight)
    {
        StampController dashStamp = NetworkInstantiate(dashStampPrefab, networkId);
        dashStamp.Sort(CurrentSortingLayer);
        dashStamp.transform.SetParent(transform);
        dashStamp.transform.position = origin;
        dashStamp.transform.localRotation = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);

        dashStamp.frontMask.transform.localScale = new Vector2(distance, weight * DashStampPercent);
        dashStamp.backMask.transform.localScale = new Vector2(distance, weight * DashStampPercent * Config.MapOutlineInPercent);
    }

    #endregion

    #region Shield

    private IEnumerable<Vector2> ShieldPositions()
    {
        return GameManager
            .Instance // TODO prio 6 could optimize or had an appropriate getter
            .PlayerControllers
            .SelectMany(i => i.CharacterManagers)
            .Select(i => i.Shield)
            .Where(i => i != null)
            .Select(i => (Vector2)i.transform.position)
        ;
    }

    #endregion

    #region AI

    public MapController AIRefreshGraphs()
    {
        // Config
        float clearanceHeight = 4.2f;
        float clearanceWidth = 1.6f;
        float raycastElevation = 1f;

        var graphs = new List<Graph>();

        // For each colliders' collider path (most likely one)
        var mask = (Layer.Mine | Layer.Bounds).Mask;
        foreach (PolygonCollider2D collider in currentColliders) {
            foreach (var polygon in ColliderConverter.Convert(collider)) {
                var points = polygon.Hull.Verts;
                graphs.AddRange(Graph.GraphsForPolygon(points, CharacterMove.MaxSlope, clearanceHeight, clearanceWidth, mask));
            }
        }

        var links = Graph.GetLinksBetweenGraphs(graphs, AIJumpDistance, raycastElevation);
        currentGraphs = Graph.MergeByLinks(graphs, links);


        foreach (var g in currentGraphs) {
            //Debug.Log(g);
            Graph.DrawDebugGraph(g, 30f);
        }

        return this;
    }

    public List<GraphPoint> AICalculatePathOnGraph(Vector2 from, Vector2 to, Graph graph)
    {
        var start = new GraphPoint(graph.GetClosestPoint(from));
        var goal = new GraphPoint(graph.GetClosestPoint(to));
        var search = new AStarSearch<GraphPoint>();
        var found = search.Search(graph, start, goal);
        var path = search.GetPath();

        return path;
    }

    public Graph AIGetGraphFor(Vector2 point)
    {
        return Graph.GetClosestGraph(point, currentGraphs);
    }

    /// <summary>
    /// Given a list of points, return the points where the path to go there is the smallest
    /// </summary>
    public Vector2? AIFindSmallestPathTo(IEnumerable<Vector2> points, float distanceMax)
    {
        foreach (var g in currentGraphs) {
            foreach (var p in points) {
                var point = g.PointIsNearGraph(p, distanceMax);
                if (point.HasValue) {
                    return point.Value;
                }
            }
        }
        return null;
    }

    public IEnumerable<Vector2> AIFindBox()
    {
        return FindObjectsOfType<BoxControllerBase>()
            .Select(b => (Vector2)b.transform.position);
    }

    public IEnumerable<Vector2> AIFindEnemies()
    {
        return GameManager.Instance.PlayerHumain.Characters()
            .Where(c => !ShieldAmmoController.IntoShield(c))
            .Select(c => (Vector2)c.transform.position)
        ;
    }

    /// <summary>
    /// Return a positive int if most of my enemies are on my right,
    /// a negative int if most of my enemies are on my left
    /// </summary>
    public int AIMostEnemiesDirection(Vector2 relativeTo)
    {
        return GameManager.Instance.PlayerHumain.Characters()
            // Enemy is on my right: +1 / on my left: -1
            .Select(c => c.transform.position.x > relativeTo.x ? 1 : -1)
            .Sum()
        ;
    }

    public float AIRaycastPossibles(Vector2 origin, Vector2[] directions, float distance)
    {
        float sum = 0f;
        foreach (var direction in directions) {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, Layer.Terrain.Mask);
            // Debugging.DrawLine(origin, hit ? hit.point : origin + distance * direction, Color.green.WithA(0.2f), 5f);
            sum += hit ? hit.distance : distance;
        }
        return sum;
    }

    #endregion

    #region Positioning

    /// <summary>
    /// We need to wait between each placement
    /// so our colliders can be positionned/refreshed by the physics2D engine
    /// </summary>
    private async UniTask WaitForPhysics()
    {
        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken: this.GetCancellationTokenOnDestroy());
    }

    public async UniTask<Vector2?> PositionOnMap(
        PositionableInterface positionable,
        float objectRadius,
        float maxSlope,
        PositioningConstraints positioningConstraints = null,
        bool canFail = false
    )
    {
        var candidates = new List<Vector2>();
        var mask = (Layer.Bounds).Mask;
        foreach (PolygonCollider2D collider in currentColliders) {
            foreach (var polygon in ColliderConverter.Convert(collider)) {
                var points = polygon.Hull.Verts;
                foreach (var g in Graph.GraphsForPolygon(points, maxSlope, objectRadius, objectRadius, mask, objectRadius)) {
                    candidates.AddRange(g.Points);
                }
            }
        }

        if (positioningConstraints == null) {
            positioningConstraints = PositioningConstraints.None;
        }

        float smallestX = MapHelpers.SmallestX(candidates);
        float biggestX = MapHelpers.BiggestX(candidates);
        float amplitudeWidth = biggestX - smallestX;

        await WaitForPhysics().CancelOnDestroy(this);

        int tries = 0;
        int maxTries = width * height / (canFail ? 10 : 1);

        while (++tries < maxTries) try {
            // Every few tries we wait a bit so the interface can show some progress
            if (++globalTries % 20 == 0) {
                await UniTask.WaitForEndOfFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            // Last chances, so we remove/reduce constraints
            if (!canFail && tries > maxTries * 0.75f) {
                Log.Error("MapController", "Positioning tries at 75%+");
                positioningConstraints.RemoveHorizontalConstraints();
                positioningConstraints.RemoveVerticalConstraints();
            }

            List<Vector2> filteredCandidates = candidates;

            // Horizontal constraints
            if (positioningConstraints.OfPartsHorizontal > 1) {

                float partWidth = amplitudeWidth / positioningConstraints.OfPartsHorizontal;
                float currentMinX = smallestX + ((positioningConstraints.InPartHorizontal - 1) * partWidth);
                float currentMaxX = smallestX + (positioningConstraints.InPartHorizontal * partWidth);

                filteredCandidates = filteredCandidates
                    .Where((ii) => ii.x > currentMinX && ii.x < currentMaxX)
                    .ToList();
            }

            // Vertical constraints
            if (positioningConstraints.MinVertical > 0) {
                // Debugging.DrawLine(new Vector2(0, positioningConstraints.MinVertical), new Vector2(width * globalMultiplyer, positioningConstraints.MinVertical), Color.white, 10f);
                var filteredHeightCandidates = filteredCandidates.Where((ii) => ii.y > positioningConstraints.MinVertical);
                if (filteredHeightCandidates.Count() > 0) {
                    filteredCandidates = filteredHeightCandidates.ToList();
                }
            }
            if (positioningConstraints.MaxVertical < height * globalMultiplyer * 1.1f) {
                // Debugging.DrawLine(new Vector2(0, 100), new Vector2(width * globalMultiplyer, 100), Color.red, 10f);
                var filteredHeightCandidates = filteredCandidates.Where((ii) => ii.y < positioningConstraints.MaxVertical);
                if (filteredHeightCandidates.Count() > 0) {
                    filteredCandidates = filteredHeightCandidates.ToList();
                }
            }

            // We get a candidate from the filtered candidates and move up a bit so we can raycast and get a slope
            Vector2 candidate = filteredCandidates.GetRandomValue() + Vector2.up * 0.01f;
            // Debugging.DrawPoint(candidate, Color.cyan, 10f);

            // Raycast a circle and checks if nothing of the mask is in object radius
            var circleCast = Physics2D.CircleCast(candidate, objectRadius, Vector2.down, 0.1f, (Layer.Character | Layer.Mine | Layer.Bounds | Layer.Box).Mask);
            if (circleCast) {
                // Debugging.DrawCircle(candidate, objectRadius, Color.red, 10f);
                // Log.Message("MapController", "Positioning of " + positionable.name + " fail due to collision with " + circleCast.collider.name);
                continue; // try again
            }

            // We also test that we are not too close from an edge,
            // so we raycast a bit on the left/right down and check for Terrain
            if (
                !Physics2D.Raycast(candidate + new Vector2(+0.85f, 1f), Vector2.down, 2f, Layer.Terrain.Mask) ||
                !Physics2D.Raycast(candidate + new Vector2(-0.85f, 1f), Vector2.down, 2f, Layer.Terrain.Mask)
            ) {
                // Debugging.DrawPoint(candidate + new Vector2(+0.85f, 1f), Color.red, 10f);
                // Debugging.DrawPoint(candidate + new Vector2(-0.85f, 1f), Color.red, 10f);
                // Log.Message("MapController", "Positioning fail due to being too close to an edge");
                continue; // try again
            }

            // Log.Message("MapController", "Position found at " + candidate);
            return candidate;

        } catch (System.Exception e) {
            Log.Critical("MapController", "Exception in PositionOnMap " + e);
        }

        if (canFail) {
            // Log.Message("MapController", "Impossible to find a position for: " + positionable.name);
            return null;
        }

        throw new Exception("Impossible to find a position for: " + positionable.name);
    }

    public async UniTask<Vector2?> PositionPlatform(
        MapPlatformController platform,
        int inPartHorizontal = 1,
        int ofPartsHorizontal = 1
    )
    {
        await WaitForPhysics().CancelOnDestroy(this);

        int tries = 0;
        int maxTries = width * height / 10;

        // Sum up all the bounds in a giant one for global positioning
        Bounds allBounds = new Bounds();
        bool firstBound = true;
        foreach (PolygonCollider2D collider in duplicableContainer.GetComponentsInChildren<PolygonCollider2D>()) {
            if (firstBound) {
                firstBound = false;
                allBounds = collider.bounds;
            } else {
                allBounds.Encapsulate(collider.bounds);
            }
        }

        while (++tries < maxTries) {
            // Every few tries we wait a bit so the interface can show some progress
            if (++globalTries % 20 == 0) {
                await UniTask.WaitForEndOfFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            // Last chances, so we remove some constraints
            if (tries > maxTries * 0.75f) {
                inPartHorizontal = 1;
                ofPartsHorizontal = 1;
            }

            // Get a random point
            inPartHorizontal = ((inPartHorizontal - 1) % ofPartsHorizontal) + 1;
            float length = allBounds.max.x - allBounds.min.x;
            float part = length / ofPartsHorizontal;
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(allBounds.min.x + ((inPartHorizontal - 1) * part), allBounds.min.x + (inPartHorizontal * part)),
                UnityEngine.Random.Range(allBounds.min.y, allBounds.max.y)
            );

            // Raycast a box and checks if nothing of the mask type is at that distance
            if (!Physics2D.BoxCast(candidate, Vector2.one * 20f, 0, Vector2.down, 0.1f, (Layer.Terrain | Layer.Platform | Layer.Bounds).Mask)) {
                return candidate;
            }
        }

        // Log.Message("MapController", "Impossible to find a position for platform");
        return null;
    }

    public async UniTask<Vector2?> PositionObject(
        PositionableInterface positionable,
        int inPartHorizontal = 1,
        int ofPartsHorizontal = 1
    )
    {
        await WaitForPhysics().CancelOnDestroy(this);

        int tries = 0;
        int maxTries = width * height / 10;

        var candidates = MapHelpers.PointsFromColliders(currentColliders);
        float smallestX = MapHelpers.SmallestX(candidates);
        float biggestX = MapHelpers.BiggestX(candidates);
        float amplitudeWidth = biggestX - smallestX;

        while (++tries < maxTries) {
            // Every few tries we wait a bit so the interface can show some progress
            if (++globalTries % 20 == 0) {
                await UniTask.WaitForEndOfFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            inPartHorizontal = ((inPartHorizontal - 1) % ofPartsHorizontal) + 1;

            float partWidth = amplitudeWidth / ofPartsHorizontal;
            float currentMinX = smallestX + ((inPartHorizontal - 1) * partWidth);
            float currentMaxX = smallestX + (inPartHorizontal * partWidth);

            // We get a candidate from the filtered collider points
            Vector2 candidate = candidates
                .Where((ii) => ii.x > currentMinX && ii.x < currentMaxX)
                .ToList()
                .GetRandomValue()
            ;

            // Check if we are inside anything else
            if (
                Physics2D.OverlapPoint(candidate) ||
                Physics2D.OverlapPoint(candidate + Vector2.up * 10f) ||
                Physics2D.OverlapPoint(candidate + Vector2.up * 15f)
            ) {
                // Debugging.DrawPoint(candidate, Color.white, 10f);
                // Debugging.DrawPoint(candidate + Vector2.up * 10f, Color.white, 10f);
                // Debugging.DrawPoint(candidate + Vector2.up * 15f, Color.white, 10f);
                continue; // try again
            }

            // Raycast a circle and checks if nothing of the mask type is in that radius
            if (!Physics2D.CircleCast(candidate, 15f, Vector2.down, 0.1f, (Layer.Platform | Layer.Object | Layer.Bounds).Mask)) {
                return candidate;
            }
        }

        // Log.Message("MapController", "Impossible to find a position for object: " + positionable.name);
        return null;
    }

    #endregion

    #region Beams

    public void PositionBeam(Vector2 position, float angle, bool isFacingRight, float distance)
    {
        var networkId = NetworkIdentifierNew();
        PositionBeam_Common(networkId, position, angle, isFacingRight, distance);
        NetworkRecordSnapshot(Method_PositionBeam, networkId, position, angle, isFacingRight, distance);
    }

    [StreamPlay(Method_PositionBeam)]
    protected void PositionBeam_Common(int networkId, Vector2 position, float angle, bool isFacingRight, float distance)
    {
        if (!isFacingRight) angle *= -1;
        Vector2 distanceToCharacter = position + (isFacingRight ? Vector2.right : Vector2.left) * distance;
        Vector2 translatedPosition = Basics.PointRotateAroundPivot(distanceToCharacter, position, angle);

        MapBeamController beam = NetworkInstantiate(mapBeamPrefab, networkId);
        beam.transform.SetParent(transform);
        beam.SetPosition(translatedPosition);
        beam.SetAngle(angle);
        beam.SetSymmetry(!isFacingRight);

        CurrentSortingLayer += 2;
        beam.Sort(CurrentSortingLayer);

        CurrentCollidersAdd(beam.GetComponentsInChildren<PolygonCollider2D>());
    }

    #endregion

    [ContextMenu("DrawColliders")]
    protected void DrawColliders()
    {
        lineRendererContainer.transform.RemoveAllChildren();
        foreach (var c in currentColliders) {
            GetLineRendererForPolygonCollider(c, dottedLineRendererPrefab, lineRendererContainer.transform);
        }
    }

    public static List<LineRenderer> GetLineRendererForPolygonCollider(PolygonCollider2D collider, LineRenderer lineRendererPrefab, Transform parent = null)
    {
        var lineRenderers = new List<LineRenderer>();
        for (int pathIndex = 0, pathCount = collider.pathCount; pathIndex < pathCount; pathIndex++) {
            var points = collider.GetTranslatedScaledPath(pathIndex);
            LineRenderer lineRenderer = Instantiate(lineRendererPrefab, parent);
            lineRenderer.positionCount = points.Length + 1;
            for (int pointIndex = 0, pointCount = points.Length; pointIndex < pointCount; pointIndex++) {
                var point = points[pointIndex];
                lineRenderer.SetPosition(pointIndex, point);
            }
            lineRenderer.SetPosition(points.Length, points[0]);
            lineRenderers.Add(lineRenderer);
        }
        return lineRenderers;
    }

    #region Wind

    public void UpdateWind(float force)
    {
        particleSystemForceField.directionX = force * 25f;
        BackgroundController.UpdateWind(force * 2.5f);
    }

    #endregion

    #region Sync

    public void Sync()
    {
        // TODO prio 2 it could send the current collider paths
        // and stamps size/position.
        // For now we suppose the snapshots made along the way are enough
        // and the map is not out of sync
        NetworkRecordSnapshot(Method_Map_Sync_Guest);
    }

    [StreamPlay(Method_Map_Sync_Guest)]
    protected void Sync_Guest()
    {

    }

    #endregion

    #region Colliders

    // TODO prio 3 it will be better to find out why some of those colliders are null/destroyed
    private void CurrentCollidersAdd(PolygonCollider2D[] colliders)
    {
#if CC_DEBUG
        foreach (var c in colliders) {
            if (c == null) {
                Log.Error("MapController", "Trying to add a null colliders to CurrentColliders list");
                // Debugging.LogTraceFromHere();
            }
        }
#endif
        currentColliders.AddRange(colliders.Where(c => c != null));
    }

    #endregion
}
