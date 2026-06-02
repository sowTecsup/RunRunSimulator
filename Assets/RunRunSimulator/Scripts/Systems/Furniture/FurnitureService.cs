using System.Collections.Generic;
using Sirenix.OdinInspector;
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

    [Title("Hotbar")]
    [Tooltip("Pieces the build mode selects with keys 1-4 (index 0 = key 1). Hardcoded here for now.")]
    [SerializeField] private List<FurnitureDefinitionSO> activePieces = new List<FurnitureDefinitionSO>();

    [Title("Test placement (Play mode)")]
    [SerializeField] private int cellX;
    [SerializeField] private int cellY;
    [PropertyRange(0, 270), LabelText("Rotation (90° steps)")]
    [SerializeField] private int rotation;

    // Which hotbar entry is selected (set by SelectPiece in build mode; test buttons use it too).
    private int selectedIndex;

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
    // the ghost mesh and TryPlace's def. Single source of "what am I placing".
    public FurnitureDefinitionSO ActivePiece =>
        (activePieces != null && selectedIndex >= 0 && selectedIndex < activePieces.Count)
            ? activePieces[selectedIndex] : null;

    // Selects hotbar slot 'index' (0-based). Returns false if empty / out of range.
    public bool SelectPiece(int index)
    {
        if (activePieces == null || index < 0 || index >= activePieces.Count || activePieces[index] == null)
            return false;
        selectedIndex = index;
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
