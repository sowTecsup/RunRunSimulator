using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class ArenaClockControl : MonoBehaviour
{
    [SerializeField] private MMTimeManager timeManager;
    [SerializeField] private float[] speeds = { 1f, 2f, 5f, 10f };

    public static float Speed { get; private set; } = 1f;
    public static bool Paused { get; private set; }

    private int speedIndex;

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) Apply(0);
        if (keyboard.digit2Key.wasPressedThisFrame) Apply(1);
        if (keyboard.digit3Key.wasPressedThisFrame) Apply(2);
        if (keyboard.digit4Key.wasPressedThisFrame) Apply(3);
        if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
    }

    private void OnDisable()
    {
        Paused = false;
        if (Speed != 1f) Set(1f);
    }

    private void Apply(int index)
    {
        if (index < 0 || index >= speeds.Length) return;
        speedIndex = index;
        Set(speeds[index]);
    }

    public void CycleSpeed()
    {
        Apply((speedIndex + 1) % speeds.Length);
    }

    public void TogglePause()
    {
        Paused = !Paused;
        ApplyScale(Paused ? 0f : Speed);
    }

    public void Set(float speed)
    {
        Speed = Mathf.Max(0.1f, speed);
        Paused = false;
        ApplyScale(Speed);
    }

    private void ApplyScale(float scale)
    {
        if (timeManager != null)
        {
            timeManager.NormalTimeScale  = scale;
            timeManager.CurrentTimeScale = scale;
            timeManager.TargetTimeScale  = scale;
        }
        Time.timeScale = scale;
    }
}
}
