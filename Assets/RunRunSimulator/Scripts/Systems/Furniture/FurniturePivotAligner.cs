using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class FurniturePivotAligner : MonoBehaviour
{
    [Tooltip("Cells the piece occupies — match the FurnitureDefinitionSO footprint. Used only for the preview gizmo.")]
    public Vector2Int footprint = Vector2Int.one;

    [Tooltip("World size of one grid cell (match PlacementGrid.cellSize). Used only for the preview gizmo.")]
    [MinValue(0.01f)] public float cellSize = 1f;

    [Tooltip("Put the mesh's BASE on the pivot's Y plane (rests on the floor). Off = vertical center.")]
    public bool restOnFloor = true;

#if UNITY_EDITOR
    [Button("Center pivot on mesh (XZ center + base)", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    private void CenterPivot()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) { Debug.LogWarning("[PivotAligner] No renderers under this root."); return; }
        if (transform.childCount == 0)
        {
            Debug.LogWarning("[PivotAligner] Put the mesh under a CHILD of this root — the root can't move relative to itself.");
            return;
        }

        Bounds b = LocalBounds(renderers);

        Vector3 offset = new Vector3(-b.center.x, restOnFloor ? -b.min.y : -b.center.y, -b.center.z);

        var children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++) children[i] = transform.GetChild(i);

        UnityEditor.Undo.RecordObjects(children, "Center furniture pivot");
        foreach (var child in children) child.localPosition += offset;

        Debug.Log($"[PivotAligner] Recentered {children.Length} child(ren) by {offset}. Safe to remove this component now.");
    }

    private Bounds LocalBounds(Renderer[] renderers)
    {
        Matrix4x4 toLocal = transform.worldToLocalMatrix;
        Bounds result = default;
        bool has = false;

        foreach (var r in renderers)
        {
            Vector3 c = r.bounds.center, e = r.bounds.extents;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 corner = toLocal.MultiplyPoint3x4(c + new Vector3(sx * e.x, sy * e.y, sz * e.z));
                if (!has) { result = new Bounds(corner, Vector3.zero); has = true; }
                else result.Encapsulate(corner);
            }
        }
        return result;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(footprint.x * cellSize, 0.02f, footprint.y * cellSize));
    }
#endif
}
}
