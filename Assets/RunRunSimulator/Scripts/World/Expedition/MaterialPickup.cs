using UnityEngine;
using UnityEngine.Events;

namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Perceivable))]
public class MaterialPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;
    [SerializeField, Min(0f)] private float disableDelay = 0f;
    [SerializeField, Min(0f)] private float standoffRadius = 0f;
    [SerializeField] private UnityEvent onTaken;

    private float cachedRadius = -1f;

    public int Value => value;
    public bool Taken { get; private set; }

    public float Radius
    {
        get
        {
            if (cachedRadius < 0f)
                cachedRadius = standoffRadius > 0f ? standoffRadius : ComputeRadius();
            return cachedRadius;
        }
    }

    internal void SetValue(int newValue) => value = Mathf.Max(1, newValue);

    private float ComputeRadius()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return 0.5f;
        var e = renderer.bounds.extents;
        return Mathf.Max(e.x, e.z);
    }

    public Vector3 ApproachPoint(Vector3 from, float margin)
    {
        Vector3 center = transform.position;
        Vector3 dir = from - center;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        }
        dir.Normalize();
        return center + dir * (Radius + margin);
    }

    public bool TryTake(out int taken)
    {
        if (Taken) { taken = 0; return false; }
        Taken = true;
        taken = value;
        onTaken?.Invoke();
        if (disableDelay <= 0f) gameObject.SetActive(false);
        else StartCoroutine(DisableAfter(disableDelay));
        return true;
    }

    private System.Collections.IEnumerator DisableAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
}
