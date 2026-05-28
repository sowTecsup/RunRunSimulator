using System;
using System.Collections.Generic;

[Serializable]
public class CombatResult
{
    public string       WinnerID;
    public string       LoserID;
    public bool         LoserDied;
    public bool         WinnerEvolved;
    public string       EvolvedSlot;       // "Body" | "Arm" | "Eye" | "Mouth" | null
    public List<string> Log = new List<string>();

    public string Summary =>
        $"Winner: {WinnerID[..Math.Min(12, WinnerID.Length)]}... " +
        $"{(WinnerEvolved  ? $"[EVOLVED {EvolvedSlot}] " : "")}" +
        $"{(LoserDied      ? "[LOSER DIED]"             : "")}";
}
