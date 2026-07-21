using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
public class MonchiLivePortrait : MonoBehaviour
{
    public static MonchiLivePortrait Instance { get; private set; }

    [Required, SerializeField] private Camera liveCamera;
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float framePadding = 0.9f;
    [SerializeField] private float cameraPitch = 12f;
    [SerializeField] private float cameraYaw = 155f;
    [SerializeField] private float followDamp = 8f;

    private RenderTexture rt;
    private VisualElement element;
    private CreatureDNA dna;
    private Transform target;
    private Transform modelRoot;
    private readonly List<(Transform t, int layer)> originalLayers = new();
    private int focusLayer = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        liveCamera.targetTexture = rt;
        liveCamera.enabled = false;

        focusLayer = LayerMask.NameToLayer("MonchiFocus");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }

    public bool Begin(VisualElement portraitElement, CreatureDNA portraitDna)
    {
        if (portraitElement == null || portraitDna == null || liveCamera == null)
            return false;

        if (MoriMochiSpawner.Instance == null)
            return false;

        Transform foundTarget = null;
        Transform foundModelRoot = null;
        foreach (var kv in MoriMochiSpawner.Instance.SpawnedEntries)
        {
            if (kv.Key != portraitDna.UniqueID)
                continue;

            if (kv.Value == null || !kv.Value.gameObject.activeInHierarchy)
                continue;

            foundTarget = kv.Value.transform;
            foundModelRoot = kv.Value.Visualizer != null ? kv.Value.Visualizer.ModelRoot : null;
            break;
        }

        if (foundTarget == null)
            return false;

        if (element != null)
            End();

        element = portraitElement;
        dna = portraitDna;
        target = foundTarget;
        modelRoot = foundModelRoot;

        if (focusLayer >= 0 && modelRoot != null)
        {
            foreach (var t in modelRoot.GetComponentsInChildren<Transform>(true))
            {
                originalLayers.Add((t, t.gameObject.layer));
                t.gameObject.layer = focusLayer;
            }
        }

        liveCamera.enabled = true;
        UpdateCameraTransform(1f);

        portraitElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        portraitElement.style.backgroundColor = Color.clear;
        portraitElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

        return true;
    }

    public void End()
    {
        if (liveCamera != null)
            liveCamera.enabled = false;

        if (element != null && element.panel != null)
            MonchiPortraitUI.Apply(element, dna);

        RestoreLayers();

        element = null;
        dna = null;
        target = null;
        modelRoot = null;
    }

    private void RestoreLayers()
    {
        foreach (var entry in originalLayers)
            if (entry.t != null)
                entry.t.gameObject.layer = entry.layer;

        originalLayers.Clear();
    }

    private void LateUpdate()
    {
        if (element == null)
            return;

        if (element.panel == null
            || IsHidden(element)
            || target == null
            || !target.gameObject.activeInHierarchy
            || modelRoot == null)
        {
            End();
            return;
        }

        UpdateCameraTransform(Time.deltaTime * followDamp);
    }

    private static bool IsHidden(VisualElement ve)
    {
        for (var p = ve; p != null; p = p.hierarchy.parent)
            if (p.resolvedStyle.display == DisplayStyle.None)
                return true;
        return false;
    }

    private void UpdateCameraTransform(float lerpT)
    {
        var renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Bounds bounds;
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }
        else
        {
            bounds = new Bounds(target.position + Vector3.up * 0.5f, Vector3.one);
        }

        float radius = bounds.extents.magnitude * framePadding;
        float dist = radius / Mathf.Sin(liveCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = target.rotation * Quaternion.Euler(cameraPitch, cameraYaw, 0f) * Vector3.forward;
        Vector3 wanted = bounds.center - dir * dist;

        liveCamera.transform.position = Vector3.Lerp(liveCamera.transform.position, wanted, lerpT);
        liveCamera.transform.rotation = Quaternion.LookRotation(bounds.center - liveCamera.transform.position, Vector3.up);
    }
}
}
