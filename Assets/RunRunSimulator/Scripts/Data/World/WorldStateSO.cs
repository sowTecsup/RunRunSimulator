using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class WorldStateData
{
    public int   Day          = 1;
    public float MinuteOfDay  = 360f;
    public int   TutorialStep = 0;
}

[CreateAssetMenu(fileName = "WorldState", menuName = "RunRunSimulator/World/World State")]
public class WorldStateSO : ScriptableObject
{
    [Title("World State (runtime)")]
    [SerializeField, ReadOnly] private int   day          = 1;
    [SerializeField, ReadOnly] private float minuteOfDay  = 360f;
    [SerializeField, ReadOnly] private int   tutorialStep = 0;

    public int   Day          { get => day;          set => day = value; }
    public float MinuteOfDay  { get => minuteOfDay;  set => minuteOfDay = value; }
    public int   TutorialStep { get => tutorialStep; set => tutorialStep = value; }

    public WorldStateData GetData() => new WorldStateData
    {
        Day          = day,
        MinuteOfDay  = minuteOfDay,
        TutorialStep = tutorialStep,
    };

    public void LoadFrom(WorldStateData data)
    {
        day          = data?.Day ?? 1;
        minuteOfDay  = data?.MinuteOfDay ?? 360f;
        tutorialStep = data?.TutorialStep ?? 0;
    }
}
}
