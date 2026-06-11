using Unity.Cinemachine;
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
    [Tooltip("The first-person CinemachineCamera. We read its transform (forward = look dir) and its Input Axis Controller (disabled to freeze the camera in menus).")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
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

    [Header("Pet")]
    [Tooltip("Layer(s) that MoriMochi creatures are on. The pet check uses OverlapSphere (no raycast) so NameTag world-space panels cannot block it. Assign a dedicated creature layer here and set the prefab to that layer.")]
    [SerializeField] private LayerMask creatureLayer = ~0;

    [Header("Throw")]
    [SerializeField] private float throwForce = 10f;
    [Tooltip("Extra upward lift blended into the aim direction so a level throw still arcs a little. 0 = throw exactly where you aim.")]
    [Range(0f, 1f)]
    [SerializeField] private float throwUpwardBias = 0.15f;
    [Tooltip("How far down the camera's look ray we search for the aim point the throw converges on. Hitting geometry closer than this aims there instead.")]
    [SerializeField] private float throwAimDistance = 30f;

    // ── State ─────────────────────────────────────────────────────

    private CharacterController controller;
    private Vector2 moveInput;        // cached from MoveChanged — event carries the data
    private float verticalVelocity;
    private IThrowable held;          // currently grabbed object, or null
    private Transform  heldTransform; // its transform, so the throw-aim ray can ignore it

    // Hold-to-grab tracking (only while free and E is held down).
    private bool  grabbing;
    private float grabTimer;

    // What the player is doing. Menu suspends movement/camera so the player can
    // operate an open UI panel with a free cursor.
    private PlayerStateType state = PlayerStateType.Exploring;

    // Derived from cinemachineCamera in Awake: the transform we read for look
    // direction, and the look input we disable to freeze the camera in menus.
    private Transform cameraTransform;
    private CinemachineInputAxisController lookAxis;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cinemachineCamera != null)
        {
            cameraTransform = cinemachineCamera.transform;
            cinemachineCamera.TryGetComponent(out lookAxis);
        }
    }

    private void Start() => SetState(PlayerStateType.Exploring);

    private void OnEnable()
    {
        PlayerInputs.MoveChanged       += OnMoveChanged;
        PlayerInputs.Jumped            += OnJump;
        PlayerInputs.InteractPressed   += OnInteractPressed;
        PlayerInputs.InteractReleased  += OnInteractReleased;
        PlayerInputs.ThrowPressed      += OnThrow;
        UIManager.OnUIFocusChanged     += OnUIFocusChanged;
        BuildModeController.OnBuildModeChanged += OnBuildModeChanged;
    }

    private void OnDisable()
    {
        PlayerInputs.MoveChanged       -= OnMoveChanged;
        PlayerInputs.Jumped            -= OnJump;
        PlayerInputs.InteractPressed   -= OnInteractPressed;
        PlayerInputs.InteractReleased  -= OnInteractReleased;
        PlayerInputs.ThrowPressed      -= OnThrow;
        UIManager.OnUIFocusChanged     -= OnUIFocusChanged;
        BuildModeController.OnBuildModeChanged -= OnBuildModeChanged;
    }

    private void OnMoveChanged(Vector2 move) => moveInput = move;

    // ── State ─────────────────────────────────────────────────────
    // A panel opening puts us in Menu; closing the last one returns to Exploring.
    private void OnUIFocusChanged(bool uiFocused) =>
        SetState(uiFocused ? PlayerStateType.Menu : PlayerStateType.Exploring);

    // Build mode is the player's third state: movement and the camera stay live (you
    // aim the placement ghost by looking), but grab/throw/jump are suspended below.
    // BuildModeController owns the ghost and placement; we only flip our own state.
    private void OnBuildModeChanged(bool building) =>
        SetState(building ? PlayerStateType.Building : PlayerStateType.Exploring);

    private void SetState(PlayerStateType next)
    {
        state = next;
        // Exploring and Building both play first-person (cursor locked, camera live);
        // only Menu frees the cursor and freezes the camera for UI.
        bool firstPerson = state == PlayerStateType.Exploring || state == PlayerStateType.Building;

        Cursor.lockState = firstPerson ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !firstPerson;

        if (lookAxis != null) lookAxis.enabled = firstPerson;
    }

    private void Update()
    {
        Move();
        UpdateGrabHold();
    }

    // ── Move ──────────────────────────────────────────────────────
    // Camera-relative movement; the body yaw follows the camera (first-person).
    private void Move()
    {
        // Walk in Exploring and Building (reposition while placing); suspended in Menu.
        if (state != PlayerStateType.Exploring && state != PlayerStateType.Building) return;
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
        if (state != PlayerStateType.Exploring) return;
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
        if (state != PlayerStateType.Exploring) return;   // no grab/interact while building or in a menu

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
        if (state != PlayerStateType.Exploring) { grabbing = false; return; }
        if (!grabbing) return;   // the hold already resolved into a grab — ignore this release
        grabbing = false;

        // Released before the threshold → it was a TAP.
        // Pet check first (OverlapSphere + dot, no raycast → NameTag panels can't block it).
        // Falls through to the general IInteractable raycast if no creature is pettable.
        if (TryPetNearbyCreature()) return;

        Debug.Log("[PlayerController] E tapped → trying to interact.");
        if (TryFindInView<IInteractable>(out var interactable))
            interactable.Interact();
        else
            Debug.Log("[PlayerController] Nothing interactable in front of the camera.");
    }

    // Counts how long E has been held while free; resolves once it passes the threshold.
    // MoriMonchi agents keep the physical grab; for everything else hold E THROWS the
    // active hotbar item (props live in the hotbar now, not in a physical grab).
    private void UpdateGrabHold()
    {
        // Safety net: in a menu the Player input map is disabled (PlayerInputs gates
        // it on UI focus), so no grab/interact reaches us anyway. Panels are closed
        // with ESC now (UIInputs → UIManager pops the stack), not with E.
        if (state != PlayerStateType.Exploring) return;
        if (!grabbing || held != null) return;

        grabTimer += Time.deltaTime;
        if (grabTimer < grabHoldDuration) return;

        grabbing = false;   // consume the hold (so the release doesn't also interact)

        // A MoriMonchi under the crosshair → grab it physically (its existing flow).
        if (TryFindInView<MoriMochiAgent>(out var agent))
        {
            Debug.Log("[PlayerController] Hold complete + MoriMonchi found → grabbing.");
            agent.OnGrab(holdAnchor);
            held          = agent;
            heldTransform = agent.transform;
            return;
        }

        // Otherwise → throw the active hotbar item, if any.
        if (cameraTransform != null && HotbarController.Instance != null && HotbarController.Instance.HasActiveItem)
        {
            Debug.Log("[PlayerController] Hold complete → throwing active hotbar item.");
            HotbarController.Instance.ThrowActive(ComputeThrowImpulse(null));
        }
    }

    private void Drop()
    {
        if (held == null) return;
        held.OnRelease();
        held          = null;
        heldTransform = null;
    }

    // Click (Attack): throw a carried MoriMonchi (its existing flow) OR, when not
    // carrying one, USE the active hotbar item.
    private void OnThrow()
    {
        if (state != PlayerStateType.Exploring) return;

        if (held != null)
        {
            if (cameraTransform == null) { Debug.LogWarning("[PlayerController] camera not assigned — cannot throw."); return; }
            held.OnThrow(ComputeThrowImpulse(heldTransform));
            held          = null;
            heldTransform = null;
            return;
        }

        if (HotbarController.Instance != null && HotbarController.Instance.HasActiveItem)
            HotbarController.Instance.UseActive();
        else
            Debug.Log("[PlayerController] Nothing held and no active hotbar item — click does nothing.");
    }

    // Impulse for a throw aimed at the crosshair. The object floats at the hold anchor,
    // which sits a bit off-center; throwing along camera.forward from there flies
    // parallel and never reaches the aim. Instead aim from the anchor TOWARD where the
    // camera looks, so it converges on the screen center. 'ignore' = a transform whose
    // colliders the aim ray skips (the held object), or null.
    private Vector3 ComputeThrowImpulse(Transform ignore)
    {
        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * throwAimDistance;
        var hits = Physics.RaycastAll(cameraTransform.position, cameraTransform.forward,
                                      throwAimDistance, grabMask, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        foreach (var h in hits)
        {
            if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
            if (h.distance < nearest) { nearest = h.distance; aimPoint = h.point; }
        }

        Vector3 origin = holdAnchor != null ? holdAnchor.position : cameraTransform.position;
        Vector3 dir    = ((aimPoint - origin).normalized + Vector3.up * throwUpwardBias).normalized;
        return dir * throwForce;
    }

    // Pet check: OverlapSphere (no raycast → NameTag panels can't block it).
    // CanBePetted already includes the IsPlayerFacingMe() dot-product check.
    private bool TryPetNearbyCreature()
    {
        var cols = Physics.OverlapSphere(transform.position, grabRange, creatureLayer);
        foreach (var col in cols)
        {
            var a = col.GetComponent<MoriMochiAgent>();
            if (a == null && col.attachedRigidbody != null)
                a = col.attachedRigidbody.GetComponent<MoriMochiAgent>();
            if (a == null || !a.CanBePetted) continue;
            a.Interact();
            return true;
        }
        return false;
    }

    // Raycasts from the camera for a component of type T (interface or class).
    // Full forward, so you can reach things above/below eye level too. T may sit
    // on the hit collider or on its Rigidbody root.
    //
    // Hits triggers too (QueryTriggerInteraction.Collide): MoriMonchis carry a TRIGGER
    // collider while NavMesh-driven (the throwable handoff makes it solid only in flight),
    // so an Ignore raycast would never find a roaming one to grab. We walk the hits nearest
    // -first: the first one that resolves to T wins; a SOLID collider that isn't T blocks the
    // ray (no grabbing through walls); a non-T trigger is seen through (panels, station zones).
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

        var hits = Physics.RaycastAll(cameraTransform.position, cameraTransform.forward, grabRange, grabMask, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            Debug.Log($"[PlayerController] Raycast hit NOTHING (range={grabRange}, mask={grabMask.value}).");
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent(out component)) return true;

            var rb = hit.collider.attachedRigidbody;
            if (rb != null && rb.TryGetComponent(out component)) return true;

            // A solid collider that isn't the target blocks the reach; triggers are transparent.
            if (!hit.collider.isTrigger)
            {
                Debug.Log($"[PlayerController] Reach blocked by solid '{hit.collider.name}' — no {typeof(T).Name}.");
                return false;
            }
        }

        component = null;
        return false;
    }
}
