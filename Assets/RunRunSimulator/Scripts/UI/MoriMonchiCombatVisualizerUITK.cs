using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class MoriMonchiCombatVisualizerUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private bool hideForDebug = false;
    [SerializeField, Range(0.05f, 2f)] private float fillLerpSeconds = 0.4f;
    [SerializeField] private bool uprightOnly = true;
    [SerializeField] private CombatPopupPaletteSO palette;
    [SerializeField, Range(0.5f, 6f)] private float reactionFlashSeconds = 2f;

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
    private VisualElement statesNegativeBox;
    private VisualElement statesPositiveBox;
    private VisualElement shieldTrack;
    private VisualElement shieldFill;
    private Label         reactionRow;
    private float         targetPct   = 1f;
    private float         currentPct  = 1f;
    private float         maxHp       = 1f;
    private string        desiredName = "?";
    private string        desiredAtk  = "";
    private string        desiredSpd  = "";
    private Role           desiredRole;
    private string         desiredElementName  = "";
    private Color          desiredElementColor = Color.white;
    private float          desiredShield;
    private string         desiredReactionText  = "";
    private Color          desiredReactionColor = Color.white;
    private float          reactionUntil;
    private bool           reactionVisible;
    private List<CombatStatusMark> desiredStatus = new List<CombatStatusMark>();
    private List<ElementChipData>  desiredMarks  = new List<ElementChipData>();
    private List<ElementChipData>  desiredArmed  = new List<ElementChipData>();
    private bool          staticDirty;
    private bool          statusDirty;
    private bool          elementStateDirty;
    private bool          shieldDirty;
    private bool          reactionDirty;
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
        desiredShield = 0f;
        shieldDirty   = true;
        reactionUntil  = 0f;
        reactionVisible = false;
        if (reactionRow != null) reactionRow.style.display = DisplayStyle.None;
        Apply();
    }

    public void SetHp(float current, float max)
    {
        maxHp     = Mathf.Max(0f, max);
        targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
    }

    public void SetShield(float shield)
    {
        desiredShield = shield;
        shieldDirty   = true;
    }

    public void FlashReaction(string text, Color color)
    {
        desiredReactionText  = text;
        desiredReactionColor = color;
        reactionUntil = Time.time + reactionFlashSeconds;
        reactionDirty = true;
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

        shieldTrack = root.Q<VisualElement>("shield-track");
        if (shieldTrack == null && fill != null)
        {
            var track = fill.parent;
            var trackParent = track != null ? track.parent : null;
            if (track != null && trackParent != null)
            {
                shieldTrack = new VisualElement { name = "shield-track" };
                shieldTrack.style.height          = 4;
                shieldTrack.style.width           = Length.Percent(100f);
                shieldTrack.style.marginBottom    = 1;
                shieldTrack.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);

                shieldFill = new VisualElement { name = "shield-fill" };
                shieldFill.style.height          = Length.Percent(100f);
                shieldFill.style.width           = Length.Percent(0f);
                shieldFill.style.backgroundColor = new Color(90f / 255f, 160f / 255f, 255f / 255f);
                shieldTrack.Add(shieldFill);

                int trackIdx = trackParent.IndexOf(track);
                trackParent.Insert(trackIdx, shieldTrack);
            }
        }
        else if (shieldTrack != null)
        {
            shieldFill = shieldTrack.Q<VisualElement>("shield-fill");
        }

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

        statesNegativeBox = armedRow.Q<VisualElement>("states-negative");
        if (statesNegativeBox == null)
        {
            statesNegativeBox = new VisualElement { name = "states-negative" };
            statesNegativeBox.style.flexDirection  = FlexDirection.Column;
            statesNegativeBox.style.alignItems     = Align.Center;
            statesNegativeBox.style.backgroundColor = new Color(140f / 255f, 24f / 255f, 24f / 255f, 0.75f);
            statesNegativeBox.style.borderTopColor    = new Color(1f, 90f / 255f, 90f / 255f);
            statesNegativeBox.style.borderBottomColor = new Color(1f, 90f / 255f, 90f / 255f);
            statesNegativeBox.style.borderLeftColor   = new Color(1f, 90f / 255f, 90f / 255f);
            statesNegativeBox.style.borderRightColor  = new Color(1f, 90f / 255f, 90f / 255f);
            statesNegativeBox.style.borderTopWidth    = 1;
            statesNegativeBox.style.borderBottomWidth = 1;
            statesNegativeBox.style.borderLeftWidth   = 1;
            statesNegativeBox.style.borderRightWidth  = 1;
            statesNegativeBox.style.borderTopLeftRadius     = 3;
            statesNegativeBox.style.borderTopRightRadius    = 3;
            statesNegativeBox.style.borderBottomLeftRadius  = 3;
            statesNegativeBox.style.borderBottomRightRadius = 3;
            statesNegativeBox.style.paddingTop    = 2;
            statesNegativeBox.style.paddingBottom = 2;
            statesNegativeBox.style.paddingLeft   = 2;
            statesNegativeBox.style.paddingRight  = 2;
            statesNegativeBox.style.marginRight   = 2;
            statesNegativeBox.style.display       = DisplayStyle.None;
            armedRow.Add(statesNegativeBox);
        }

        statesPositiveBox = armedRow.Q<VisualElement>("states-positive");
        if (statesPositiveBox == null)
        {
            statesPositiveBox = new VisualElement { name = "states-positive" };
            statesPositiveBox.style.flexDirection  = FlexDirection.Column;
            statesPositiveBox.style.alignItems     = Align.Center;
            statesPositiveBox.style.backgroundColor = new Color(24f / 255f, 110f / 255f, 40f / 255f, 0.75f);
            statesPositiveBox.style.borderTopColor    = new Color(90f / 255f, 220f / 255f, 110f / 255f);
            statesPositiveBox.style.borderBottomColor = new Color(90f / 255f, 220f / 255f, 110f / 255f);
            statesPositiveBox.style.borderLeftColor   = new Color(90f / 255f, 220f / 255f, 110f / 255f);
            statesPositiveBox.style.borderRightColor  = new Color(90f / 255f, 220f / 255f, 110f / 255f);
            statesPositiveBox.style.borderTopWidth    = 1;
            statesPositiveBox.style.borderBottomWidth = 1;
            statesPositiveBox.style.borderLeftWidth   = 1;
            statesPositiveBox.style.borderRightWidth  = 1;
            statesPositiveBox.style.borderTopLeftRadius     = 3;
            statesPositiveBox.style.borderTopRightRadius    = 3;
            statesPositiveBox.style.borderBottomLeftRadius  = 3;
            statesPositiveBox.style.borderBottomRightRadius = 3;
            statesPositiveBox.style.paddingTop    = 2;
            statesPositiveBox.style.paddingBottom = 2;
            statesPositiveBox.style.paddingLeft   = 2;
            statesPositiveBox.style.paddingRight  = 2;
            statesPositiveBox.style.display       = DisplayStyle.None;
            armedRow.Add(statesPositiveBox);
        }

        reactionRow = root.Q<Label>("reaction-row");
        if (reactionRow == null)
        {
            reactionRow = new Label { name = "reaction-row" };
            reactionRow.style.fontSize                = 10;
            reactionRow.style.unityFontStyleAndWeight  = FontStyle.Bold;
            reactionRow.style.unityTextAlign           = TextAnchor.MiddleCenter;
            reactionRow.style.display                  = DisplayStyle.None;
            int armedIdx = root.IndexOf(armedRow);
            if (armedIdx >= 0) root.Insert(armedIdx + 1, reactionRow);
            else root.Add(reactionRow);
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
        shieldDirty       = true;
        return nameLabel != null && fill != null;
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (!EnsureRefs()) return;
        if (root != null) root.style.display = hideForDebug ? DisplayStyle.None : DisplayStyle.Flex;
        if (hideForDebug) return;
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
        {
            hpValueLabel.text = desiredShield >= 0.5f
                ? $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}  +{Mathf.RoundToInt(desiredShield)}"
                : $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
        }
        if (shieldDirty)
        {
            if (shieldFill != null)
                shieldFill.style.width = Length.Percent(Mathf.Clamp01(maxHp > 0f ? desiredShield / maxHp : 0f) * 100f);
            if (shieldTrack != null)
                shieldTrack.style.display = desiredShield >= 0.5f ? DisplayStyle.Flex : DisplayStyle.None;
            shieldDirty = false;
        }
        if (reactionDirty && reactionRow != null)
        {
            reactionRow.text        = desiredReactionText;
            reactionRow.style.color = desiredReactionColor.a <= 0f ? Color.white : desiredReactionColor;
            reactionRow.style.display = DisplayStyle.Flex;
            reactionVisible = true;
            reactionDirty   = false;
        }
        if (reactionVisible && Time.time >= reactionUntil)
        {
            if (reactionRow != null) reactionRow.style.display = DisplayStyle.None;
            reactionVisible = false;
        }
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
            if (statesNegativeBox != null && statesPositiveBox != null)
            {
                statesNegativeBox.Clear();
                statesPositiveBox.Clear();
                foreach (var armed in desiredArmed)
                {
                    var label = BuildStateLabel(armed);
                    if (armed.Negative) statesNegativeBox.Add(label);
                    else                statesPositiveBox.Add(label);
                }
                statesNegativeBox.style.display = statesNegativeBox.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                statesPositiveBox.style.display = statesPositiveBox.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
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

    private static Label BuildStateLabel(ElementChipData data)
    {
        var label = new Label(data.Label);
        label.style.fontSize                = 9;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.color                   = Color.white;
        label.style.unityTextAlign          = TextAnchor.MiddleCenter;
        return label;
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
