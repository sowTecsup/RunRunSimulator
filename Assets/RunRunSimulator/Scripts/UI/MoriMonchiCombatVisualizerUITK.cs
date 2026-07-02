using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class MoriMonchiCombatVisualizerUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField, Range(0.05f, 2f)] private float fillLerpSeconds = 0.4f;
    [SerializeField] private bool uprightOnly = true;
    [SerializeField] private CombatPopupPaletteSO palette;

    private VisualElement root;
    private Label         nameLabel;
    private Label         hpValueLabel;
    private Label         atkLabel;
    private Label         spdLabel;
    private VisualElement fill;
    private VisualElement effectsRow;
    private float         targetPct   = 1f;
    private float         currentPct  = 1f;
    private float         maxHp       = 1f;
    private string        desiredName = "?";
    private string        desiredAtk  = "";
    private string        desiredSpd  = "";
    private List<CombatStatusMark> desiredStatus = new List<CombatStatusMark>();
    private bool          staticDirty;
    private bool          statusDirty;
    private Transform     cam;

    public void Bind(string displayName, float attack, float speed)
    {
        desiredName = displayName;
        desiredAtk  = $"ATK {Mathf.RoundToInt(attack)}";
        desiredSpd  = $"VEL {Mathf.RoundToInt(speed)}";
        staticDirty = true;
        targetPct   = 1f;
        currentPct  = 1f;
        Apply();
    }

    public void SetHp(float current, float max)
    {
        maxHp     = Mathf.Max(0f, max);
        targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
    }

    public void SetStatus(List<CombatStatusMark> marks)
    {
        desiredStatus = marks ?? new List<CombatStatusMark>();
        statusDirty   = true;
    }

    private bool EnsureRefs()
    {
        if (document == null) document = GetComponentInChildren<UIDocument>(true);
        if (document == null) return false;
        var docRoot = document.rootVisualElement;
        if (docRoot == null) return false;
        if (docRoot == root && nameLabel != null && fill != null) return true;

        root         = docRoot;
        nameLabel    = root.Q<Label>("name");
        fill         = root.Q<VisualElement>("fill");
        hpValueLabel = root.Q<Label>("hp-value");
        atkLabel     = root.Q<Label>("atk");
        spdLabel     = root.Q<Label>("spd");
        effectsRow   = root.Q<VisualElement>("effects");
        if (effectsRow == null)
        {
            effectsRow = new VisualElement { name = "effects" };
            effectsRow.style.flexDirection = FlexDirection.Row;
            effectsRow.style.flexWrap      = Wrap.Wrap;
            effectsRow.style.marginTop     = 2;
            root.Add(effectsRow);
        }
        staticDirty  = true;
        statusDirty  = true;
        return nameLabel != null && fill != null;
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (!EnsureRefs()) return;
        if (staticDirty)
        {
            if (nameLabel != null) nameLabel.text = desiredName;
            if (atkLabel  != null) atkLabel.text  = desiredAtk;
            if (spdLabel  != null) spdLabel.text  = desiredSpd;
            staticDirty = false;
        }
        if (!Mathf.Approximately(currentPct, targetPct))
        {
            float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
            currentPct = Mathf.Lerp(currentPct, targetPct, t);
        }
        if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
        if (hpValueLabel != null)
            hpValueLabel.text = $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
        if (statusDirty && effectsRow != null)
        {
            effectsRow.Clear();
            foreach (var mark in desiredStatus)
            {
                var chip = new Label(StatusText(mark));
                chip.style.fontSize          = 10;
                chip.style.unityFontStyleAndWeight = FontStyle.Bold;
                chip.style.paddingTop        = 1;
                chip.style.paddingBottom     = 1;
                chip.style.paddingLeft       = 3;
                chip.style.paddingRight      = 3;
                chip.style.marginRight       = 2;
                chip.style.borderTopLeftRadius     = 3;
                chip.style.borderTopRightRadius    = 3;
                chip.style.borderBottomLeftRadius  = 3;
                chip.style.borderBottomRightRadius = 3;
                chip.style.backgroundColor   = new Color(0f, 0f, 0f, 0.55f);
                chip.style.color             = palette != null ? palette.GetColor(MapKind(mark.Kind)) : Color.white;
                effectsRow.Add(chip);
            }
            statusDirty = false;
        }
    }

    private static string StatusText(CombatStatusMark mark)
    {
        string initial = StatusInitial(mark.Kind);
        return mark.Stacks > 1 ? $"{initial}×{mark.Stacks}" : initial;
    }

    private static string StatusInitial(ModifierEffectKind kind)
    {
        switch (kind)
        {
            case ModifierEffectKind.Poison:       return "V";
            case ModifierEffectKind.Burn:         return "Q";
            case ModifierEffectKind.Regen:        return "R";
            case ModifierEffectKind.Stun:         return "A";
            case ModifierEffectKind.ReturnDamage: return "E";
            case ModifierEffectKind.Heal:         return "C";
            default:                              return kind.ToString().Substring(0, 1);
        }
    }

    private static CombatPopupKind MapKind(ModifierEffectKind kind)
    {
        switch (kind)
        {
            case ModifierEffectKind.Poison:       return CombatPopupKind.Poison;
            case ModifierEffectKind.Burn:         return CombatPopupKind.Burn;
            case ModifierEffectKind.Regen:        return CombatPopupKind.Regen;
            case ModifierEffectKind.Stun:         return CombatPopupKind.Stun;
            case ModifierEffectKind.ReturnDamage: return CombatPopupKind.Thorns;
            case ModifierEffectKind.Heal:         return CombatPopupKind.Heal;
            case ModifierEffectKind.Synergy:      return CombatPopupKind.Synergy;
            default:                              return CombatPopupKind.Hit;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }
        Vector3 toCam = transform.position - cam.position;
        if (uprightOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam);
    }
}
}
