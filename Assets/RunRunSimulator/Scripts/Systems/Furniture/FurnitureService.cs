using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;

// Orchestrates furniture placement: validates against the grid, mutates the registry,
// and announces changes via GameEvents (the spawner reacts, persistence later). It owns
// the placement API the Building mode drives — TryPlace / TryLift / TryRemove — plus the
// hotbar of placeable pieces (activePieces, picked with 1-4 in build mode). The Odin TEST
// buttons place the currently selected hotbar piece without entering build mode.
public class FurnitureService : MonoBehaviour
{
    [Required, SerializeField] private PlacementGrid grid;
    [Required, SerializeField] private FurnitureRegistrySO registry;
    [Required, SerializeField] private FurnitureDatabaseSO database;

    [Title("NavMesh")]
    [Tooltip("Scene NavMesh surface, rebaked after furniture loads / on demand. Needed so breeding-room floors get their painted area baked in. Leave empty if this scene has no pens.")]
    [SerializeField] private NavMeshSurface navSurface;

    [Title("Hotbar — DEBUG ONLY (no production)")]
    [InfoBox("Esta lista es un fallback de testing (teclas 1-4 hardcoded). En producción la pieza activa siempre vendrá del BuildBrowserUITK vía SetActivePiece(). No eliminar: SelectPiece() aún la usa internamente.", InfoMessageType.Warning)]
    [Tooltip("Debug fallback: build browser overrides this at runtime via SetActivePiece.")]
    [SerializeField] private List<FurnitureDefinitionSO> activePieces = new List<FurnitureDefinitionSO>();

    [Title("Test placement (Play mode)")]
    [SerializeField] private int cellX;
    [SerializeField] private int cellY;
    [PropertyRange(0, 270), LabelText("Rotation (90° steps)")]
    [SerializeField] private int rotation;

    // Which hotbar entry is selected (set by SelectPiece in build mode; test buttons use it too).
    private int selectedIndex;

    // Set by the build browser (SetActivePiece): overrides the indexed hotbar slot so the
    // browser can drive placement directly without mutating the serialized activePieces list.
    private FurnitureDefinitionSO runtimeActivePiece;

    private bool rebakePending;

    private void OnEnable()  => GameEvents.OnFurnitureReloaded += OnFurnitureReloaded;
    private void OnDisable() => GameEvents.OnFurnitureReloaded -= OnFurnitureReloaded;

    // After the initial furniture load (the spawner repopulates on its own Start), rebake once so
    // any breeding-room floors that loaded in get their painted area into the NavMesh.
    private void Start() => ScheduleRebake();

    // A bulk (re)load just repopulated the scene → rebake so newly-loaded pens are navigable.
    private void OnFurnitureReloaded(FurnitureRegistrySO r) => ScheduleRebake();

    // Defers the rebake to end of frame so the FurnitureSpawner has finished (re)building the
    // meshes first (both react to the same event; instantiation order between them isn't
    // guaranteed). Coalesces multiple requests in the same frame into a single bake.
    private void ScheduleRebake()
    {
        if (rebakePending || !isActiveAndEnabled || navSurface == null) return;
        rebakePending = true;
        StartCoroutine(RebakeEndOfFrame());
    }

