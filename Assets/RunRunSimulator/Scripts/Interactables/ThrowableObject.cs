using UnityEngine;

// A Rigidbody object the player can pick up, hold in front of them, and throw.
// Works for the starter cube and anything else with a Rigidbody + Collider.
//
// While held it chases the hold anchor by VELOCITY (not teleport), so it stays
// a real physics body — it bumps into walls and shelves instead of clipping
// through them. Release/throw just hands gravity back and (optionally) adds an
// impulse. The player drives this purely through the IThrowable contract.
[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour, IThrowable
{
    [Tooltip("How snappily the object chases the hold anchor while held.")]
    [SerializeField] private float followSpeed = 15f;

    private Rigidbody rb;
    private Transform holdAnchor;   // non-null only while held

    public bool IsHeld => holdAnchor != null;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public void OnGrab(Transform anchor)
    {
        Debug.Log($"[ThrowableObject] OnGrab '{name}' — anchor={(anchor != null ? anchor.name : "NULL")}");
        holdAnchor          = anchor;
        rb.useGravity       = false;
        rb.angularVelocity  = Vector3.zero;
    }

    public void OnRelease()
    {
        holdAnchor    = null;
        rb.useGravity = true;
    }

    public void OnThrow(Vector3 force)
    {
        Debug.Log($"[ThrowableObject] OnThrow '{name}' — force={force} (mag {force.magnitude:0.0}).");
        OnRelease();
        rb.AddForce(force, ForceMode.Impulse);
    }

    // Knocked by another thrown object. Ignore while held; otherwise it's already a
    // free dynamic body, so just shove it.
    public void Knock(Vector3 force)
    {
        if (IsHeld) return;
        rb.useGravity = true;
        rb.AddForce(force, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (holdAnchor == null) return;
        // Velocity toward the anchor — keeps the body solid against colliders.
        rb.linearVelocity = (holdAnchor.position - rb.position) * followSpeed;
    }
}
