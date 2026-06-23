using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class QueueDirectionHandler
{
    public struct Candidate
    {
        public Vector3 Pos;
    }

    public void Candidates(Vector3 anchorPos, Vector3 backAxis, float spacing, List<Candidate> outBuf)
    {
        outBuf.Clear();
        Vector3 back  = backAxis.sqrMagnitude > 0.0001f ? backAxis.normalized : Vector3.forward;
        Vector3 left  = Quaternion.AngleAxis(-90f, Vector3.up) * back;
        Vector3 right = Quaternion.AngleAxis( 90f, Vector3.up) * back;

        outBuf.Add(new Candidate { Pos = anchorPos + back  * spacing });
        outBuf.Add(new Candidate { Pos = anchorPos + left  * spacing });
        outBuf.Add(new Candidate { Pos = anchorPos + right * spacing });
    }
}
}