    private IEnumerator RebakeEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        rebakePending = false;
        RebakeNavMesh();
    }

    // ── NavMesh rebake ────────────────────────────────────────────

    // Rebuilds the scene NavMesh from current geometry — call after placing/loading a breeding
    // room so its painted floor area gets baked in. Occasional by design (placement is a
    // decorate-time action), so the rebake cost is acceptable.
    [Button("Rebake NavMesh", ButtonSizes.Large), GUIColor(0.6f, 0.8f, 1f)]
    public void RebakeNavMesh()
    {
       
       if (navSurface != null && navSurface.navMeshData != null)
        {
            StartCoroutine(UpdateNavMeshAsyncRoutine());
        }
       else
        {
            navSurface.BuildNavMesh();
        }
    }
    private System.Collections.IEnumerator UpdateNavMeshAsyncRoutine()
    {
        // UpdateNavMesh calcula solo las áreas que cambiaron usando los datos existentes
        AsyncOperation op = navSurface.UpdateNavMesh(navSurface.navMeshData);

        // Esperamos a que termine en segundo plano
        yield return op;

        Debug.Log("¡NavMesh actualizado con éxito (Asíncrono)!");
    }

    // ── Odin test buttons ─────────────────────────────────────────

    [Button("Place at Cell", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void PlaceTest()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[FurnitureService] Enter Play mode to place."); return; }
        TryPlace(new Vector2Int(cellX, cellY), Snap90(rotation));
    }

    [Button("Remove at Cell"), GUIColor(1f, 0.6f, 0.5f)]
    private void RemoveTest()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[FurnitureService] Enter Play mode to remove."); return; }
        TryRemove(new Vector2Int(cellX, cellY));
    }

    [Button("Clear All"), GUIColor(1f, 0.4f, 0.4f)]
    private void ClearTest()
    {
        if (!Application.isPlaying) return;
        registry.LoadFrom(null);
        grid.Clear();
        GameEvents.FurnitureReloaded(registry);
    }

    // ── Hotbar selection ──────────────────────────────────────────

    // The piece currently selected for placement — BuildModeController reads it for both
    // the ghost mesh and TryPlace's def. Single source of "what am I placing". The browser's
    // runtime pick wins; otherwise fall back to the serialized hotbar slot (1-4 test path).
    public FurnitureDefinitionSO ActivePiece =>
        runtimeActivePiece != null
            ? runtimeActivePiece
            : (activePieces != null && selectedIndex >= 0 && selectedIndex < activePieces.Count)
                ? activePieces[selectedIndex] : null;

    // Selects hotbar slot 'index' (0-based). Returns false if empty / out of range.
    // Clears any browser override so the indexed slot takes effect.
    public bool SelectPiece(int index)
    {
        if (activePieces == null || index < 0 || index >= activePieces.Count || activePieces[index] == null)
            return false;
        runtimeActivePiece = null;
        selectedIndex = index;
        return true;
    }

    // The build browser's pick: drive placement with an arbitrary owned piece without
    // touching the serialized activePieces list (mutating a serialized List at runtime
    // is fragile). Returns false for a null def.
    public bool SetActivePiece(FurnitureDefinitionSO def)
    {
        if (def == null) return false;
        runtimeActivePiece = def;
        return true;
    }

    // ── Placement API (Building mode + test buttons) ──────────────

    public bool TryPlace(Vector2Int cell, int rot) => TryPlace(ActivePiece, cell, rot);

    public bool TryPlace(FurnitureDefinitionSO def, Vector2Int cell, int rot)
    {
        if (def == null) { Debug.LogError("[FurnitureService] No piece to place."); return false; }
        if (!grid.CanPlace(cell, def.Footprint, rot))
        {
            Debug.Log($"[FurnitureService] Cell {cell} blocked or out of bounds.");
            return false;
        }

        var piece = new PlacedFurniture { DefId = def.Id, CellX = cell.x, CellY = cell.y, Rotation = rot };
        if (!registry.Place(piece)) return false;

        grid.Occupy(cell, def.Footprint, rot);
        GameEvents.FurnitureChanged(registry);
        Debug.Log($"[FurnitureService] Placed '{def.Id}' at {cell} (rot {rot}).");
        return true;
    }

    public bool TryRemove(Vector2Int cell)
    {
        string key = PlacedFurniture.Key(cell.x, cell.y);
        if (!registry.TryGet(key, out var piece))
        {
            Debug.Log($"[FurnitureService] Nothing placed at {cell}.");
            return false;
        }

        var def = database.GetById(piece.DefId);
        Vector2Int footprint = def != null ? def.Footprint : Vector2Int.one;

        grid.Free(cell, footprint, piece.Rotation);
        registry.RemoveAt(key);
        GameEvents.FurnitureChanged(registry);
        Debug.Log($"[FurnitureService] Removed at {cell}.");
        return true;
    }

    // Lifts the piece anchored at 'cell' (removes it from registry + grid) and returns its
    // definition + rotation, so build mode can re-place it after editing or drop it on
    // delete. Symmetric with TryPlace — fires FurnitureChanged, so the mesh despawns now.
    public bool TryLift(Vector2Int cell, out FurnitureDefinitionSO def, out int rot)
    {
        def = null; rot = 0;
        string key = PlacedFurniture.Key(cell.x, cell.y);
        if (!registry.TryGet(key, out var piece)) return false;

        def = database.GetById(piece.DefId);
        rot = piece.Rotation;
        Vector2Int footprint = def != null ? def.Footprint : Vector2Int.one;

        grid.Free(cell, footprint, piece.Rotation);
        registry.RemoveAt(key);
        GameEvents.FurnitureChanged(registry);
        return true;
    }

    // Rounds an arbitrary angle to the nearest 90° step in [0, 270].
    private static int Snap90(int deg) => ((Mathf.RoundToInt(deg / 90f) * 90) % 360 + 360) % 360;
}
