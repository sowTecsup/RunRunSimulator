using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "RarityOddsTable", menuName = "RunRunSimulator/Rarity Odds Table")]
public class RarityOddsTableSO : SerializedScriptableObject
{
    [InfoBox("Weights are relative — they are normalized internally. " +
             "A weight of 0 means that rarity never appears.")]
    [DictionaryDrawerSettings(KeyLabel = "Rarity", ValueLabel = "Weight")]
    [OdinSerialize]
    private Dictionary<Rarity, float> _weights = new Dictionary<Rarity, float>
    {
        { Rarity.Common,    60f },
        { Rarity.Uncommon,  25f },
        { Rarity.Rare,      10f },
        { Rarity.Epic,       4f },
        { Rarity.Legendary,  1f },
    };

    [ShowInInspector, ReadOnly, MultiLineProperty(4)]
    [LabelText("Effective %")]
    private string EffectiveOdds
    {
        get
        {
            float total = _weights.Values.Sum(w => Mathf.Max(0f, w));
            if (total <= 0f) return "No weights set.";
            var sb = new StringBuilder();
            foreach (var kvp in _weights)
                sb.AppendLine($"{kvp.Key,-14}  {Mathf.Max(0f, kvp.Value) / total * 100f:F1}%");
            return sb.ToString().TrimEnd();
        }
    }

    // Returns a Rarity sampled from the weighted distribution.
    public Rarity Roll()
    {
        float total = _weights.Values.Sum(w => Mathf.Max(0f, w));
        if (total <= 0f) return Rarity.Common;

        float roll       = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var kvp in _weights)
        {
            cumulative += Mathf.Max(0f, kvp.Value);
            if (roll < cumulative) return kvp.Key;
        }

        return Rarity.Common;
    }
}
