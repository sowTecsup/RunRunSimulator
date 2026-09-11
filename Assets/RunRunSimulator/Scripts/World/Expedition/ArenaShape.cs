using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
namespace MoriMonchiSimulator
{

[ExecuteAlways]
public class ArenaShape : MonoBehaviour
{
    [Title("Identidad")]
    [SerializeField] private string displayName = "Sala";

    [Title("Splines")]
    [Required, SerializeField] private SplineContainer outline;
    [SerializeField] private SplineContainer rocks;
    [SerializeField] private SplineContainer lakes;
    [SerializeField] private SplineContainer pits;
    [SerializeField] private SplineContainer groves;
    [SerializeField] private bool symmetric = true;
    [SerializeField, Min(0.2f)] private float samplesPerMeter = 1.5f;

    [Title("Entradas")]
    [SerializeField] private List<Transform> entries = new();

    [Title("Relieve")]
    [SerializeField, Min(0f)] private float cliffHeight = 1.2f;
    [SerializeField, Min(0f)] private float cliffDepth = 1.5f;
    [SerializeField, Min(0f)] private float fenceHeight = 1.5f;
    [SerializeField, Min(0f)] private float rockHeight = 1.2f;
    [SerializeField, Min(0f)] private float lakeDepth = 0.6f;
    [SerializeField] private float waterLevel = -0.15f;
    [SerializeField, Min(0f)] private float pitDepth = 3f;
    [SerializeField, Min(0.01f)] private float uvScale = 0.25f;

    [Title("Materiales")]
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Material rockMaterial;
    [SerializeField] private Material lakeBedMaterial;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Material pitMaterial;

    [Title("Bosque")]
    [SerializeField] private List<GameObject> grovePrefabs = new();
    [SerializeField, Min(0.5f)] private float groveSpacing = 2.5f;
    [SerializeField] private int groveSeed = 1;
    [SerializeField] private Vector2 groveScale = new Vector2(0.8f, 1.3f);
    [SerializeField, Min(0f)] private float groveEdgeMargin = 1f;

    [Title("Pasto de borde")]
    [SerializeField] private List<GameObject> edgePrefabs = new();
    [SerializeField, Min(0.3f)] private float edgeStep = 1.6f;
    [SerializeField, Min(0f)] private float edgeInset = 0.9f;
    [SerializeField, Min(0f)] private float edgeJitter = 0.35f;
    [SerializeField] private Vector2 edgeScale = new Vector2(0.9f, 1.3f);

    [Title("Extras de escena")]
    [SerializeField] private List<GameObject> extras = new();

    private readonly List<Vector2> outlinePolygon = new();
    private readonly List<IReadOnlyList<Vector2>> rockPolygons = new();
    private readonly List<IReadOnlyList<Vector2>> lakePolygons = new();
    private readonly List<IReadOnlyList<Vector2>> pitPolygons = new();
    private readonly List<IReadOnlyList<Vector2>> grovePolygons = new();
    private GameObject generated;
    private bool dirty;

    public string DisplayName => displayName;
    public bool Symmetric => symmetric;
    public Vector3 Center => transform.position;
    public Bounds Bounds => ArenaShapeMesher.Bounds(outlinePolygon);
    public IReadOnlyList<Vector2> OutlinePolygon => outlinePolygon;
    public IReadOnlyList<IReadOnlyList<Vector2>> RockPolygons => rockPolygons;
    public IReadOnlyList<IReadOnlyList<Vector2>> LakePolygons => lakePolygons;
    public IReadOnlyList<IReadOnlyList<Vector2>> PitPolygons => pitPolygons;
    public IReadOnlyList<IReadOnlyList<Vector2>> GrovePolygons => grovePolygons;
    public bool FloorBuilt { get; private set; }

    public event System.Action Rebuilt;

    public bool Contains(Vector3 world)
    {
        var p = new Vector2(world.x, world.z);
        if (!ArenaShapeMesher.Contains(outlinePolygon, p)) return false;

        foreach (var poly in rockPolygons)
            if (ArenaShapeMesher.Contains(poly, p)) return false;
        foreach (var poly in lakePolygons)
            if (ArenaShapeMesher.Contains(poly, p)) return false;
        foreach (var poly in pitPolygons)
            if (ArenaShapeMesher.Contains(poly, p)) return false;

        return true;
    }

