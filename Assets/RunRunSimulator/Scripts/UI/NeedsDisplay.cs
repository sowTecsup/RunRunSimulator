using UnityEngine;
namespace MoriMonchiSimulator
{

public static class NeedsDisplay
{
    public static string ColorClass(NeedType need, float value)
    {
        float f = Fill01(need, value);
        if (f >= 0.6f) return "exp-bar--good";
        if (f >= 0.3f) return "exp-bar--warn";
        return "exp-bar--crit";
    }

    public static float Fill01(NeedType need, float value) =>
        need == NeedType.Affect ? Mathf.Clamp01((value + 100f) / 200f) : Mathf.Clamp01(value / 100f);
}
}
