using TMPro;
using UnityEngine;

// A floating 3D label above a MoriMochi: its name plus a status line (busy/dead).
// Billboards toward the camera and only shows when the player is near, so the
// world isn't a wall of text. Pure view — it reads a CreatureDNA reference each
// frame (name is stable, but BusyState/IsDead can change live).
//
// Setup: child of the creature prefab, with two TextMeshPro (3D) components
// assigned below. Uses TMP_Text so either 3D or UGUI variants work.
public class NameTag : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Visibility")]
    [Tooltip("Show the tag only when the camera is within this distance.")]
    [SerializeField] private float showDistance = 8f;
    [Tooltip("Keep the text upright (ignore camera pitch) instead of fully facing it.")]
    [SerializeField] private bool uprightOnly = true;

    private CreatureDNA dna;
    private Transform   cam;

    // Called by the spawner/agent right after instantiation.
    public void Bind(CreatureDNA creature)
    {
        dna = creature;
        if (nameLabel != null) nameLabel.text = creature?.CustomName ?? "";
        Refresh();
    }

    private void Awake()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        // Distance-gated visibility.
        float distSqr = (cam.position - transform.position).sqrMagnitude;
        bool  visible = distSqr <= showDistance * showDistance;
        if (nameLabel != null && nameLabel.enabled != visible)   nameLabel.enabled = visible;
        if (statusLabel != null && statusLabel.enabled != visible) statusLabel.enabled = visible;
        if (!visible) return;

        Refresh();

        // Billboard toward the camera.
        Vector3 toCam = transform.position - cam.position;
        if (uprightOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam);
    }

    private void Refresh()
    {
        if (statusLabel == null || dna == null) return;
        statusLabel.text = StatusText(dna);
    }

    private static string StatusText(CreatureDNA dna)
    {
        if (dna.IsDead) return "<color=#ff6464>Muerto</color>";
        return dna.BusyState switch
        {
            BusyReason.QueuedForCombat => "<color=#ffb464>En cola</color>",
            BusyReason.Breeding        => "<color=#ff9bd2>Incubando</color>",
            _                          => "",
        };
    }
}