    public bool IsClear(Vector3 world, float margin)
    {
        if (!Contains(world)) return false;

        var p = new Vector2(world.x, world.z);
        if (ArenaShapeMesher.DistanceToEdge(outlinePolygon, p) < margin) return false;

        foreach (var poly in rockPolygons)
            if (ArenaShapeMesher.DistanceToEdge(poly, p) < margin) return false;
        foreach (var poly in lakePolygons)
            if (ArenaShapeMesher.DistanceToEdge(poly, p) < margin) return false;
        foreach (var poly in pitPolygons)
            if (ArenaShapeMesher.DistanceToEdge(poly, p) < margin) return false;

        return true;
    }

    public bool TryRandomPoint(System.Random rng, float margin, out Vector3 point)
    {
        var bounds = Bounds;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            float x = (float)(rng.NextDouble() * bounds.size.x + bounds.min.x);
            float z = (float)(rng.NextDouble() * bounds.size.z + bounds.min.z);
            var candidate = new Vector3(x, Center.y, z);

            if (IsClear(candidate, margin))
            {
                point = candidate;
                return true;
            }
        }

        point = default;
        return false;
    }

    public int EntryPairCount => symmetric ? entries.Count : entries.Count / 2;

    public Vector3 EntryPoint(int pair, ExpeditionTeam team)
    {
        if (entries.Count == 0) return Center;

        if (symmetric)
        {
            if (pair < 0 || pair >= entries.Count || entries[pair] == null) return Center;

            var playerXZ = new Vector2(entries[pair].position.x, entries[pair].position.z);
            var pointXZ = team == ExpeditionTeam.Rival ? Mirror(playerXZ) : playerXZ;
            return new Vector3(pointXZ.x, Center.y, pointXZ.y);
        }

        int index = team == ExpeditionTeam.Rival ? pair * 2 + 1 : pair * 2;
        if (index < 0 || index >= entries.Count || entries[index] == null) return Center;

        var p = entries[index].position;
        return new Vector3(p.x, Center.y, p.z);
    }

    public string EntryName(int pair)
    {
        if (symmetric)
        {
            if (pair < 0 || pair >= entries.Count || entries[pair] == null) return "";
            return entries[pair].name;
        }

        int index = pair * 2;
        if (index < 0 || index >= entries.Count || entries[index] == null) return "";
        return entries[index].name;
    }

    public void WriteOutline(IReadOnlyList<Vector2> points)
    {
        if (outline == null || points == null || points.Count < 3) return;

        outline.Splines = new[] { BuildClosedSpline(outline.transform, points) };
        Rebuild();
    }

