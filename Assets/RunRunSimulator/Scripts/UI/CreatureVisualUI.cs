using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Visual card for ONE MoriMochi inside the in-game grid (Canvas).
// Pure view (Etapa 1.2 visual): receives a CreatureDNA and paints itself.
// Never mutates data, never reaches into the registry or GameManager — the
// grid that spawns it hands over everything it needs through Bind().
//
// Initial layout (this stage): name on top, a sprite slot in the middle,
// and the current state at the bottom. Stats/parents come later.
public class CreatureVisualUI : MonoBehaviour
{
    [Header("Display references")]

    // The MoriMochi's name (CustomName). Sits at the top of the card.
    [SerializeField] private TextMeshProUGUI nameLabel;

    // Slot for the creature's icon/sprite. We don't render real icons yet,
    // so for now we just tint this Image with the creature's PrimaryColor —
    // gives each card a distinct look and proves the sprite slot is wired.
    // When per-creature sprites exist, set iconImage.sprite here instead.
    [SerializeField] private Image iconImage;

    // Current state line at the bottom: Free / Breeding / In Queue / DEAD.
    [SerializeField] private TextMeshProUGUI stateLabel;

    // ── Public Methods ────────────────────────────────────────────

    // Fills every field from one creature. Called by the grid right after
    // instantiating the prefab. Idempotent — safe to call again to repaint.
    public void Bind(CreatureDNA dna)
    {
        nameLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        if (iconImage != null)
            iconImage.color = dna.PrimaryColor;

        stateLabel.text = StateOf(dna);
    }

    // ── Private Methods ───────────────────────────────────────────

    private static string StateOf(CreatureDNA d) =>
        d.IsDead                                  ? "DEAD"     :
        d.BusyState == BusyReason.Breeding        ? "Breeding" :
        d.BusyState == BusyReason.QueuedForCombat ? "In Queue" :
        "Free";
}
