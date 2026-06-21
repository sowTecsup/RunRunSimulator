using System;
namespace MoriMonchiSimulator
{

// One placed furniture instance — the persistent record (analogue of CreatureDNA).
// Lightweight by design: the world is rebuilt from these. The registry keys it by
// its anchor cell, so a cell holds at most one piece.
[Serializable]
public class PlacedFurniture
{
    public string DefId = "";   // FurnitureDefinitionSO.Id — resolves prefab + footprint
    public int    CellX;        // anchor cell (min corner) on the PlacementGrid
    public int    CellY;
    public int    Rotation;     // yaw in degrees: 0 / 90 / 180 / 270

    public string CellKey => Key(CellX, CellY);
    public static string Key(int x, int y) => $"{x}_{y}";
}
}