    public void WriteRegions(ArenaRegionKind kind, IReadOnlyList<IReadOnlyList<Vector2>> polygons)
    {
        var container = kind switch
        {
            ArenaRegionKind.Rock => rocks,
            ArenaRegionKind.Lake => lakes,
            ArenaRegionKind.Pit => pits,
            ArenaRegionKind.Grove => groves,
            _ => null,
        };

        if (container == null) return;

        var accepted = new List<IReadOnlyList<Vector2>>();

        if (polygons != null)
        {
            foreach (var polygon in polygons)
            {
                if (symmetric)
                {
                    var centroid = Centroid(polygon);
                    bool isDuplicate = false;

                    foreach (var other in accepted)
                    {
                        if (Vector2.Distance(centroid, Mirror(Centroid(other))) < 0.5f)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (isDuplicate) continue;
                }

                accepted.Add(polygon);
            }
        }

        var splines = new Spline[accepted.Count];
        for (int i = 0; i < accepted.Count; i++)
            splines[i] = BuildClosedSpline(container.transform, accepted[i]);

        container.Splines = splines;
        Rebuild();
    }

    public void SetEntries(IReadOnlyList<string> names, IReadOnlyList<Vector3> positions)
    {
        var entriesParent = transform.Find("Entries");
        if (entriesParent == null)
        {
            var holder = new GameObject("Entries");
            holder.transform.SetParent(transform, false);
            entriesParent = holder.transform;
        }

        var result = new List<Transform>(names.Count);

        for (int i = 0; i < names.Count; i++)
        {
            var child = entriesParent.Find(names[i]);
            if (child == null)
            {
                var holder = new GameObject(names[i]);
                holder.transform.SetParent(entriesParent, false);
                child = holder.transform;
            }

            child.position = positions[i];
            result.Add(child);
        }

        for (int i = entriesParent.childCount - 1; i >= 0; i--)
        {
            var child = entriesParent.GetChild(i);
            bool keep = false;

            for (int j = 0; j < names.Count; j++)
            {
                if (child.name == names[j])
                {
                    keep = true;
                    break;
                }
            }

            if (!keep) DestroyImmediate(child.gameObject);
        }

        entries.Clear();
        entries.AddRange(result);
        Rebuilt?.Invoke();
    }

    private Spline BuildClosedSpline(Transform space, IReadOnlyList<Vector2> points)
    {
        var spline = new Spline();

        for (int i = 0; i < points.Count; i++)
        {
            var localPoint = space.InverseTransformPoint(new Vector3(points[i].x, Center.y, points[i].y));
            var local = new Vector2(localPoint.x, localPoint.z);
            spline.Add(new BezierKnot(new float3(local.x, 0, local.y)), TangentMode.AutoSmooth);
        }

        spline.Closed = true;
        return spline;
    }

    private static Vector2 Centroid(IReadOnlyList<Vector2> polygon)
    {
        var sum = Vector2.zero;
        for (int i = 0; i < polygon.Count; i++)
            sum += polygon[i];
        return sum / polygon.Count;
    }

    private void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
        SplineContainer.SplineAdded += OnContainerChanged;
        SplineContainer.SplineRemoved += OnContainerChanged;

        foreach (var extra in extras)
            if (extra != null) extra.SetActive(true);

        Rebuild();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
        SplineContainer.SplineAdded -= OnContainerChanged;
        SplineContainer.SplineRemoved -= OnContainerChanged;

        DestroyGenerated();

        foreach (var extra in extras)
            if (extra != null) extra.SetActive(false);
    }

    private void Update()
    {
        if (dirty) Rebuild();
    }

    private void OnValidate()
    {
        RequestRebuild();
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
    {
        if (Owns(spline)) RequestRebuild();
    }

    private void OnContainerChanged(SplineContainer container, int index)
    {
        if (container == outline || container == rocks || container == lakes || container == pits || container == groves)
            RequestRebuild();
    }

    private bool Owns(Spline spline)
    {
        return ContainerOwns(outline, spline) || ContainerOwns(rocks, spline) || ContainerOwns(lakes, spline)
            || ContainerOwns(pits, spline) || ContainerOwns(groves, spline);
    }

    private static bool ContainerOwns(SplineContainer container, Spline spline)
    {
        if (container == null) return false;

        foreach (var s in container.Splines)
            if (s == spline) return true;

        return false;
    }

    private void RequestRebuild()
    {
        dirty = true;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= RebuildIfDirty;
            UnityEditor.EditorApplication.delayCall += RebuildIfDirty;
        }
#endif
    }

    private void RebuildIfDirty()
    {
        if (this == null || !isActiveAndEnabled) return;
        if (dirty) Rebuild();
    }

    public void Rebuild()
    {
        dirty = false;
        FloorBuilt = false;

        DestroyGenerated();
        RecalculatePolygons();

        generated = new GameObject("Generated") { hideFlags = HideFlags.DontSave };
        generated.transform.SetParent(transform, false);

        BuildFloor();
        BuildCliff();
        BuildRocks();
        BuildLakes();
        BuildPits();
        BuildGroves();
        BuildEdge();

        Rebuilt?.Invoke();
    }

