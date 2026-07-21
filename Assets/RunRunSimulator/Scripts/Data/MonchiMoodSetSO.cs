using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "MonchiMoodSet", menuName = "RunRunSimulator/Monchi Mood Set")]
public class MonchiMoodSetSO : SerializedScriptableObject
{
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Mood", ValueLabel = "Face Materials")]
    private Dictionary<MonchiMood, List<Material>> faces = new Dictionary<MonchiMood, List<Material>>();

    public Material GetFace(MonchiMood mood)
    {
        if (faces != null && faces.TryGetValue(mood, out var list) && list != null && list.Count > 0)
            return list[UnityEngine.Random.Range(0, list.Count)];

        if (faces != null && faces.TryGetValue(MonchiMood.Neutral, out var neutralList) && neutralList != null && neutralList.Count > 0)
            return neutralList[UnityEngine.Random.Range(0, neutralList.Count)];

        return null;
    }

    [Button("Populate from Enum", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromEnum()
    {
        faces ??= new Dictionary<MonchiMood, List<Material>>();
        foreach (MonchiMood mood in Enum.GetValues(typeof(MonchiMood)))
            if (!faces.ContainsKey(mood))
                faces[mood] = new List<Material>();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
