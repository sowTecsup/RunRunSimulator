using UnityEngine;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour, IThrowable
{
    [Tooltip("How snappily the object chases the hold anchor while held.")]
    [SerializeField] private float followSpeed = 15f;

    private Rigidbody rb;
    private Transform holdAnchor;

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
        rb.isKinematic    = false;
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity  = force / Mathf.Max(rb.mass, 0.0001f);
    }

    public void Knock(Vector3 force)
    {
        if (IsHeld) return;
        rb.useGravity = true;
        rb.AddForce(force, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (holdAnchor == null) return;
        rb.linearVelocity = (holdAnchor.position - rb.position) * followSpeed;
    }
}
}
