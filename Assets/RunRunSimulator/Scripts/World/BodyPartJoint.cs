using UnityEngine;

// Marks the connection point of a body part prefab.
// Every part prefab (body, arm, eye, mouth) has exactly one of these on its root.
//
// isMirror: when true, MoriMonchiVisualizer instantiates the same prefab on BOTH
//   the L and R sockets for this slot, flipping localScale.x on the right instance.
//   Arms and eyes are typically mirror parts; body and mouth are not.
//
// insertionJoint: child Transform that aligns to the parent socket on attach.
//   Leave null to use this object's own pivot as the connection point.
//
// Future: add rotationMin / rotationMax here for procedural animation constraints.
public class BodyPartJoint : MonoBehaviour
{
    [Tooltip("Instantiate this part on both L and R sockets, mirroring the R instance.")]
    public bool isMirror;

    [Tooltip("Connection point. Null = use this object's pivot.")]
    public Transform insertionJoint;

    public Transform Joint => insertionJoint != null ? insertionJoint : transform;

    // Mirror parts draw in cyan, single parts in yellow — instant visual distinction.
    private void OnDrawGizmos()
    {
        Gizmos.color = isMirror ? new Color(0.4f, 0.8f, 1f) : Color.yellow;
        Gizmos.DrawWireSphere(Joint.position, 0.04f);
    }
}
