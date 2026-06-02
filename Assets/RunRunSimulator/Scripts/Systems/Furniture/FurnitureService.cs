using Sirenix.OdinInspector;
using UnityEngine;

// Orchestrates furniture placement: validates against the grid, mutates the registry,
// and announces changes via GameEvents (the spawner reacts, persistence later). For
// now it exposes Odin TEST buttons to prove the data → grid → world pipeline with a
// 1x1 cube. The real building-mode flow (ghost preview, click to position, F to
// confirm, Esc to cancel, right-click to delete) will land next on top of TryPlace /
// TryRemove — these are the API the Building action map will drive.
public class FurnitureService : MonoBehaviour
{
    [Required, SerializeField] private PlacementGrid grid;
    [Required, SerializeField] private FurnitureRegistrySO registry;
    [Required, SerializeField] private FurnitureDatabaseSO database;

    [Tooltip("The piece the test buttons place. For now: the 1x1 cube definition.")]
    [Required, SerializeField] private FurnitureDefinitionSO activePiece;

    [Title("Test placement (Play mode)")]
    [SerializeField] private int cellX;
    [SerializeField] private int cellY;
    [PropertyRange(0, 270), LabelText("Rotation (90° steps)")]
    [SerializeField] private int rotation;

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

    // ── API (the Building mode will call these too) ───────────────

    public bool TryPlace(Vector2Int cell, int rot)
    {
        if (activePiece == null) { Debug.LogError("[FurnitureService] No active piece set."); return false; }
        if (!grid.CanPlace(cell, activePiece.Footprint, rot))
        {
            Debug.Log($"[FurnitureService] Cell {cell} blocked or out of bounds.");
            return false;
        }

        var piece = new PlacedFurniture { DefId = activePiece.Id, CellX = cell.x, CellY = cell.y, Rotation = rot };
        if (!registry.Place(piece)) return false;

        grid.Occupy(cell, activePiece.Footprint, rot);
        GameEvents.FurnitureChanged(registry);
        Debug.Log($"[FurnitureService] Placed '{activePiece.Id}' at {cell} (rot {rot}).");
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

    // Rounds an arbitrary angle to the nearest 90° step in [0, 270].
    private static int Snap90(int deg) => ((Mathf.RoundToInt(deg / 90f) * 90) % 360 + 360) % 360;
}
