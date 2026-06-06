using System.Collections.Generic;
using UnityEngine;

// Logical placement grid for furniture. Owns cell↔world conversion, bounds and
// occupancy — all the placement MATH, isolated from meshes (FurnitureSpawner) and
// input/flow (FurnitureService). Origin is this transform's position (the grid's
// min corner); the buildable area spans Dimensions cells of CellSize on the XZ plane.
public class PlacementGrid : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int dimensions = new Vector2Int(20, 20);

    [Header("Floor snapping")]
    [Tooltip("FLOOR layers the vertical probe hits to find the ground Y under a cell. The grid stays a flat LOGICAL plane (XZ); placement Y is read from the real floor below, so it works over irregular terrain. Keep the grid transform slightly above the highest floor.")]
    [SerializeField] private LayerMask floorMask = ~0;
    [Tooltip("Max angle (deg) between the floor normal and up for a cell to count as flat. Steeper than this → the cell is an invalid placement (no tilted furniture).")]
    [SerializeField] private float maxSlopeAngle = 5f;
    [Tooltip("How far above/below the grid plane the vertical floor probe reaches.")]
    [SerializeField] private float floorProbeHeight = 50f;

    // Runtime occupancy (derived from the registry). Cleared on a full reload.
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    public float CellSize => cellSize;

    // Snaps a world point to the anchor cell that contains it.
    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - transform.position;
        return new Vector2Int(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
    }

    // World-space center of a footprint anchored at 'anchor', accounting for rotation.
    public Vector3 FootprintCenter(Vector2Int anchor, Vector2Int footprint, int rotation)
    {
        Vector2Int fp = Rotated(footprint, rotation);
        return transform.position + new Vector3((anchor.x + fp.x * 0.5f) * cellSize, 0f, (anchor.y + fp.y * 0.5f) * cellSize);
    }

    // Vertical probe at the footprint center to read the real floor under a cell. Returns the
    // ground Y and whether that ground is flat enough to build on (normal within maxSlopeAngle
    // of up). Shared by the build-mode ghost and the spawner so preview == placed (Option B:
    // Y is recomputed from the terrain, never stored on the lightweight record).
    public bool TrySampleFloor(Vector2Int anchor, Vector2Int footprint, int rotation, out float y, out bool flat)
    {
        Vector3 c = FootprintCenter(anchor, footprint, rotation);
        Vector3 origin = new Vector3(c.x, transform.position.y + floorProbeHeight, c.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, floorProbeHeight * 2f, floorMask, QueryTriggerInteraction.Ignore))
        {
            y = hit.point.y;
            flat = Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle;
            return true;
        }
        y = transform.position.y;
        flat = false;
        return false;
    }

    public bool CanPlace(Vector2Int anchor, Vector2Int footprint, int rotation)
    {
        foreach (var cell in Cells(anchor, footprint, rotation))
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= dimensions.x || cell.y >= dimensions.y) return false;
            if (occupied.Contains(cell)) return false;
        }
        return true;
    }

    public void Occupy(Vector2Int anchor, Vector2Int footprint, int rotation)
    {
        foreach (var cell in Cells(anchor, footprint, rotation)) occupied.Add(cell);
    }

    public void Free(Vector2Int anchor, Vector2Int footprint, int rotation)
    {
        foreach (var cell in Cells(anchor, footprint, rotation)) occupied.Remove(cell);
    }

    public void Clear() => occupied.Clear();

    // A 90°/270° turn swaps the footprint's X/Y extent.
    private static Vector2Int Rotated(Vector2Int fp, int rotation)
        => (Mathf.Abs(rotation % 180) == 0) ? fp : new Vector2Int(fp.y, fp.x);

    private static IEnumerable<Vector2Int> Cells(Vector2Int anchor, Vector2Int footprint, int rotation)
    {
        Vector2Int fp = Rotated(footprint, rotation);
        for (int i = 0; i < fp.x; i++)
            for (int j = 0; j < fp.y; j++)
                yield return new Vector2Int(anchor.x + i, anchor.y + j);
    }

    private void OnDrawGizmos()
    {
        Vector3 o = transform.position;

        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
        for (int x = 0; x <= dimensions.x; x++)
            Gizmos.DrawLine(o + new Vector3(x * cellSize, 0, 0), o + new Vector3(x * cellSize, 0, dimensions.y * cellSize));
        for (int y = 0; y <= dimensions.y; y++)
            Gizmos.DrawLine(o + new Vector3(0, 0, y * cellSize), o + new Vector3(dimensions.x * cellSize, 0, y * cellSize));

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        foreach (var c in occupied)
            Gizmos.DrawCube(o + new Vector3((c.x + 0.5f) * cellSize, 0.05f, (c.y + 0.5f) * cellSize),
                            new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
    }
}
