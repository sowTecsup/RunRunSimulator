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
