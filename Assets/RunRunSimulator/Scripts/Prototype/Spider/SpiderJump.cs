using UnityEngine;
using UnityEngine.InputSystem;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderJump : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private SpiderRagdollMode ragdollMode;
        [SerializeField] private float gravityScale = 2.5f;

        private float heightOffset;
        private float verticalVelocity;

        public float HeightOffset { get { return heightOffset; } }
        public bool IsAirborne { get { return heightOffset > 0f; } }

        public void Jump()
        {
            if (IsAirborne) return;
            if (ragdollMode != null && ragdollMode.IsRagdoll) return;
            if (tuning == null) return;
            verticalVelocity = tuning.jumpImpulse;
        }

        private void Update()
        {
            if (ragdollMode != null && ragdollMode.IsRagdoll)
            {
                heightOffset = 0f;
                verticalVelocity = 0f;
                return;
            }

            if (!IsAirborne && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Jump();
            }

            if (IsAirborne || verticalVelocity > 0f)
            {
                verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
                heightOffset += verticalVelocity * Time.deltaTime;
                if (heightOffset <= 0f)
                {
                    heightOffset = 0f;
                    verticalVelocity = 0f;
                }
            }
        }
    }
}
