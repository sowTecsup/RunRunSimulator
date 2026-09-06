using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class ArenaCameraControl : MonoBehaviour
{
    [Required, SerializeField] private CinemachineOrbitalFollow orbital;
    [SerializeField] private ArenaCameraDirector director;
    [SerializeField, Min(0.01f)] private float zoomStep = 0.08f;
    [SerializeField, Min(0.01f)] private float orbitDegreesPerPixel = 0.25f;
    [SerializeField, Min(0.01f)] private float pitchDegreesPerPixel = 0.15f;
    [SerializeField, Min(0f)] private float suspendSeconds = 3f;

    private float homeHorizontal;
    private float homeVertical;
    private float homeRadial;

    private void Awake()
    {
        if (orbital == null) return;
        homeHorizontal = orbital.HorizontalAxis.Value;
        homeVertical   = orbital.VerticalAxis.Value;
        homeRadial     = orbital.RadialAxis.Value;
    }

    private void Update()
    {
        if (orbital == null) return;

        var mouse    = Mouse.current;
        var keyboard = Keyboard.current;
        bool touched = false;

        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                orbital.RadialAxis.Value = orbital.RadialAxis.ClampValue(orbital.RadialAxis.Value - Mathf.Sign(scroll) * zoomStep);
                touched = true;
            }

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0.01f)
                {
                    orbital.HorizontalAxis.Value = orbital.HorizontalAxis.ClampValue(orbital.HorizontalAxis.Value + delta.x * orbitDegreesPerPixel);
                    orbital.VerticalAxis.Value   = orbital.VerticalAxis.ClampValue(orbital.VerticalAxis.Value - delta.y * pitchDegreesPerPixel);
                    touched = true;
                }
            }
        }

        if (keyboard != null)
        {
            if (keyboard.fKey.wasPressedThisFrame && director != null) director.enabled = !director.enabled;
            if (keyboard.rKey.wasPressedThisFrame)
            {
                orbital.HorizontalAxis.Value = homeHorizontal;
                orbital.VerticalAxis.Value   = homeVertical;
                orbital.RadialAxis.Value     = homeRadial;
            }
        }

        if (touched && director != null) director.Suspend(suspendSeconds);
    }
}
}
