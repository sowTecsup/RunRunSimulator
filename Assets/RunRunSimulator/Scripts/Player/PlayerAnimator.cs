using UnityEngine;
namespace MoriMonchiSimulator
{

public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Assign when a rig + clips exist. Until then this layer does nothing.")]
    [SerializeField] private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash  = Animator.StringToHash("Jump");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    private Vector2 moveInput;

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
