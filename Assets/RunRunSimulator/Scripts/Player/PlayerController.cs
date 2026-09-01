using Unity.Cinemachine;
using UnityEngine;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The first-person CinemachineCamera. We read its transform (forward = look dir) and its Input Axis Controller (disabled to freeze the camera in menus).")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [Tooltip("Where a grabbed object floats. Make it a child of the camera so it tracks the look.")]
    [SerializeField] private Transform holdAnchor;

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

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;
    private IThrowable held;
    private Transform  heldTransform;
    private MoriMochiAgent petTarget;

    private bool  grabbing;
    private float grabTimer;

    private PlayerStateType state = PlayerStateType.Exploring;

    private Transform cameraTransform;
    private CinemachineInputAxisController lookAxis;

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

    private void OnUIFocusChanged(bool uiFocused) =>
        SetState(uiFocused ? PlayerStateType.Menu : PlayerStateType.Exploring);

    private void OnBuildModeChanged(bool building) =>
        SetState(building ? PlayerStateType.Building : PlayerStateType.Exploring);

    private void SetState(PlayerStateType next)
    {
        state = next;
        bool firstPerson = state == PlayerStateType.Exploring || state == PlayerStateType.Building;

        Cursor.lockState = firstPerson ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !firstPerson;

        if (lookAxis != null) lookAxis.enabled = firstPerson;

        if (next != PlayerStateType.Exploring && petTarget != null)
        {
            petTarget.EndPetting();
            petTarget = null;
        }
    }

    private void Update()
    {
        Move();
        UpdateGrabHold();
    }

    private void Move()
    {
        if (state != PlayerStateType.Exploring && state != PlayerStateType.Building) return;
        if (cameraTransform == null) return;

        Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight   = cameraTransform.right;   camRight.y   = 0f; camRight.Normalize();

        if (camForward.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(camForward);

        Vector3 dir = (camForward * moveInput.y + camRight * moveInput.x) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        dir.y = verticalVelocity;

        controller.Move(dir * Time.deltaTime);
    }

    private void OnJump()
    {
        if (state != PlayerStateType.Exploring) return;
        if (controller.isGrounded) verticalVelocity = jumpForce;
    }

    private void OnInteractPressed()
    {
        if (state != PlayerStateType.Exploring) return;

        if (held != null)
        {
            Debug.Log("[PlayerController] Carrying + E pressed → dropping in place.");
            Drop();
            return;
        }

        if (TryBeginPetting(out var pettable))
        {
            petTarget = pettable;
            return;
        }

        grabbing  = true;
        grabTimer = 0f;
    }

    private void OnInteractReleased()
    {
        if (petTarget != null)
        {
            petTarget.EndPetting();
            petTarget = null;
            grabbing  = false;
            return;
        }

        if (state != PlayerStateType.Exploring) { grabbing = false; return; }
        if (!grabbing) return;
        grabbing = false;

        Debug.Log("[PlayerController] E tapped → trying to interact.");
        if (TryFindInView<IInteractable>(out var interactable) && interactable is not MoriMochiAgent)
            interactable.Interact();
        else
            Debug.Log("[PlayerController] Nothing interactable in front of the camera.");
    }

    private void UpdateGrabHold()
    {
        if (state != PlayerStateType.Exploring) return;
        if (!grabbing || held != null) return;

        grabTimer += Time.deltaTime;
        if (grabTimer < grabHoldDuration) return;

        grabbing = false;

        if (TryFindInView<MoriMochiAgent>(out var agent))
        {
            Debug.Log("[PlayerController] Hold complete + MoriMonchi found → grabbing.");
            agent.OnGrab(holdAnchor);
            held          = agent;
            heldTransform = agent.transform;
            return;
        }

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
    }

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

    private bool TryBeginPetting(out MoriMochiAgent target)
    {
        target = null;
        var cols = Physics.OverlapSphere(transform.position, grabRange, creatureLayer);
        foreach (var col in cols)
        {
            var a = col.GetComponent<MoriMochiAgent>();
            if (a == null && col.attachedRigidbody != null)
                a = col.attachedRigidbody.GetComponent<MoriMochiAgent>();
            if (a == null || !a.BeginPetting()) continue;
            target = a;
            return true;
        }
        return false;
    }

    private bool TryFindInView<T>(out T component) where T : class
    {
        component = null;

        if (cameraTransform == null)
        {
            Debug.LogWarning("[PlayerController] cameraTransform is NOT assigned — cannot raycast.");
            return false;
        }

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
            component = hit.collider.GetComponentInParent<T>();
            if (component != null) return true;

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
}
