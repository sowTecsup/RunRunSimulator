using System;
using System.Collections.Generic;

[Serializable]
public class CombatResult
{
    public string       WinnerID;
    public string       LoserID;
    public bool         LoserDied;
    public bool         WinnerEvolved;
    public bool         IsDraw;
    public string       EvolvedSlot;       // "Body" | "Arm" | "Eye" | "Mouth" | null
    public List<string> Log = new List<string>();

    public string Summary => IsDraw
        ? "DRAW — Max rounds reached. No consequences."
        : $"Winner: {WinnerID[..Math.Min(12, WinnerID.Length)]}... " +
          $"{(WinnerEvolved ? $"[EVOLVED {EvolvedSlot}] " : "")}" +
          $"{(LoserDied     ? "[LOSER DIED]"              : "")}";
}
