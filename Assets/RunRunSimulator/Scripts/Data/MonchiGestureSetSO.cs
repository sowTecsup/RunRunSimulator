using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "MonchiGestureSet", menuName = "RunRunSimulator/Monchi Gesture Set")]
public class MonchiGestureSetSO : SerializedScriptableObject
{
    [Serializable]
    public class Fidget
    {
        public string State;
        [Min(0f)] public float Weight = 1f;
        [Range(0f, 1f)] public float MinBoldness = 0f;
    }

    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Intent", ValueLabel = "Enter Gesture")]
    private Dictionary<CreatureIntent, string> enterGestures = new Dictionary<CreatureIntent, string>();

    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Intent", ValueLabel = "Hold Gesture")]
    private Dictionary<CreatureIntent, string> holdGestures = new Dictionary<CreatureIntent, string>();

    public string SickGesture = "Sick";

    public Vector2 FidgetInterval = new Vector2(4f, 9f);

    public List<Fidget> Fidgets = new List<Fidget>();

    public bool TryEnterGesture(CreatureIntent intent, out string state)
    {
        if (enterGestures != null && enterGestures.TryGetValue(intent, out state) && !string.IsNullOrEmpty(state))
            return true;

        state = "";
        return false;
    }

    public bool TryHoldGesture(CreatureIntent intent, out string state)
    {
        if (holdGestures != null && holdGestures.TryGetValue(intent, out state) && !string.IsNullOrEmpty(state))
            return true;

        state = "";
        return false;
    }

    public string PickFidget(float boldness)
    {
        if (Fidgets == null || Fidgets.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var fidget in Fidgets)
            if (fidget != null && fidget.MinBoldness <= boldness)
                totalWeight += Mathf.Max(0f, fidget.Weight);

        if (totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.value * totalWeight;
        foreach (var fidget in Fidgets)
        {
            if (fidget == null || fidget.MinBoldness > boldness) continue;
            roll -= Mathf.Max(0f, fidget.Weight);
            if (roll <= 0f) return fidget.State;
        }

        return null;
    }

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    public void PopulateDefaults()
    {
        enterGestures ??= new Dictionary<CreatureIntent, string>();
        holdGestures ??= new Dictionary<CreatureIntent, string>();
        Fidgets ??= new List<Fidget>();

        if (!enterGestures.ContainsKey(CreatureIntent.Taking)) enterGestures[CreatureIntent.Taking] = "Eat";
        if (!enterGestures.ContainsKey(CreatureIntent.Fighting)) enterGestures[CreatureIntent.Fighting] = "Roar";
        if (!enterGestures.ContainsKey(CreatureIntent.Losing)) enterGestures[CreatureIntent.Losing] = "No";
        if (!enterGestures.ContainsKey(CreatureIntent.Dazed)) enterGestures[CreatureIntent.Dazed] = "No";

        if (!holdGestures.ContainsKey(CreatureIntent.Resting)) holdGestures[CreatureIntent.Resting] = "Rest";
        if (!holdGestures.ContainsKey(CreatureIntent.SleepingTogether)) holdGestures[CreatureIntent.SleepingTogether] = "Rest";

        if (Fidgets.Count == 0)
        {
            Fidgets.Add(new Fidget { State = "No", Weight = 1f, MinBoldness = 0f });
            Fidgets.Add(new Fidget { State = "Yes", Weight = 1f, MinBoldness = 0f });
            Fidgets.Add(new Fidget { State = "Eat", Weight = 0.6f, MinBoldness = 0f });
            Fidgets.Add(new Fidget { State = "Roar", Weight = 0.8f, MinBoldness = 0.55f });
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
