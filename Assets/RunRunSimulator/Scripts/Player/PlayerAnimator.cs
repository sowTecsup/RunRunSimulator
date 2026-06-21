using UnityEngine;
namespace MoriMonchiSimulator
{

// Player ANIMATION layer. Like PlayerController, it only subscribes to
// PlayerInputs' static events — no reference to PlayerInputs, no movement logic.
// When rigs/clips arrive, only THIS script changes.
//
// No Animator is wired yet: every call is guarded, so the component is inert
// until you assign one. The parameter names below are the seam — create matching
// Animator params when the clips exist.
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Assign when a rig + clips exist. Until then this layer does nothing.")]
    [SerializeField] private Animator animator;

    // Parameter hashes — cheaper than string lookups each frame.
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash  = Animator.StringToHash("Jump");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    private Vector2 moveInput;   // cached from MoveChanged — event carries the data

    private void OnEnable()
    {
        PlayerInputs.MoveChanged  += OnMoveChanged;
        PlayerInputs.Jumped       += OnJump;
        PlayerInputs.ThrowPressed += OnThrow;
    }

    private void OnDisable()
    {
        PlayerInputs.MoveChanged  -= OnMoveChanged;
        PlayerInputs.Jumped       -= OnJump;
        PlayerInputs.ThrowPressed -= OnThrow;
    }

    private void OnMoveChanged(Vector2 move) => moveInput = move;

    private void Update()
    {
        if (animator == null) return;
        animator.SetFloat(SpeedHash, moveInput.magnitude);
    }

    private void OnJump()  { if (animator != null) animator.SetTrigger(JumpHash); }
    private void OnThrow() { if (animator != null) animator.SetTrigger(ThrowHash); }
}
}
