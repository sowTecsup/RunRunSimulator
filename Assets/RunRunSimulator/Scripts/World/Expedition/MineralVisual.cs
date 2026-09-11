using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

public class MineralVisual : MonoBehaviour
{
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    [Title("Refs")]
    [SerializeField, Required] private MaterialPickup pickup;
    [SerializeField, Required] private Transform crystal;
    [SerializeField, Required] private Renderer crystalRenderer;
    [SerializeField] private Light glow;

    [Title("Vida")]
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 1.2f;
    [SerializeField] private float spinDegreesPerSecond = 18f;
    [SerializeField] private float pulseSpeed = 1.6f;
    [SerializeField] private float pulseAmount = 0.35f;

    [Title("Colores")]
    [SerializeField] private Color veinColor = new Color(0.45f, 0.95f, 1f);
    [SerializeField] private Color lodeColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private Color dropColor = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private float emissionStrength = 2.2f;

    [Title("Desgaste")]
    [SerializeField, Range(0f, 1f)] private float minScaleFraction = 0.45f;
    [SerializeField] private float shrinkSmoothing = 6f;

    private MaterialPropertyBlock mpb;
    private Vector3 baseScale;
    private float baseY;
    private float phase;
    private float baseLightIntensity;
    private Renderer[] renderers;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        baseScale = crystal.localScale;
        baseY = crystal.localPosition.y;
        phase = Random.Range(0f, Mathf.PI * 2f);
        if (glow != null) baseLightIntensity = glow.intensity;
        renderers = crystal.GetComponentsInChildren<Renderer>(true);
    }

    private void LateUpdate()
    {
        Color color = pickup.IsLode ? lodeColor : pickup.IsDrop ? dropColor : veinColor;
        float k = 1f + pulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f + phase));

        mpb.Clear();
        mpb.SetColor(BaseColorID, color);
        mpb.SetColor(EmissionColorID, color * emissionStrength * k);
        foreach (var r in renderers) r.SetPropertyBlock(mpb);

        if (glow != null)
        {
            glow.color = color;
            glow.intensity = baseLightIntensity * k;
        }

        Vector3 localPos = crystal.localPosition;
        localPos.y = baseY + Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f + phase) * bobAmplitude;
        crystal.localPosition = localPos;
        crystal.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.Self);

        float fraction = pickup.Value > 0 ? Mathf.Clamp01((float)pickup.Remaining / pickup.Value) : 1f;
        Vector3 targetScale = pickup.Taken
            ? baseScale * (minScaleFraction * 0.5f)
            : baseScale * Mathf.Lerp(minScaleFraction, 1f, fraction);
        crystal.localScale = Vector3.Lerp(crystal.localScale, targetScale, 1f - Mathf.Exp(-shrinkSmoothing * Time.deltaTime));
    }
}
}
