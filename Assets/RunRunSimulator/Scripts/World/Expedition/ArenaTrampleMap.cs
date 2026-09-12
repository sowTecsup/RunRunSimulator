using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaTrampleMap : MonoBehaviour
{
    private struct StampRequest
    {
        public Vector3 World;
        public float Radius;
        public float Strength;
        public float TrailGain;
    }

    [Title("Shader")]
    [SerializeField] private Shader trampleShader;

    [Title("Resolucion")]
    [SerializeField, Min(64)] private int resolution = 512;
    [SerializeField] private float padding = 4f;

    [Title("Decaimiento")]
    [SerializeField] private float freshHalfLife = 0.35f;
    [SerializeField] private float trailHalfLife = 6f;

    [Title("Radios")]
    [SerializeField] private float creatureRadius = 0.9f;
    [SerializeField] private float mineralRadius = 0.5f;
    [SerializeField] private float impactRadius = 2.5f;

    [Title("Rastro")]
    [SerializeField] private float trailGain = 0.6f;
    [SerializeField] private float stampRate = 8f;

    private static readonly int AreaID = Shader.PropertyToID("_ArenaTrampleArea");
    private static readonly int TrampleTexID = Shader.PropertyToID("_ArenaTrampleTex");
    private static readonly int FreshDecayID = Shader.PropertyToID("_FreshDecay");
    private static readonly int TrailDecayID = Shader.PropertyToID("_TrailDecay");

    public static ArenaTrampleMap Instance { get; private set; }

    private ArenaShape shape;
    private RenderTexture current;
    private RenderTexture scratch;
    private Material decayMaterial;
    private Material stampMaterial;

    private float areaOriginX;
    private float areaOriginZ;
    private float areaSize;
    private float areaInvSize;

    private readonly List<Perceivable> perceiveBuffer = new List<Perceivable>();
    private readonly Dictionary<MoriMochiAgent, float> lastClashHit = new Dictionary<MoriMochiAgent, float>();
    private readonly List<StampRequest> queuedStamps = new List<StampRequest>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PublishDefaults()
    {
        Shader.SetGlobalTexture(TrampleTexID, Texture2D.blackTexture);
        Shader.SetGlobalVector(AreaID, new Vector4(0f, 0f, 1f, 1f));
    }

    public static void Stamp(Vector3 world, float radius, float strength)
    {
        if (Instance == null) return;
        Instance.EnqueueStamp(world, radius, strength, 0f);
    }

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();

        if (trampleShader == null) return;

        Instance = this;
        shape.Rebuilt += RecalculateArea;

        current = CreateRenderTexture();
        scratch = CreateRenderTexture();
        ClearBlack(current);
        ClearBlack(scratch);

        decayMaterial = new Material(trampleShader) { hideFlags = HideFlags.DontSave };
        stampMaterial = new Material(trampleShader) { hideFlags = HideFlags.DontSave };

        RecalculateArea();
    }

    private void OnDisable()
    {
        if (shape != null) shape.Rebuilt -= RecalculateArea;

        lastClashHit.Clear();
        queuedStamps.Clear();

        ReleaseRT(ref current);
        ReleaseRT(ref scratch);

        if (decayMaterial != null) { DestroyImmediate(decayMaterial); decayMaterial = null; }
        if (stampMaterial != null) { DestroyImmediate(stampMaterial); stampMaterial = null; }

        if (Instance == this) Instance = null;

        Shader.SetGlobalTexture(TrampleTexID, Texture2D.blackTexture);
    }

    private void LateUpdate()
    {
        if (current == null) return;

        if (!Application.isPlaying)
        {
            Shader.SetGlobalTexture(TrampleTexID, Texture2D.blackTexture);
            return;
        }

        ApplyDecay(Time.deltaTime);
        CollectPerceivedStamps();
        DrawStamps();

        Shader.SetGlobalTexture(TrampleTexID, current);
    }

    private void RecalculateArea()
    {
        var bounds = shape.Bounds;

        float minX = bounds.min.x - padding;
        float minZ = bounds.min.z - padding;
        float sizeX = (bounds.max.x + padding) - minX;
        float sizeZ = (bounds.max.z + padding) - minZ;

        areaSize = Mathf.Max(sizeX, sizeZ);
        areaInvSize = areaSize > 0f ? 1f / areaSize : 0f;
        areaOriginX = minX - (areaSize - sizeX) * 0.5f;
        areaOriginZ = minZ - (areaSize - sizeZ) * 0.5f;

        Shader.SetGlobalVector(AreaID, new Vector4(areaOriginX, areaOriginZ, areaInvSize, areaInvSize));
        Shader.SetGlobalTexture(TrampleTexID, current);
    }

    private void ApplyDecay(float dt)
    {
        decayMaterial.SetFloat(FreshDecayID, Mathf.Exp(-dt * Mathf.Log(2f) / freshHalfLife));
        decayMaterial.SetFloat(TrailDecayID, Mathf.Exp(-dt * Mathf.Log(2f) / trailHalfLife));

        Graphics.Blit(current, scratch, decayMaterial, 0);

        var swap = current;
        current = scratch;
        scratch = swap;
    }

    private void CollectPerceivedStamps()
    {
        PerceivableRegistry.QueryInRadius(shape.Center, areaSize, null, perceiveBuffer);

        float pressure = Mathf.Min(1f, stampRate * Time.deltaTime);

        for (int i = 0; i < perceiveBuffer.Count; i++)
        {
            var perceivable = perceiveBuffer[i];

            if (perceivable.Monchi != null)
            {
                EnqueueStamp(perceivable.Position, creatureRadius, pressure, trailGain * pressure);
                CollectClashStamp(perceivable.Monchi);
            }
            else
            {
                EnqueueStamp(perceivable.Position, mineralRadius, pressure, 0f);
            }
        }
    }

    private void CollectClashStamp(MoriMochiAgent agent)
    {
        float hitAt = agent.ClashHitAt;

        if (lastClashHit.TryGetValue(agent, out float seen))
        {
            if (hitAt != seen)
            {
                lastClashHit[agent] = hitAt;
                EnqueueStamp(agent.ClashHitPoint, impactRadius, 1f, 1f);
            }
        }
        else
        {
            lastClashHit[agent] = hitAt;
        }
    }

    private void EnqueueStamp(Vector3 world, float radius, float strength, float gain)
    {
        queuedStamps.Add(new StampRequest { World = world, Radius = radius, Strength = strength, TrailGain = gain });
    }

    private void DrawStamps()
    {
        if (queuedStamps.Count == 0) return;

        Graphics.SetRenderTarget(current);
        stampMaterial.SetPass(1);

        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);

        for (int i = 0; i < queuedStamps.Count; i++)
        {
            var stamp = queuedStamps[i];

            float u = (stamp.World.x - areaOriginX) * areaInvSize;
            float v = (stamp.World.z - areaOriginZ) * areaInvSize;
            float ru = stamp.Radius * areaInvSize;

            GL.Color(new Color(stamp.Strength, stamp.TrailGain, 0f, 0f));

            GL.TexCoord2(0f, 0f);
            GL.Vertex3(u - ru, v - ru, 0f);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(u + ru, v - ru, 0f);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(u + ru, v + ru, 0f);
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(u - ru, v + ru, 0f);
        }

        GL.End();
        GL.PopMatrix();
        Graphics.SetRenderTarget(null);

        queuedStamps.Clear();
    }

    private RenderTexture CreateRenderTexture()
    {
        var rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            hideFlags = HideFlags.DontSave
        };
        rt.Create();
        return rt;
    }

    private static void ClearBlack(RenderTexture rt)
    {
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = active;
    }

    private static void ReleaseRT(ref RenderTexture rt)
    {
        if (rt == null) return;
        rt.Release();
        DestroyImmediate(rt);
        rt = null;
    }
}
}
