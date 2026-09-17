using System.Collections.Generic;
using UnityEngine;

namespace Sowtank.MiniShapes
{

public class PathState
{
    public Vector3 ShownEnd;
    public bool HasShown;
    public float Alpha;
    public List<Vector3> Points = new();
    public float DestAlpha;
    public Vector3 LastDestination;
    public bool HasDestination;
}
}
