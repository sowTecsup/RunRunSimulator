using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "MonchiVisualBank", menuName = "RunRunSimulator/Databases/Monchi Visual Bank")]
public class MonchiVisualBankSO : SerializedScriptableObject
{
    [SerializeField] private List<GameObject> bodies = new List<GameObject>();

    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Body Shape ID", ValueLabel = "Body Override")]
    private Dictionary<string, GameObject> bodyOverrides = new Dictionary<string, GameObject>();

    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private List<Material> gemMaterials = new List<Material>();
    [SerializeField] private MonchiMoodSetSO moodSet;

    public RuntimeAnimatorController AnimatorController => animatorController;
    public MonchiMoodSetSO MoodSet => moodSet;

    public GameObject GetBody(string bodyShapeId)
    {
        if (bodyOverrides != null && bodyOverrides.TryGetValue(bodyShapeId, out var overrideBody) && overrideBody != null)
            return overrideBody;

        if (bodies == null || bodies.Count == 0)
        {
            Debug.LogWarning("[MonchiVisualBankSO] No bodies assigned.");
            return null;
        }

        return bodies[StableHash(bodyShapeId) % bodies.Count];
    }

    public Material GetGem(string uniqueId)
    {
        if (gemMaterials == null || gemMaterials.Count == 0)
            return null;

        return gemMaterials[StableHash(uniqueId) % gemMaterials.Count];
    }

    public static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261u;
            if (!string.IsNullOrEmpty(s))
                foreach (char c in s)
                    h = (h ^ c) * 16777619u;
            return (int)(h & 0x7FFFFFFF);
        }
    }
}
}
