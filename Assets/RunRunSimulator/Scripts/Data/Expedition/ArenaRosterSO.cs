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
        public Role Role = Role.Protector;
        [Range(0f, 1f)] public float Sociability = 0.5f;
        [Range(0f, 1f)] public float Boldness = 0.5f;
        public string BodyShapeID = "";
        public string HornID = "";
        public string BackID = "";
        public string WingID = "";
        public Color BaseColor = new Color(0f, 0f, 0f, 0f);
        public Occupation Occupation = Occupation.Gather;
    }

    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true)]
    public List<Entry> Entries = new List<Entry>();

    [Button("Populate Defaults", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    public void PopulateDefaults()
    {
        if (Entries == null) Entries = new List<Entry>();

        if (Entries.Count == 0)
        {
            Entries.Add(new Entry { Name = "Osado", Team = ExpeditionTeam.Player, Role = Role.Agresivo, Sociability = 0.25f, Boldness = 0.9f, Occupation = Occupation.Guard });
            Entries.Add(new Entry { Name = "Tímida", Team = ExpeditionTeam.Player, Role = Role.Empatico, Sociability = 0.85f, Boldness = 0.15f, Occupation = Occupation.Gather });
            Entries.Add(new Entry { Name = "Equilibrado", Team = ExpeditionTeam.Player, Role = Role.Protector, Sociability = 0.5f, Boldness = 0.5f, Occupation = Occupation.Gather });
            Entries.Add(new Entry { Name = "Fiero", Team = ExpeditionTeam.Rival, Role = Role.Agresivo, Sociability = 0.25f, Boldness = 0.9f, Occupation = Occupation.Break });
            Entries.Add(new Entry { Name = "Cauta", Team = ExpeditionTeam.Rival, Role = Role.Empatico, Sociability = 0.85f, Boldness = 0.15f, Occupation = Occupation.Gather });
            Entries.Add(new Entry { Name = "Templado", Team = ExpeditionTeam.Rival, Role = Role.Protector, Sociability = 0.5f, Boldness = 0.5f, Occupation = Occupation.Gather });
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
