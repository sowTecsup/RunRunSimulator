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
    private VisualElement roleElementRow;
    private Label         roleLabel;
    private VisualElement elementChip;
    private Label         elementLabel;
    private VisualElement marksAllyRow;
    private VisualElement armedRow;
    private VisualElement marksEnemyRow;
    private float         targetPct   = 1f;
    private float         currentPct  = 1f;
    private float         maxHp       = 1f;
    private string        desiredName = "?";
    private string        desiredAtk  = "";
    private string        desiredSpd  = "";
    private Role           desiredRole;
    private string         desiredElementName  = "";
    private Color          desiredElementColor = Color.white;
    private List<CombatStatusMark> desiredStatus = new List<CombatStatusMark>();
    private List<ElementChipData>  desiredMarks  = new List<ElementChipData>();
    private List<ElementChipData>  desiredArmed  = new List<ElementChipData>();
    private bool          staticDirty;
    private bool          statusDirty;
    private bool          elementStateDirty;
    private Transform     cam;

    public void Bind(string displayName, float attack, float speed, Role role, string elementName, Color elementColor)
    {
        desiredName         = displayName;
        desiredAtk          = $"ATK {Mathf.RoundToInt(attack)}";
        desiredSpd          = $"VEL {Mathf.RoundToInt(speed)}";
        desiredRole         = role;
        desiredElementName  = elementName;
        desiredElementColor = elementColor.a <= 0f ? Color.white : elementColor;
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

    public void SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)
    {
        desiredMarks      = marks ?? new List<ElementChipData>();
        desiredArmed      = armed ?? new List<ElementChipData>();
        elementStateDirty = true;
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

        roleElementRow = root.Q<VisualElement>("role-element");
        if (roleElementRow == null)
        {
            roleElementRow = new VisualElement { name = "role-element" };
            roleElementRow.style.flexDirection = FlexDirection.Row;
            roleElementRow.style.alignItems    = Align.Center;
            roleElementRow.style.marginTop     = 1;
            roleElementRow.style.marginBottom  = 1;

            roleLabel = new Label { name = "role-label" };
            roleLabel.style.fontSize                = 9;
            roleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            roleLabel.style.color                   = new Color(0.85f, 0.85f, 0.85f);
            roleLabel.style.marginRight             = 4;
            roleElementRow.Add(roleLabel);

            elementChip = new VisualElement { name = "element-chip" };
            elementChip.style.width  = 8;
            elementChip.style.height = 8;
            elementChip.style.marginRight              = 3;
            elementChip.style.borderTopLeftRadius      = 2;
            elementChip.style.borderTopRightRadius     = 2;
            elementChip.style.borderBottomLeftRadius   = 2;
            elementChip.style.borderBottomRightRadius  = 2;
            roleElementRow.Add(elementChip);

            elementLabel = new Label { name = "element-label" };
            elementLabel.style.fontSize = 9;
            roleElementRow.Add(elementLabel);

            if (nameLabel != null)
            {
                int idx = root.IndexOf(nameLabel);
                if (idx >= 0) root.Insert(idx + 1, roleElementRow);
                else root.Add(roleElementRow);
            }
            else
            {
                root.Add(roleElementRow);
            }
        }
        else
        {
            roleLabel    = roleElementRow.Q<Label>("role-label");
            elementChip  = roleElementRow.Q<VisualElement>("element-chip");
            elementLabel = roleElementRow.Q<Label>("element-label");
        }

        marksAllyRow = root.Q<VisualElement>("marks-ally");
        if (marksAllyRow == null)
        {
            marksAllyRow = new VisualElement { name = "marks-ally" };
            marksAllyRow.style.flexDirection  = FlexDirection.Row;
            marksAllyRow.style.flexWrap       = Wrap.Wrap;
            marksAllyRow.style.justifyContent = Justify.Center;
            marksAllyRow.style.marginTop      = 1;
            marksAllyRow.style.marginBottom   = 1;
            root.Insert(0, marksAllyRow);
        }

        armedRow = root.Q<VisualElement>("armed-row");
        if (armedRow == null)
        {
            armedRow = new VisualElement { name = "armed-row" };
            armedRow.style.flexDirection  = FlexDirection.Row;
            armedRow.style.flexWrap       = Wrap.Wrap;
            armedRow.style.justifyContent = Justify.Center;
            armedRow.style.marginTop      = 1;
            armedRow.style.marginBottom   = 1;
            root.Add(armedRow);
        }

        marksEnemyRow = root.Q<VisualElement>("marks-enemy");
        if (marksEnemyRow == null)
        {
            marksEnemyRow = new VisualElement { name = "marks-enemy" };
            marksEnemyRow.style.flexDirection  = FlexDirection.Row;
            marksEnemyRow.style.flexWrap       = Wrap.Wrap;
            marksEnemyRow.style.justifyContent = Justify.Center;
            marksEnemyRow.style.marginTop      = 1;
            marksEnemyRow.style.marginBottom   = 1;
            root.Add(marksEnemyRow);
        }

        staticDirty       = true;
        statusDirty       = true;
        elementStateDirty = true;
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
            if (roleLabel != null) roleLabel.text = RoleLabel(desiredRole);
            if (elementChip != null) elementChip.style.backgroundColor = desiredElementColor;
            if (elementLabel != null)
            {
                elementLabel.text          = desiredElementName;
                elementLabel.style.color   = desiredElementColor;
            }
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
                var chip = new VisualElement();
                chip.style.flexDirection     = FlexDirection.Column;
                chip.style.alignItems        = Align.Center;
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

                var code = new Label(StatusCode(mark.Kind));
                code.style.fontSize                 = 9;
                code.style.unityFontStyleAndWeight  = FontStyle.Bold;
                code.style.color                    = palette != null ? palette.GetColor(MapKind(mark.Kind)) : Color.white;
                chip.Add(code);

                if (mark.Stacks > 1)
                {
                    var count = new Label($"×{mark.Stacks}");
                    count.style.fontSize = 8;
                    count.style.color    = new Color(1f, 1f, 1f, 0.85f);
                    chip.Add(count);
                }
                effectsRow.Add(chip);
            }
            statusDirty = false;
        }
        if (elementStateDirty)
        {
            if (marksAllyRow != null)
            {
                marksAllyRow.Clear();
                foreach (var mark in desiredMarks)
                {
                    if (!mark.AllySource) continue;
                    marksAllyRow.Add(BuildMarkChip(mark, top: true));
                }
            }
            if (armedRow != null)
            {
                armedRow.Clear();
                foreach (var armed in desiredArmed)
                    armedRow.Add(BuildArmedChip(armed));
            }
            if (marksEnemyRow != null)
            {
                marksEnemyRow.Clear();
                foreach (var mark in desiredMarks)
                {
                    if (mark.AllySource) continue;
                    marksEnemyRow.Add(BuildMarkChip(mark, top: false));
                }
            }
            elementStateDirty = false;
        }
    }

    private static string RoleLabel(Role role)
    {
        switch (role)
        {
            case Role.Protector: return "Protector";
            case Role.Agresivo:  return "Agresivo";
            case Role.Empatico:  return "Empático";
            default:             return role.ToString();
        }
    }

    private static VisualElement BuildMarkChip(ElementChipData data, bool top)
    {
        Color c = data.Color.a <= 0f ? Color.white : data.Color;
        var chip = new VisualElement();
        chip.style.paddingTop      = 1;
        chip.style.paddingBottom   = 1;
        chip.style.paddingLeft     = 3;
        chip.style.paddingRight    = 3;
        chip.style.marginLeft      = 1;
        chip.style.marginRight     = 1;
        chip.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        if (top)
        {
            chip.style.borderTopWidth = 2;
            chip.style.borderTopColor = c;
        }
        else
        {
            chip.style.borderBottomWidth = 2;
            chip.style.borderBottomColor = c;
        }

        var label = new Label(data.Label);
        label.style.fontSize                = 8;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.color                   = c;
        chip.Add(label);
        return chip;
    }

    private static VisualElement BuildArmedChip(ElementChipData data)
    {
        Color c = data.Color.a <= 0f ? Color.white : data.Color;
        var chip = new VisualElement();
        chip.style.paddingTop         = 1;
        chip.style.paddingBottom      = 1;
        chip.style.paddingLeft        = 3;
        chip.style.paddingRight       = 3;
        chip.style.marginLeft         = 1;
        chip.style.marginRight        = 1;
        chip.style.backgroundColor    = new Color(0f, 0f, 0f, 0.55f);
        chip.style.borderTopWidth     = 1;
        chip.style.borderBottomWidth  = 1;
        chip.style.borderLeftWidth    = 1;
        chip.style.borderRightWidth   = 1;
        chip.style.borderTopColor     = c;
        chip.style.borderBottomColor  = c;
        chip.style.borderLeftColor    = c;
        chip.style.borderRightColor   = c;

        var label = new Label("★" + data.Label);
        label.style.fontSize                = 8;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.color                   = c;
        chip.Add(label);
        return chip;
    }

    private static string StatusCode(ModifierEffectKind kind)
    {
        switch (kind)
        {
            case ModifierEffectKind.Poison:       return "POI";
            case ModifierEffectKind.Burn:         return "BUR";
            case ModifierEffectKind.Static:       return "STA";
            case ModifierEffectKind.Pulse:        return "PUL";
            case ModifierEffectKind.Steel:        return "STE";
            case ModifierEffectKind.Mist:         return "MIS";
            case ModifierEffectKind.Regen:        return "REG";
            case ModifierEffectKind.Stun:         return "ATU";
            case ModifierEffectKind.ReturnDamage: return "ESP";
            case ModifierEffectKind.Heal:         return "CUR";
            case ModifierEffectKind.Lifesteal:    return "ROB";
            default: return kind.ToString().Substring(0, Mathf.Min(3, kind.ToString().Length)).ToUpperInvariant();
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
            case ModifierEffectKind.Static:       return CombatPopupKind.Static;
            case ModifierEffectKind.Pulse:        return CombatPopupKind.Pulse;
            case ModifierEffectKind.Steel:        return CombatPopupKind.Steel;
            case ModifierEffectKind.Mist:         return CombatPopupKind.Mist;
            case ModifierEffectKind.Lifesteal:    return CombatPopupKind.Lifesteal;
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
