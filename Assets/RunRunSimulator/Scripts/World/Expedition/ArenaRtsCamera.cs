using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
namespace MoriMonchiSimulator
{

public class ArenaRtsCamera : MonoBehaviour
{
    [Title("Referencias")]
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ArenaLayoutBuilder layout;
    [SerializeField] private ArenaCameraDirector director;

    [Title("Vista")]
    [SerializeField] private float pitch = 56f;
    [SerializeField, Min(1f)] private float zoomMin = 14f;
    [SerializeField, Min(1f)] private float zoomMax = 46f;
    [SerializeField, Min(1f)] private float zoomStart = 30f;
    [SerializeField, Min(0.1f)] private float zoomStep = 3f;
    [SerializeField, Min(0.01f)] private float zoomSmoothing = 8f;

    [Title("Paneo")]
    [SerializeField, Min(0f)] private float panSpeed = 18f;
    [SerializeField, Min(0f)] private float edgePanPixels = 12f;
    [SerializeField] private bool edgePanEnabled = true;
    [SerializeField, Min(0.01f)] private float panSmoothing = 10f;

    [Title("Límite")]
    [SerializeField, Min(0f)] private float boundsInset = 2f;
    [SerializeField, Min(0f)] private float boundsOvershoot = 6f;
    [SerializeField, Min(0f)] private float boundsSpring = 6f;
    [SerializeField, Range(0f, 1f)] private float boundsResistance = 0.35f;

    [Title("Bloqueo")]
    [SerializeField, Min(0.01f)] private float followSmoothing = 12f;

    private Vector3 pivot;
    private Vector3 desiredPivot;
    private float yaw;
    private float zoom;
    private float zoomTarget;
    private int lastSeenSeed;
    private bool initialized;

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        bool layoutReady = layout != null && layout.IsBuilt;
        int currentSeed = sandbox != null ? sandbox.ActiveSeed : 0;

        if (layoutReady && (!initialized || currentSeed != lastSeenSeed))
        {
            ResetView();
            lastSeenSeed = currentSeed;
            initialized = true;
        }
        else if (!initialized)
        {
            pivot = Vector3.zero;
            desiredPivot = Vector3.zero;
            yaw = 0f;
            zoom = zoomStart;
            zoomTarget = zoomStart;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) ResetView();

        var rotation = Quaternion.Euler(pitch, yaw, 0f);

        bool locked = (director != null && director.Pinned != null) || (keyboard != null && keyboard.spaceKey.isPressed);
        var followTarget = locked ? ResolveFollowTarget() : null;

        if (followTarget != null)
        {
            float followLerp = 1f - Mathf.Exp(-followSmoothing * dt);
            Vector3 targetPivot = followTarget.transform.position;
            targetPivot.y = pivot.y;
            desiredPivot = Vector3.Lerp(desiredPivot, targetPivot, followLerp);
        }
        else
        {
            Vector3 rawDelta = ReadKeyboardPan(rotation, dt);
            rawDelta += ReadEdgePan(rotation, dt, mouse);
            rawDelta += ReadMiddleDrag(rotation, mouse);
            rawDelta = ApplyResistance(rawDelta);
            desiredPivot += rawDelta;
        }

        UpdateZoom(dt, mouse);
        ApplyBounds(dt);

        float panLerp = 1f - Mathf.Exp(-panSmoothing * dt);
        pivot = Vector3.Lerp(pivot, desiredPivot, panLerp);

        Vector3 position = pivot - rotation * Vector3.forward * zoom;
        transform.SetPositionAndRotation(position, rotation);
    }

    private void ResetView()
    {
        ComputeInitialView(out yaw, out var initialPivot);
        pivot = initialPivot;
        desiredPivot = initialPivot;
        zoom = zoomStart;
        zoomTarget = zoomStart;
    }

    private void ComputeInitialView(out float newYaw, out Vector3 newPivot)
    {
        if (layout == null || !layout.IsBuilt)
        {
            newYaw = 0f;
            newPivot = Vector3.zero;
            return;
        }

        Vector3 spawn = layout.SpawnPoint(ExpeditionTeam.Player);
        Vector3 center = layout.Center;
        Vector3 direction = center - spawn;
        direction.y = 0f;

        newYaw = direction.sqrMagnitude > 0.0001f ? Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg : 0f;

        Vector3 pivotPoint = spawn + direction * 0.3f;
        pivotPoint.y = center.y;
        newPivot = pivotPoint;
    }

    private MoriMochiAgent ResolveFollowTarget()
    {
        if (director != null && director.Pinned != null) return director.Pinned;
        if (sandbox == null) return null;

        foreach (var controller in sandbox.Spawned)
        {
            if (controller == null || controller.Agent == null) continue;
            if (controller.Agent.Team == ExpeditionTeam.Player) return controller.Agent;
        }

        return null;
    }

    private Vector3 ReadKeyboardPan(Quaternion rotation, float dt)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector3.zero;

        float v = 0f, h = 0f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;

        if (v == 0f && h == 0f) return Vector3.zero;

