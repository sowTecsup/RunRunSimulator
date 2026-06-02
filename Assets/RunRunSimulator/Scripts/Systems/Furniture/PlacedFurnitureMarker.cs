using UnityEngine;

// Stamped on every spawned furniture instance so build mode can pick a placed piece by
// pointing AT it (a furniture-layer raycast) and recover its anchor cell directly — no second
// floor raycast, no name parsing, and exact for multi-cell pieces. Added by FurnitureSpawner.
public class PlacedFurnitureMarker : MonoBehaviour
{
    public Vector2Int AnchorCell;
}
