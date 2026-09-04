using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ArenaRoster", menuName = "RunRunSimulator/Expedition/Arena Roster")]
public class ArenaRosterSO : SerializedScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string Name = "";
        public ExpeditionTeam Team = ExpeditionTeam.Player;
        [Range(0f, 1f)] public float Sociability = 0.5f;
        [Range(0f, 1f)] public float Boldness = 0.5f;
        public string BodyShapeID = "";
        public Color BaseColor = new Color(0f, 0f, 0f, 0f);
    }

    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true)]
    public List<Entry> Entries = new List<Entry>();

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    public void PopulateDefaults()
    {
        if (Entries == null) Entries = new List<Entry>();

        if (Entries.Count == 0)
        {
            Entries.Add(new Entry { Name = "Osado", Team = ExpeditionTeam.Player, Sociability = 0.25f, Boldness = 0.9f });
            Entries.Add(new Entry { Name = "Tímida", Team = ExpeditionTeam.Player, Sociability = 0.85f, Boldness = 0.15f });
            Entries.Add(new Entry { Name = "Equilibrado", Team = ExpeditionTeam.Player, Sociability = 0.5f, Boldness = 0.5f });
            Entries.Add(new Entry { Name = "Fiero", Team = ExpeditionTeam.Rival, Sociability = 0.25f, Boldness = 0.9f });
            Entries.Add(new Entry { Name = "Cauta", Team = ExpeditionTeam.Rival, Sociability = 0.85f, Boldness = 0.15f });
            Entries.Add(new Entry { Name = "Templado", Team = ExpeditionTeam.Rival, Sociability = 0.5f, Boldness = 0.5f });
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
