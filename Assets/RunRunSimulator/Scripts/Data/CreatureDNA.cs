using System;
using UnityEngine;

// String format: "BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"  (e.g. "BS1-A4-E2-M3-FF00AA")
// Constraint: part IDs must not contain the '-' character.
[Serializable]
public class CreatureDNA
{
    public string BodyShapeID = "";
    public string ArmID       = "";
    public string EyeID       = "";
    public string MouthID     = "";

    [ColorUsage(false)]
    public Color PrimaryColor = Color.white;

    // Gender is instance metadata — NOT encoded in the DNA string.
    // Determined during breeding based on the parent's battle-index.
    // Wild-caught and generated MoriMonchis default to Unknown.
    public CreatureGender Gender = CreatureGender.Unknown;

    public string ToStringID()
    {
        return $"{BodyShapeID}-{ArmID}-{EyeID}-{MouthID}-{ColorUtility.ToHtmlStringRGB(PrimaryColor)}";
    }

    public static CreatureDNA FromID(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[CreatureDNA] Cannot parse a null or empty ID.");
            return new CreatureDNA();
        }

        // Color hex is always the last 6 chars, preceded by '-'
        int lastDash = id.LastIndexOf('-');
        if (lastDash < 0 || id.Length - lastDash - 1 != 6)
        {
            Debug.LogError($"[CreatureDNA] Invalid ID '{id}'. Expected: BODYSHAPE-ARM-EYE-MOUTH-RRGGBB");
            return new CreatureDNA();
        }

        string colorHex = id.Substring(lastDash + 1);
        string[] parts  = id.Substring(0, lastDash).Split('-');

        if (parts.Length != 4)
        {
            Debug.LogError($"[CreatureDNA] Expected 4 part IDs, got {parts.Length} in '{id}'.");
            return new CreatureDNA();
        }

        var dna = new CreatureDNA
        {
            BodyShapeID = parts[0],
            ArmID       = parts[1],
            EyeID       = parts[2],
            MouthID     = parts[3],
        };

        if (!ColorUtility.TryParseHtmlString("#" + colorHex, out dna.PrimaryColor))
            Debug.LogWarning($"[CreatureDNA] Could not parse color '{colorHex}', defaulting to white.");

        return dna;
    }
}
