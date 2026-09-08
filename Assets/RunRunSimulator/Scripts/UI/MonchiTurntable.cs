using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
public class MonchiTurntable : MonoBehaviour
{
    [Required, SerializeField] private MonchiVisualBankSO visualBank;
    [Required, SerializeField] private FurTypeDatabaseSO furDatabase;
    [Min(1), SerializeField] private int slotCount = 3;
    [Min(64), SerializeField] private int textureSize = 320;
    [SerializeField] private float spinDegreesPerSecond = 30f;
    [SerializeField] private float cameraPitch = 12f;
    [SerializeField] private float cameraFov = 30f;
    [SerializeField] private float framePadding = 1.1f;
    [SerializeField] private float slotSpacing = 20f;
    [SerializeField] private float stageHeight = -500f;
    [SerializeField] private string focusLayerName = "MonchiFocus";

    private class Booth
    {
        public GameObject Root;
        public Transform Model;
        public MonchiVisualizer Visualizer;
        public Camera Camera;
        public RenderTexture Texture;
        public VisualElement Element;
        public float Yaw;
        public bool Active;
    }

    private Booth[] booths;

    private void Awake()
    {
        int focusLayer = LayerMask.NameToLayer(focusLayerName);

        booths = new Booth[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            var root = new GameObject($"Booth_{i}");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(i * slotSpacing, stageHeight, 0f);

            var modelGO = new GameObject("Model");
            modelGO.transform.SetParent(root.transform, false);
            var visualizer = modelGO.AddComponent<MonchiVisualizer>();
            visualizer.SetBank(visualBank);
            visualizer.SetFurDatabase(furDatabase);

            var cameraGO = new GameObject("Camera");
            cameraGO.transform.SetParent(root.transform, false);
            var camera = cameraGO.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.fieldOfView = cameraFov;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            var texture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
            camera.targetTexture = texture;
            camera.enabled = false;

            if (focusLayer >= 0)
            {
                camera.cullingMask = 1 << focusLayer;
                root.layer = focusLayer;
                modelGO.layer = focusLayer;
                cameraGO.layer = focusLayer;
            }

            booths[i] = new Booth
            {
                Root = root,
                Model = modelGO.transform,
                Visualizer = visualizer,
                Camera = camera,
                Texture = texture
            };
        }
    }

    public bool Show(int slot, CreatureDNA dna, VisualElement element)
    {
        if (slot < 0 || slot >= booths.Length || dna == null || element == null)
            return false;

        var booth = booths[slot];

        booth.Visualizer.Assemble(dna);
        booth.Visualizer.SetMood(MonchiMood.Neutral);
        if (booth.Visualizer.Animator != null)
            booth.Visualizer.Animator.Play("Idle", 0, 0f);

        int focusLayer = LayerMask.NameToLayer(focusLayerName);
        if (focusLayer >= 0)
        {
            foreach (var t in booth.Model.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = focusLayer;
        }

        booth.Active = true;
        booth.Camera.enabled = true;
        booth.Element = element;
        element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(booth.Texture));
        element.style.backgroundColor = Color.clear;
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

        Frame(booth);

        return true;
    }

    public void Hide(int slot)
    {
        if (slot < 0 || slot >= booths.Length)
            return;

        var booth = booths[slot];
        booth.Camera.enabled = false;
        booth.Active = false;
        booth.Element = null;
    }

    public void HideAll()
    {
        for (int i = 0; i < booths.Length; i++)
            Hide(i);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < booths.Length; i++)
        {
            var booth = booths[i];
            if (!booth.Active)
                continue;

            if (booth.Element.panel == null)
            {
                Hide(i);
                continue;
            }

            booth.Yaw += spinDegreesPerSecond * Time.unscaledDeltaTime;
            booth.Model.localRotation = Quaternion.Euler(0f, booth.Yaw, 0f);
            Frame(booth);
        }
    }

    private void Frame(Booth booth)
    {
        var renderers = booth.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Bounds bounds;
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
        }
        else
        {
            bounds = new Bounds(booth.Model.position + Vector3.up * 0.5f, Vector3.one);
        }

        float radius = bounds.extents.magnitude * framePadding;
        float dist = radius / Mathf.Sin(cameraFov * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = Quaternion.Euler(cameraPitch, 180f, 0f) * Vector3.forward;
        booth.Camera.transform.position = bounds.center - dir * dist;
        booth.Camera.transform.rotation = Quaternion.LookRotation(bounds.center - booth.Camera.transform.position, Vector3.up);
    }

    private void OnDestroy()
    {
        if (booths == null)
            return;

        foreach (var booth in booths)
        {
            if (booth?.Texture == null)
                continue;

            booth.Texture.Release();
            Destroy(booth.Texture);
        }
    }
}
}