        Vector3 direction = PlanarForward(rotation) * v + PlanarRight(rotation) * h;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        float speed = panSpeed * (zoom / zoomStart);
        return direction * speed * dt;
    }

    private Vector3 ReadEdgePan(Quaternion rotation, float dt, Mouse mouse)
    {
        if (!edgePanEnabled || mouse == null || !Application.isFocused) return Vector3.zero;

        Vector2 position = mouse.position.ReadValue();
        if (position.x < 0f || position.x > Screen.width || position.y < 0f || position.y > Screen.height)
            return Vector3.zero;

        float h = 0f, v = 0f;
        if (position.x <= edgePanPixels) h -= 1f;
        else if (position.x >= Screen.width - edgePanPixels) h += 1f;
        if (position.y <= edgePanPixels) v -= 1f;
        else if (position.y >= Screen.height - edgePanPixels) v += 1f;

        if (h == 0f && v == 0f) return Vector3.zero;

        Vector3 direction = PlanarForward(rotation) * v + PlanarRight(rotation) * h;
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        float speed = panSpeed * (zoom / zoomStart);
        return direction * speed * dt;
    }

    private Vector3 ReadMiddleDrag(Quaternion rotation, Mouse mouse)
    {
        if (mouse == null || !mouse.middleButton.isPressed) return Vector3.zero;

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude < 0.0001f) return Vector3.zero;

        float scale = 0.02f * (zoom / zoomStart);
        Vector3 right = PlanarRight(rotation);
        Vector3 forward = PlanarForward(rotation);
        return -(right * delta.x + forward * delta.y) * scale;
    }

    private void UpdateZoom(float dt, Mouse mouse)
    {
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                zoomTarget = Mathf.Clamp(zoomTarget - Mathf.Sign(scroll) * zoomStep, zoomMin, zoomMax);
        }

        zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-zoomSmoothing * dt));
    }

    private Vector3 ApplyResistance(Vector3 rawDelta)
    {
        var shape = sandbox != null ? sandbox.ActiveShape : null;
        if (shape == null) return rawDelta;

        var polygon = shape.OutlinePolygon;
        if (polygon == null || polygon.Count < 3) return rawDelta;

        Vector2 p = new Vector2(desiredPivot.x, desiredPivot.z);
        float d = SignedDistance(polygon, p);
        if (d >= boundsInset) return rawDelta;

        Vector2 inward = InwardDirection(polygon, p);
        if (inward.sqrMagnitude < 0.0001f) return rawDelta;

        Vector2 outward = -inward;
        Vector2 flat = new Vector2(rawDelta.x, rawDelta.z);
        float outwardComponent = Vector2.Dot(flat, outward);
        if (outwardComponent <= 0f) return rawDelta;

        Vector2 outwardPart = outward * outwardComponent;
        Vector2 tangentPart = flat - outwardPart;
        Vector2 attenuated = tangentPart + outwardPart * boundsResistance;
        return new Vector3(attenuated.x, rawDelta.y, attenuated.y);
    }

    private void ApplyBounds(float dt)
    {
        var shape = sandbox != null ? sandbox.ActiveShape : null;
        if (shape == null) return;

        var polygon = shape.OutlinePolygon;
        if (polygon == null || polygon.Count < 3) return;

        Vector2 p = new Vector2(desiredPivot.x, desiredPivot.z);
        float d = SignedDistance(polygon, p);
        if (d >= boundsInset) return;

        float e = boundsInset - d;
        Vector2 inward = InwardDirection(polygon, p);
        if (inward.sqrMagnitude < 0.0001f) return;

        if (e > boundsOvershoot)
        {
            float correction = e - boundsOvershoot;
            desiredPivot += new Vector3(inward.x, 0f, inward.y) * correction;
            e = boundsOvershoot;
        }

        Vector2 spring = inward * (e * boundsSpring * dt);
        desiredPivot += new Vector3(spring.x, 0f, spring.y);
    }

    private static Vector2 InwardDirection(IReadOnlyList<Vector2> polygon, Vector2 p)
    {
        const float step = 0.5f;
        float dPlusX = SignedDistance(polygon, p + new Vector2(step, 0f));
        float dMinusX = SignedDistance(polygon, p - new Vector2(step, 0f));
        float dPlusZ = SignedDistance(polygon, p + new Vector2(0f, step));
        float dMinusZ = SignedDistance(polygon, p - new Vector2(0f, step));

        Vector2 gradient = new Vector2(dPlusX - dMinusX, dPlusZ - dMinusZ);
        return gradient.sqrMagnitude > 0.0001f ? gradient.normalized : Vector2.zero;
    }

    private static float SignedDistance(IReadOnlyList<Vector2> polygon, Vector2 p)
    {
        bool inside = ArenaShapeMesher.Contains(polygon, p);
        float distance = ArenaShapeMesher.DistanceToEdge(polygon, p);
        return inside ? distance : -distance;
    }

    private static Vector3 PlanarForward(Quaternion rotation)
    {
        Vector3 f = rotation * Vector3.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    private static Vector3 PlanarRight(Quaternion rotation)
    {
        Vector3 r = rotation * Vector3.right;
        r.y = 0f;
        return r.sqrMagnitude > 0.0001f ? r.normalized : Vector3.right;
    }
}
}
