using System;
namespace MoriMonchiSimulator
{

[Serializable]
public class PlacedFurniture
{
    public string DefId = "";
    public int    CellX;
    public int    CellY;
    public int    Rotation;

    public string CellKey => Key(CellX, CellY);
    public static string Key(int x, int y) => $"{x}_{y}";
}
}
