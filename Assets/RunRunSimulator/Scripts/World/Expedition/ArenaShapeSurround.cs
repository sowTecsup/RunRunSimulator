using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaShapeSurround : MonoBehaviour
{
    [Title("Semilla")]
    [SerializeField] private int seed = 7;

    [Title("Bosque")]
    [SerializeField] private List<GameObject> treePrefabs = new();
    [SerializeField] private List<GameObject> bushPrefabs = new();
    [SerializeField] private List<GameObject> rockPrefabs = new();
    [SerializeField, Min(1)] private int rings = 5;
    [SerializeField, Min(0f)] private float ringStart = 3f;
    [SerializeField, Min(0f)] private float ringStep = 3.5f;
    [SerializeField, Min(0.5f)] private float stepAlong = 3f;
    [SerializeField, Range(0f, 1f)] private float jitter = 0.5f;
    [SerializeField, Range(0f, 1f)] private float rockChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float bushChance = 0.35f;
    [SerializeField] private Vector2 treeScale = new Vector2(1f, 1.7f);
    [SerializeField] private Vector2 bushScale = new Vector2(0.9f, 1.5f);
    [SerializeField] private Vector2 rockScale = new Vector2(0.7f, 1.6f);
    [SerializeField] private float drop = 0.05f;

    private ArenaShape shape;
    private GameObject surround;

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();
        shape.Rebuilt += Dress;
        Dress();
    }

    private void OnDisable()
    {
        shape.Rebuilt -= Dress;
        DestroySurround();
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
        DestroySurround();

        if (shape.OutlinePolygon.Count < 3) return;
        if (treePrefabs.Count == 0 && bushPrefabs.Count == 0 && rockPrefabs.Count == 0) return;

        surround = new GameObject("Surround") { hideFlags = HideFlags.DontSave };
        surround.transform.SetParent(transform, false);

        var rng = new System.Random(seed);

        for (int i = 0; i < rings; i++)
        {
            float distance = ringStart + i * ringStep;
            float step = stepAlong * (1f + i * 0.25f);
            var points = ArenaShapeScatter.AlongEdge(shape.OutlinePolygon, rng, step, -distance, jitter);

            foreach (var point in points)
            {
                bool wantsRock = rockPrefabs.Count > 0 && rng.NextDouble() < rockChance;
                bool wantsBush = !wantsRock && bushPrefabs.Count > 0 && rng.NextDouble() < bushChance;

                List<GameObject> prefabs;
                Vector2 scaleRange;

                if (wantsRock)
                {
                    prefabs = rockPrefabs;
                    scaleRange = rockScale;
                }
                else if (wantsBush)
                {
                    prefabs = bushPrefabs;
                    scaleRange = bushScale;
                }
                else if (treePrefabs.Count > 0)
                {
                    prefabs = treePrefabs;
                    scaleRange = treeScale;
                }
                else if (bushPrefabs.Count > 0)
                {
                    prefabs = bushPrefabs;
                    scaleRange = bushScale;
                }
                else
                {
                    prefabs = rockPrefabs;
                    scaleRange = rockScale;
                }

                var prefab = prefabs[rng.Next(prefabs.Count)];
                float scale = (float)(rng.NextDouble() * (scaleRange.y - scaleRange.x) + scaleRange.x);
                float yaw = (float)(rng.NextDouble() * 360.0);
                var world = new Vector3(point.x, shape.Center.y - drop, point.y);

                Spawn(prefab, world, yaw, scale);
            }
        }
    }

    private void Spawn(GameObject prefab, Vector3 position, float yaw, float scale)
    {
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), surround.transform);
        instance.transform.localScale = Vector3.one * scale;
        instance.hideFlags = HideFlags.DontSave;

        var colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            DestroyImmediate(colliders[i]);
    }

    private void DestroySurround()
    {
        if (surround != null)
        {
            DestroyImmediate(surround);
            surround = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "Surround") DestroyImmediate(child.gameObject);
        }
    }
}
}
