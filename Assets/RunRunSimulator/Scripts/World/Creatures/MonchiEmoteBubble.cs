using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{

[RequireComponent(typeof(UIDocument))]
public class MonchiEmoteBubble : MonoBehaviour
{
    [Required, SerializeField] private MoriMochiAgent agent;

    [Header("Timing")]
    [Tooltip("Cuanto tiempo queda el emote totalmente visible antes de empezar a desvanecerse.")]
    [SerializeField] private float visibleSeconds = 2f;
    [Tooltip("Duracion del efecto de aparicion (escala y opacidad de 0 a 1).")]
    [SerializeField] private float popSeconds = 0.18f;
    [Tooltip("Duracion del desvanecido final (opacidad de 1 a 0).")]
    [SerializeField] private float fadeSeconds = 0.35f;
    [Tooltip("Tamano de fuente del pictograma.")]
    [SerializeField] private int fontSize = 25;

    private UIDocument document;
    private Label      label;
    private float      shownAt;
    private bool       showing;
    private bool       colliderSilenced;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (agent != null) agent.OnEmote += Show;
        colliderSilenced = false;
        EnsureLabel();
    }

    private void TryDisableAutoCollider()
    {
        if (colliderSilenced) return;
        var col = GetComponent<Collider>();
        if (col == null) return;
        col.enabled      = false;
        colliderSilenced = true;
    }

    private void OnDisable()
    {
        if (agent != null) agent.OnEmote -= Show;
    }

    private void EnsureLabel()
    {
        var tagRoot = document != null ? document.rootVisualElement?.Q<VisualElement>("tag-root") : null;
        if (tagRoot == null) return;
        if (label != null && label.panel != null) return;

        label = new Label();
        label.style.unityTextAlign          = TextAnchor.MiddleCenter;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.fontSize                = fontSize;
        label.style.color                   = Color.white;
        label.style.opacity                 = 0f;
        label.style.height                  = fontSize + 6;
        label.style.minHeight               = fontSize + 6;
        label.pickingMode                   = PickingMode.Ignore;
        tagRoot.Insert(0, label);
    }

    public void Show(EmoteKind kind)
    {
        EnsureLabel();
        TryDisableAutoCollider();
        if (label == null) return;

        var (glyph, color) = GlyphOf(kind);
        label.text        = glyph;
        label.style.color = color;
        shownAt = Time.time;
        showing = true;
    }

    private void LateUpdate()
    {
        TryDisableAutoCollider();
        EnsureLabel();
        if (!showing || label == null) return;

        float elapsed  = Time.time - shownAt;
        float fadeStart = visibleSeconds - fadeSeconds;

        if (elapsed <= popSeconds)
        {
            float t     = popSeconds > 0f ? elapsed / popSeconds : 1f;
            float eased = 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(0.4f, 1f, eased);
            label.style.scale   = new Scale(new Vector3(scale, scale, 1f));
            label.style.opacity = eased;
        }
        else if (elapsed <= fadeStart)
        {
            label.style.scale   = new Scale(Vector3.one);
            label.style.opacity = 1f;
        }
        else if (elapsed <= visibleSeconds)
        {
            float t = fadeSeconds > 0f ? (elapsed - fadeStart) / fadeSeconds : 1f;
            label.style.scale   = new Scale(Vector3.one);
            label.style.opacity = Mathf.Lerp(1f, 0f, t);
        }
        else
        {
            showing             = false;
            label.style.opacity = 0f;
        }
    }

    private static (string, Color) GlyphOf(EmoteKind kind) => kind switch
    {
        EmoteKind.Curioso => ("?",  new Color(1f,    0.85f, 0.3f)),
        EmoteKind.Feliz   => ("☺",  new Color(0.55f, 1f,    0.6f)),
        EmoteKind.Jugando => ("♪",  new Color(0.5f,  0.85f, 1f)),
        EmoteKind.Molesto => ("!",  new Color(1f,    0.35f, 0.3f)),
        EmoteKind.Corazon => ("♥",  new Color(1f,    0.5f,  0.75f)),
        EmoteKind.Zzz     => ("Zz", new Color(0.6f,  0.7f,  0.9f)),
        _                 => ("",  Color.white),
    };
}
}
