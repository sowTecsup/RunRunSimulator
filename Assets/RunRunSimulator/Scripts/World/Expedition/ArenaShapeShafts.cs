using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaShapeShafts : MonoBehaviour
{
    [Title("Semilla")]
    [SerializeField] private int seed = 3;

    [Title("Rayos")]
    [SerializeField] private GameObject shaftPrefab;
    [SerializeField] private Vector2Int count = new Vector2Int(1, 2);
    [SerializeField] private Vector2 scale = new Vector2(0.9f, 1.5f);
    [SerializeField, Min(0f)] private float margin = 6f;
    [SerializeField] private bool alignToSun = true;
    [SerializeField] private float tilt = 14f;
    [SerializeField] private float spread = 10f;

    private ArenaShape shape;
    private GameObject shafts;

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();
        shape.Rebuilt += Dress;
        Dress();
    }

    private void OnDisable()
    {
        shape.Rebuilt -= Dress;
        DestroyShafts();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= DressIfAlive;
            UnityEditor.EditorApplication.delayCall += DressIfAlive;
        }
#endif
    }

#if UNITY_EDITOR
    private void DressIfAlive()
    {
        if (this != null && isActiveAndEnabled) Dress();
    }
#endif

    private void Dress()
    {
        DestroyShafts();

        if (shaftPrefab == null || shape.OutlinePolygon.Count < 3) return;

        shafts = new GameObject("Shafts") { hideFlags = HideFlags.DontSave };
        shafts.transform.SetParent(transform, false);

        var bounds = shape.Bounds;
        int shapeHash = unchecked(seed * 397) ^ (shape.OutlinePolygon.Count * 31) ^ (Mathf.RoundToInt(bounds.size.x * 100f) * 7) ^ Mathf.RoundToInt(bounds.size.z * 100f);
        var rng = new System.Random(shapeHash);
        int amount = rng.Next(count.x, count.y + 1);
        var sun = RenderSettings.sun;
        bool useSun = alignToSun && sun != null && sun.type == LightType.Directional;

        for (int i = 0; i < amount; i++)
        {
            bool foundPoint = false;
            Vector3 world = default;
            for (int attempt = 0; attempt < 4 && !foundPoint; attempt++)
                foundPoint = shape.TryRandomPoint(rng, margin, out world);

            if (!foundPoint) continue;

            float yaw = (float)(rng.NextDouble() * 360.0);
            float variation = (float)((rng.NextDouble() * 2.0 - 1.0) * spread);
            float shaftScale = (float)(rng.NextDouble() * (scale.y - scale.x) + scale.x);

            Quaternion rotation = useSun
                ? Quaternion.FromToRotation(Vector3.up, -sun.transform.forward) * Quaternion.Euler(variation, yaw, 0f)
                : Quaternion.Euler(tilt + variation, yaw, 0f);

            Spawn(world, rotation, shaftScale);
        }
    }

    private void Spawn(Vector3 position, Quaternion rotation, float scale)
    {
        var instance = Instantiate(shaftPrefab, position, rotation, shafts.transform);
        instance.transform.localScale = Vector3.one * scale;
        instance.hideFlags = HideFlags.DontSave;

        var colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            DestroyImmediate(colliders[i]);
    }

    private void DestroyShafts()
    {
        if (shafts != null)
        {
            DestroyImmediate(shafts);
            shafts = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "Shafts") DestroyImmediate(child.gameObject);
        }
    }
}
}
