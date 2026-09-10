using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaShapeBrush : MonoBehaviour
{
    [Title("Máscara")]
    [SerializeField, HideInInspector] private byte[] mask;
    [SerializeField] private int size = 128;
    [SerializeField, Min(0.1f)] private float cell = 0.5f;

    [Title("Pincel")]
    [SerializeField, Min(0.5f)] private float brushRadius = 3f;
    [SerializeField, Min(0.1f)] private float simplifyTolerance = 0.6f;
    [SerializeField] private ArenaRegionKind holeKind = ArenaRegionKind.Lake;

    [Title("Blobs por semilla")]
    [SerializeField] private int blobSeed = 1;
    [SerializeField] private bool proceduralBySeed;
    [SerializeField, Min(1f)] private float centerRadius = 7f;
    [SerializeField] private Vector2Int blobCount = new Vector2Int(4, 8);
    [SerializeField] private Vector2 blobRadius = new Vector2(4f, 8f);
    [SerializeField, Min(1f)] private float blobSpread = 13f;
    [SerializeField, Range(0.3f, 1f)] private float blobOverlap = 0.65f;
    [SerializeField] private Vector2Int holeCount = new Vector2Int(0, 2);
    [SerializeField] private Vector2 holeRadius = new Vector2(1.5f, 3f);
    [SerializeField, Min(0f)] private float holeMinFromCenter = 8f;

    [Title("Entradas")]
    [SerializeField] private bool autoEntries = true;
    [SerializeField, Min(0f)] private float entryInset = 2.5f;
    [SerializeField, Min(0f)] private float entryMinFromCenter = 8f;

    private ArenaShape shape;
    private int version;

    public int Size => size;
    public float Cell => cell;
    public Vector2 Origin => new Vector2(shape.Center.x, shape.Center.z) - Vector2.one * (size * cell * 0.5f);
    public float BrushRadius { get => brushRadius; set => brushRadius = value; }
    public byte[] Mask
    {
        get
        {
            EnsureMask();
            return mask;
        }
    }
    public bool ProceduralBySeed => proceduralBySeed;
    public int Version => version;

    private void Awake()
    {
        EnsureMask();
    }

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();
        EnsureMask();
    }

    private void EnsureMask()
    {
        if (mask == null || mask.Length != size * size)
            mask = new byte[size * size];
    }

    public void PaintAt(Vector3 world, bool erase)
    {
        EnsureMask();

        var center = new Vector2(world.x, world.z);
        ArenaShapeMask.Paint(mask, size, cell, Origin, center, brushRadius, !erase);

        if (shape.Symmetric)
        {
            if (!erase)
            {
                ArenaShapeMask.Symmetrize(mask, size);
            }
            else
            {
                var mirrored = Rotate180(center);
                ArenaShapeMask.Paint(mask, size, cell, Origin, mirrored, brushRadius, false);
            }
        }

        version++;
    }

    public void Apply()
    {
        EnsureMask();

        var loops = ArenaShapeMask.Contours(mask, size, cell, Origin);
        if (loops.Count == 0)
        {
            Debug.LogWarning($"[ArenaShapeBrush] {name}: la máscara está vacía, no hay nada que aplicar.");
            return;
        }

        int exteriorIndex = 0;
        float exteriorArea = 0f;
        for (int i = 0; i < loops.Count; i++)
        {
            float area = Mathf.Abs(ArenaShapeMask.SignedArea(loops[i]));
            if (area > exteriorArea)
            {
                exteriorArea = area;
                exteriorIndex = i;
            }
        }

        var exterior = loops[exteriorIndex];
        var holes = new List<IReadOnlyList<Vector2>>();
        int islands = 0;

        for (int i = 0; i < loops.Count; i++)
        {
            if (i == exteriorIndex) continue;

            var loop = loops[i];
            if (loop.Count == 0) continue;

            if (ArenaShapeMask.Contains(exterior, loop[0])) holes.Add(loop);
            else islands++;
        }

        if (islands > 0)
            Debug.Log($"[ArenaShapeBrush] {name}: se descartaron {islands} islas sueltas.");

        var simplifiedExterior = ArenaShapeMask.Simplify(exterior, simplifyTolerance);
        var simplifiedHoles = new List<IReadOnlyList<Vector2>>();
        foreach (var hole in holes)
            simplifiedHoles.Add(ArenaShapeMask.Simplify(hole, simplifyTolerance));

        shape.WriteOutline(simplifiedExterior);
        shape.WriteRegions(holeKind, simplifiedHoles);

        if (autoEntries) PlaceEntries();

        version++;
    }

    public void Regenerate(int seed)
    {
        EnsureMask();

        var rng = new System.Random(seed);
        var p = new ArenaShapeMask.BlobParams
        {
            CenterRadius = centerRadius,
            Count = rng.Next(blobCount.x, blobCount.y + 1),
            Radius = blobRadius,
            Spread = blobSpread,
            Overlap = blobOverlap,
        };

        ArenaShapeMask.Blobs(mask, size, cell, Origin, rng, p);
        if (shape.Symmetric) ArenaShapeMask.Symmetrize(mask, size);

        var centerXZ = new Vector2(shape.Center.x, shape.Center.z);
        int holes = rng.Next(holeCount.x, holeCount.y + 1);

        for (int h = 0; h < holes; h++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float distance = (float)(rng.NextDouble() * blobSpread);
                if (distance < holeMinFromCenter) continue;

                var candidate = centerXZ + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                int i = Mathf.FloorToInt((candidate.x - Origin.x) / cell);
                int j = Mathf.FloorToInt((candidate.y - Origin.y) / cell);
                if (i < 0 || i >= size || j < 0 || j >= size) continue;
                if (!ArenaShapeMask.Get(mask, size, i, j)) continue;

                float radius = (float)(rng.NextDouble() * (holeRadius.y - holeRadius.x) + holeRadius.x);
                ArenaShapeMask.Paint(mask, size, cell, Origin, candidate, radius, false);

                if (shape.Symmetric)
                {
                    var mirrored = Rotate180(candidate);
                    ArenaShapeMask.Paint(mask, size, cell, Origin, mirrored, radius, false);
                }

                break;
            }
        }

        Apply();
        blobSeed = seed;
    }

    [Button] public void Regenerate() => Regenerate(blobSeed);

    [Button] public void Clear()
    {
        EnsureMask();
        ArenaShapeMask.Clear(mask);
        version++;
    }

    private void PlaceEntries()
    {
        var directions = new (string name, Vector2 dir)[]
        {
            ("diagonal", new Vector2(-1f, -1f).normalized),
            ("diagonal inversa", new Vector2(1f, -1f).normalized),
            ("norte-sur", new Vector2(0f, -1f)),
            ("este-oeste", new Vector2(-1f, 0f)),
        };

        var centerXZ = new Vector2(shape.Center.x, shape.Center.z);
        var names = new List<string>();
        var positions = new List<Vector3>();

        foreach (var entry in directions)
        {
            bool found = false;
            var best = Vector2.zero;
            float bestDot = float.NegativeInfinity;

            foreach (var point in shape.OutlinePolygon)
            {
                var offset = point - centerXZ;
                if (Vector2.Angle(offset, entry.dir) > 25f) continue;

                float dot = Vector2.Dot(offset, entry.dir);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = point;
                    found = true;
                }
            }

            if (!found) continue;
            if ((best - centerXZ).magnitude < entryMinFromCenter) continue;

            var anchor = best - entry.dir * entryInset;
            names.Add(entry.name);
            positions.Add(new Vector3(anchor.x, shape.Center.y, anchor.y));
        }

        if (names.Count > 0) shape.SetEntries(names, positions);
    }

    private Vector2 Rotate180(Vector2 point)
    {
        var centerXZ = new Vector2(shape.Center.x, shape.Center.z);
        return ArenaShapeMesher.Rotate180(new List<Vector2> { point }, centerXZ)[0];
    }

    private void OnDrawGizmosSelected()
    {
        if (shape == null) shape = GetComponent<ArenaShape>();
        if (shape == null) return;

        var origin = Origin;
        float width = size * cell;
        float y = shape.Center.y + 0.05f;

        var a = new Vector3(origin.x, y, origin.y);
        var b = new Vector3(origin.x + width, y, origin.y);
        var c = new Vector3(origin.x + width, y, origin.y + width);
        var d = new Vector3(origin.x, y, origin.y + width);

        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}
}
