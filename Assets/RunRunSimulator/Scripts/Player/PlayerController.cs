using UnityEngine;

// First-person player LOGIC: movement, jump, and grab/throw orchestration.
// It NEVER references PlayerInputs — it only subscribes to PlayerInputs' static
// events (same pattern as GameEvents) and caches what it needs from the payload.
// Look/aim is owned by Cinemachine; this script just reads the camera's forward
// to move and to aim grabs/throws. Grab/throw goes through the IThrowable
// contract, so the controller never knows the concrete object it holds.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The Cinemachine first-person camera's transform — its forward is the look direction.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Where a grabbed object floats. Make it a child of the camera so it tracks the look.")]
    [SerializeField] private Transform holdAnchor;

    // ── Tuning ────────────────────────────────────────────────────

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity   = -20f;

    [Header("Reach (grab / interact)")]
    [Tooltip("How long E must be held (while free) to pick an object up. A shorter tap interacts instead.")]
    [SerializeField] private float grabHoldDuration = 0.3f;
    [Tooltip("How far ahead we look for a grabbable / interactable.")]
    [SerializeField] private float grabRange  = 3f;
    [SerializeField] private LayerMask grabMask = ~0;

    [Header("Throw")]
    [SerializeField] private float throwForce = 10f;
    [Tooltip("Distance of the aim point along the camera. The throw converges from the hand toward it, so it flies to where you look.")]
    [SerializeField] private float throwAimDistance = 30f;

    // ── State ─────────────────────────────────────────────────────

    private CharacterController controller;
    private Vector2 moveInput;        // cached from MoveChanged — event carries the data
    private float verticalVelocity;
    private IThrowable held;          // currently grabbed object, or null

    // Hold-to-grab tracking (only while free and E is held down).
    private bool  grabbing;
    private float grabTimer;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()
    {
        PlayerInputs.MoveChanged       += OnMoveChanged;
        PlayerInputs.Jumped            += OnJump;
        PlayerInputs.InteractPressed   += OnInteractPressed;
        PlayerInputs.InteractReleased  += OnInteractReleased;
        PlayerInputs.ThrowPressed      += OnThrow;
    }

    private void OnDisable()
    {
        PlayerInputs.MoveChanged       -= OnMoveChanged;
        PlayerInputs.Jumped            -= OnJump;
        PlayerInputs.InteractPressed   -= OnInteractPressed;
        PlayerInputs.InteractReleased  -= OnInteractReleased;
        PlayerInputs.ThrowPressed      -= OnThrow;
    }

    private void OnMoveChanged(Vector2 move) => moveInput = move;

    private void Update()
    {
        Move();
        UpdateGrabHold();
    }

    // ── Move ──────────────────────────────────────────────────────
    // Camera-relative movement; the body yaw follows the camera (first-person).
    private void Move()
    {
        if (cameraTransform == null) return;

        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight   = cameraTransform.right;   camRight.y   = 0f; camRight.Normalize();

        if (camForward.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(camForward);

        Vector3 dir = (camForward * moveInput.y + camRight * moveInput.x) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;                       // stay grounded, don't accumulate
        verticalVelocity += gravity * Time.deltaTime;
        dir.y = verticalVelocity;

        controller.Move(dir * Time.deltaTime);
    }

    private void OnJump()
    {
        if (controller.isGrounded) verticalVelocity = jumpForce;
    }

    // ── Grab / Interact / Throw ───────────────────────────────────
    // E means different things by context:
    //   • free + TAP E                       → interact with an IInteractable
    //   • free + HOLD E (≥ grabHoldDuration) → grab an IThrowable
    //   • carrying + PRESS E                 → drop it in place
    //   • carrying + Click (Attack)          → throw it
    private void OnInteractPressed()
    {
        if (held != null)
        {
            Debug.Log("[PlayerController] Carrying + E pressed → dropping in place.");
            Drop();
            return;
        }

        // Free: start timing. Release decides tap(interact); the timer decides hold(grab).
        grabbing  = true;
        grabTimer = 0f;
    }

    private void OnInteractReleased()
    {
        if (!grabbing) return;   // the hold already resolved into a grab — ignore this release
        grabbing = false;

        // Released before the threshold → it was a TAP → interact.
        Debug.Log("[PlayerController] E tapped → trying to interact.");
        if (TryFindInView<IInteractable>(out var interactable))
            interactable.Interact();
        else
            Debug.Log("[PlayerController] Nothing interactable in front of the camera.");
    }

    // Counts how long E has been held while free; grabs once it passes the threshold.
    private void UpdateGrabHold()
    {
        if (!grabbing || held != null) return;

        grabTimer += Time.deltaTime;
        if (grabTimer < grabHoldDuration) return;

        grabbing = false;   // consume the hold (so the release doesn't also interact)
        if (TryFindInView<IThrowable>(out var throwable))
        {
            Debug.Log("[PlayerController] Hold complete + throwable found → grabbing.");
            throwable.OnGrab(holdAnchor);
            held = throwable;
        }
        else
        {
            Debug.Log("[PlayerController] Hold complete but no throwable in front of the camera.");
        }
    }

    private void Drop()
    {
        if (held == null) return;
        held.OnRelease();
        held = null;
    }

    private void OnThrow()
    {
        Debug.Log($"[PlayerController] ThrowPressed received. Currently holding: {held != null}");

        if (held == null) { Debug.Log("[PlayerController] Nothing held — nothing to throw."); return; }
        if (cameraTransform == null || holdAnchor == null) { Debug.LogWarning("[PlayerController] camera/holdAnchor not assigned — cannot throw."); return; }

        // Throw from the object's current spot (the hand/holdAnchor) toward a point on
        // the camera's aim line, so it converges to where we're looking instead of
        // flying parallel from the side.
        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * throwAimDistance;
        Vector3 dir      = (aimPoint - holdAnchor.position).normalized;

        Debug.Log($"[PlayerController] Throwing toward aim point {aimPoint} (dir {dir}).");
        held.OnThrow(dir * throwForce);
        held = null;
    }

    // Raycasts from the camera for a component of type T (interface or class).
    // Full forward, so you can reach things above/below eye level too. T may sit
    // on the hit collider or on its Rigidbody root.
    private bool TryFindInView<T>(out T component) where T : class
    {
        component = null;

        if (cameraTransform == null)
        {
            Debug.LogWarning("[PlayerController] cameraTransform is NOT assigned — cannot raycast.");
            return false;
        }

        // Visualize the reach ray in Scene view for 1s.
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * grabRange, Color.green, 1f);

        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, grabRange, grabMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[PlayerController] Raycast hit NOTHING (range={grabRange}, mask={grabMask.value}).");
            return false;
        }

        Debug.Log($"[PlayerController] Raycast hit '{hit.collider.name}' (layer {hit.collider.gameObject.layer}) at {hit.distance:0.00}m.");

        if (hit.collider.TryGetComponent(out component)) return true;

        var rb = hit.collider.attachedRigidbody;
        if (rb != null && rb.TryGetComponent(out component)) return true;

        Debug.Log($"[PlayerController] '{hit.collider.name}' has no {typeof(T).Name}.");
        return false;
    }
}