    private void DestroyGenerated()
    {
        if (generated != null)
        {
            DestroyImmediate(generated);
            generated = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "Generated") DestroyImmediate(child.gameObject);
        }
    }

    private void RecalculatePolygons()
    {
        outlinePolygon.Clear();
        rockPolygons.Clear();
        lakePolygons.Clear();
        pitPolygons.Clear();
        grovePolygons.Clear();

        if (outline == null || outline.Spline == null) return;

        var centerXZ = new Vector2(Center.x, Center.z);
        var s = outline.Spline;

        if (symmetric && !s.Closed)
        {
            var half = ArenaShapeMesher.Sample(s, outline.transform, samplesPerMeter, false);
            outlinePolygon.AddRange(ArenaShapeMesher.Symmetrize(half, centerXZ));
        }
        else
        {
            outlinePolygon.AddRange(ArenaShapeMesher.Sample(s, outline.transform, samplesPerMeter, true));
        }
        ArenaShapeMesher.MakeCounterClockwise(outlinePolygon);

        CollectRegion(rocks, rockPolygons, centerXZ, true);
        CollectRegion(lakes, lakePolygons, centerXZ, true);
        CollectRegion(pits, pitPolygons, centerXZ, true);
        CollectRegion(groves, grovePolygons, centerXZ, false);
    }

    private void CollectRegion(SplineContainer container, List<IReadOnlyList<Vector2>> target, Vector2 centerXZ, bool mirrorAsPolygon)
    {
        if (container == null) return;

        foreach (var spline in container.Splines)
        {
            if (spline == null) continue;

            var poly = ArenaShapeMesher.Sample(spline, container.transform, samplesPerMeter, true);
            ArenaShapeMesher.MakeCounterClockwise(poly);
            target.Add(poly);

            if (symmetric && mirrorAsPolygon)
                target.Add(ArenaShapeMesher.Rotate180(poly, centerXZ));
        }
    }

    private void BuildFloor()
    {
        if (outlinePolygon.Count == 0) return;

        var holes = new List<IReadOnlyList<Vector2>>();
        holes.AddRange(lakePolygons);
        holes.AddRange(pitPolygons);

        var mesh = ArenaShapeMesher.Floor(outlinePolygon, holes, Center.y, uvScale);
        if (mesh == null) return;

        Piece("Floor", generated.transform, mesh, groundMaterial, true, false);
        FloorBuilt = true;
    }

    private void BuildCliff()
    {
        if (outlinePolygon.Count == 0) return;

        if (cliffHeight > 0f || cliffDepth > 0f)
        {
            var mesh = ArenaShapeMesher.Skirt(outlinePolygon, Center.y + cliffHeight, Center.y - cliffDepth, true, uvScale);
            if (mesh != null) Piece("Cliff", generated.transform, mesh, null, true, true, false);
        }

        if (fenceHeight > 0f)
        {
            var fence = ArenaShapeMesher.Skirt(outlinePolygon, Center.y + fenceHeight, Center.y, true, uvScale);
            if (fence != null) Piece("Fence", generated.transform, fence, null, true, true, false);
        }
    }

    private void BuildRocks()
    {
        for (int i = 0; i < rockPolygons.Count; i++)
        {
            var poly = rockPolygons[i];
            var holder = new GameObject($"Rock_{i}") { hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(generated.transform, false);

            var skirt = ArenaShapeMesher.Skirt(poly, Center.y + rockHeight, Center.y - 0.1f, false, uvScale);
            if (skirt != null) Piece("Skirt", holder.transform, skirt, rockMaterial, true, true);

            var cap = ArenaShapeMesher.Cap(poly, Center.y + rockHeight, uvScale);
            if (cap != null) Piece("Cap", holder.transform, cap, rockMaterial, true, true);
        }
    }

    private void BuildLakes()
    {
        for (int i = 0; i < lakePolygons.Count; i++)
        {
            var poly = lakePolygons[i];
            var holder = new GameObject($"Lake_{i}") { hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(generated.transform, false);

            var bedSkirt = ArenaShapeMesher.Skirt(poly, Center.y, Center.y - lakeDepth, true, uvScale);
            if (bedSkirt != null) Piece("BedSkirt", holder.transform, bedSkirt, lakeBedMaterial, true, true);

            var bedCap = ArenaShapeMesher.Cap(poly, Center.y - lakeDepth, uvScale);
            if (bedCap != null) Piece("BedCap", holder.transform, bedCap, lakeBedMaterial, true, true);

            var water = ArenaShapeMesher.Cap(poly, Center.y + waterLevel, uvScale);
            if (water != null) Piece("Water", holder.transform, water, waterMaterial, false, false);
        }
    }

    private void BuildPits()
    {
        for (int i = 0; i < pitPolygons.Count; i++)
        {
            var poly = pitPolygons[i];
            var holder = new GameObject($"Pit_{i}") { hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(generated.transform, false);

            var skirt = ArenaShapeMesher.Skirt(poly, Center.y, Center.y - pitDepth, true, uvScale);
            if (skirt != null) Piece("Skirt", holder.transform, skirt, pitMaterial, true, true);

            var floor = ArenaShapeMesher.Cap(poly, Center.y - pitDepth, uvScale);
            if (floor != null) Piece("Cap", holder.transform, floor, pitMaterial, true, true);
        }
    }

    private void BuildGroves()
    {
        if (grovePrefabs.Count == 0) return;

        for (int k = 0; k < grovePolygons.Count; k++)
        {
            var poly = grovePolygons[k];
            var rng = new System.Random(groveSeed * 7919 + k);
            var points = ArenaShapeScatter.InPolygon(poly, rng, groveSpacing, groveEdgeMargin);

            foreach (var candidate in points)
            {
                var prefab = grovePrefabs[rng.Next(grovePrefabs.Count)];
                float yaw = (float)(rng.NextDouble() * 360.0);
                float scale = (float)(rng.NextDouble() * (groveScale.y - groveScale.x) + groveScale.x);
                var position = new Vector3(candidate.x, Center.y, candidate.y);

                SpawnGrove(prefab, position, yaw, scale);

                if (symmetric)
                {
                    var mirrored = Mirror(candidate);
                    SpawnGrove(prefab, new Vector3(mirrored.x, Center.y, mirrored.y), yaw + 180f, scale);
                }
            }
        }
    }

    private void SpawnGrove(GameObject prefab, Vector3 position, float yaw, float scale)
    {
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), generated.transform);
        instance.transform.localScale = Vector3.one * scale;
        instance.hideFlags = HideFlags.DontSave;
    }

    private void BuildEdge()
    {
        if (edgePrefabs.Count == 0 || outlinePolygon.Count == 0) return;

        var rng = new System.Random(groveSeed * 31 + 7);
        var points = ArenaShapeScatter.AlongEdge(outlinePolygon, rng, edgeStep, edgeInset, edgeJitter);

        foreach (var point in points)
        {
            var prefab = edgePrefabs[rng.Next(edgePrefabs.Count)];
            float yaw = (float)(rng.NextDouble() * 360.0);
            float scale = (float)(rng.NextDouble() * (edgeScale.y - edgeScale.x) + edgeScale.x);
            var position = new Vector3(point.x, Center.y, point.y);

            var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), generated.transform);
            instance.transform.localScale = Vector3.one * scale;
            instance.hideFlags = HideFlags.DontSave;

            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                DestroyImmediate(colliders[i]);
        }
    }

    private Vector2 Mirror(Vector2 point)
    {
        var centerXZ = new Vector2(Center.x, Center.z);
        return ArenaShapeMesher.Rotate180(new List<Vector2> { point }, centerXZ)[0];
    }

    private GameObject Piece(string name, Transform parent, Mesh mesh, Material material, bool collider, bool notWalkable, bool render = true)
    {
        mesh.hideFlags = HideFlags.DontSave;

        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(parent, false);

        if (render)
        {
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        if (collider)
        {
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        if (notWalkable)
        {
            var modifier = go.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1;
        }

        return go;
    }

    private void OnDrawGizmos()
    {
        ArenaShapeGizmos.Draw(this);
    }
}
}
