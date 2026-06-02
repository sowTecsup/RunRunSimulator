using System;
using System.Collections.Generic;
using UnityEngine;

// Owns the BUILD-MODE lifecycle, its sub-state machine, and the placement ghost.
// Entered/left from the Player (B = PlayerInputs.BuildToggled); the rest is driven by the
// Building action map via BuildingInputs. Sub-states:
//
//   Browsing  — nothing selected. 1-4 → start placing a hotbar piece; E (aim a placed
//               piece) → edit it; right-click (aim a placed piece) → target it to delete.
//   Placing   — a NEW piece follows the aim. R rotates, left-click (or F) pins it on a
//               free cell → Editing. Green/red by grid.CanPlace.
//   Editing   — a piece fixed at its cell (new-pinned or an existing piece "lifted" off
//               the grid). R rotates; F saves if green, or reverts the colliding turn if
//               red. Saving commits and returns to Browsing.
//   Deleting  — an existing piece lifted and shown red; F confirms removal, Esc restores.
//
// Esc cancels the current selection (restoring a lifted piece) back to Browsing; in
// Browsing it exits build mode. Selecting/lifting reuses FurnitureService.TryLift /
// TryPlace, so editing is collision-free against itself and everything stays event-driven.
//
// Decoupling: announces mode changes via the static OnBuildModeChanged (mirror of
// UIManager.OnUIFocusChanged). BuildingInputs gates its map on it; PlayerController
// suspends grab/throw on it. This script never references those back.
public class BuildModeController : MonoBehaviour
{
    public static event Action<bool> OnBuildModeChanged;

    private enum BuildState { Browsing, Placing, Editing, Deleting }

    [Header("References")]
    [SerializeField] private FurnitureService service;
    [SerializeField] private PlacementGrid grid;
    [Tooltip("Transform whose forward is the aim ray (the first-person Cinemachine camera).")]
    [SerializeField] private Transform aimTransform;

    [Header("Aim")]
    [Tooltip("Layers the aim ray hits to find the ground cell under the crosshair.")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float aimDistance = 30f;

    [Header("Ghost")]
    [Tooltip("Semi-transparent material applied to every ghost renderer (URP/Lit, Surface = Transparent). Tinted per-frame.")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Color validColor   = new Color(0.3f, 1f, 0.4f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.5f);

    private bool active;
    private bool uiFocused;
    private BuildState state = BuildState.Browsing;

    private FurnitureDefinitionSO heldDef;   // the piece the ghost represents (new or lifted)
    private int rotation;                    // 0/90/180/270 of the ghost
    private int lastValidRotation;           // fallback when an Editing rotation collides

    // For a lifted EXISTING piece (Editing/Deleting): where it came from, to restore on cancel.
    private bool isExistingLift;
    private Vector2Int originalCell;
    private int originalRotation;

    private Vector2Int currentCell;          // ghost cell — follows aim in Placing, frozen once pinned
    private bool aimValid;

    private GameObject ghost;
    private readonly List<Renderer> ghostRenderers = new List<Renderer>();
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        PlayerInputs.BuildToggled     += Toggle;
        BuildingInputs.ConfirmPressed += OnConfirm;
        BuildingInputs.CancelPressed  += OnCancel;
        BuildingInputs.RotatePressed  += OnRotate;
        BuildingInputs.PinPressed     += OnPin;
        BuildingInputs.EditPressed    += OnEdit;
        BuildingInputs.DeletePressed  += OnDelete;
        BuildingInputs.SlotSelected   += OnSlot;
        UIManager.OnUIFocusChanged    += OnUIFocusChanged;
    }

    private void OnDisable()
    {
        PlayerInputs.BuildToggled     -= Toggle;
        BuildingInputs.ConfirmPressed -= OnConfirm;
        BuildingInputs.CancelPressed  -= OnCancel;
        BuildingInputs.RotatePressed  -= OnRotate;
        BuildingInputs.PinPressed     -= OnPin;
        BuildingInputs.EditPressed    -= OnEdit;
        BuildingInputs.DeletePressed  -= OnDelete;
        BuildingInputs.SlotSelected   -= OnSlot;
        UIManager.OnUIFocusChanged    -= OnUIFocusChanged;

        if (active) ExitBuildMode();   // never leave a lifted piece or a ghost behind
    }

    // ── Mode on/off (B) ───────────────────────────────────────────

    private void Toggle()
    {
        if (active) { ExitBuildMode(); return; }
        if (uiFocused) return;              // can't build with a menu open
        EnterBuildMode();
    }

    private void EnterBuildMode()
    {
        if (service == null || grid == null || aimTransform == null)
        {
            Debug.LogError("[BuildModeController] Missing service / grid / aim reference.");
            return;
        }
        active   = true;
        state    = BuildState.Browsing;
        rotation = 0;
        OnBuildModeChanged?.Invoke(true);
        Debug.Log("[BuildModeController] Build mode ON (Browsing).");
    }

    private void ExitBuildMode()
    {
        RestoreLiftedIfAny();               // don't lose/delete a piece by leaving mid-edit
        DestroyGhost();
        heldDef = null; isExistingLift = false;
        state = BuildState.Browsing;
        active = false;
        OnBuildModeChanged?.Invoke(false);
        Debug.Log("[BuildModeController] Build mode OFF.");
    }

    private void OnUIFocusChanged(bool focused)
    {
        uiFocused = focused;
        if (focused && active) ExitBuildMode();
    }

    // ── Per-frame ghost ───────────────────────────────────────────

    private void Update()
    {
        if (!active) return;

        // Browsing and Placing need the cell under the crosshair.
        if (state == BuildState.Browsing || state == BuildState.Placing)
        {
            aimValid = Physics.Raycast(aimTransform.position, aimTransform.forward,
                                       out RaycastHit hit, aimDistance, groundMask, QueryTriggerInteraction.Ignore);
            if (aimValid) currentCell = grid.WorldToCell(hit.point);
        }

        if (ghost == null) return;          // Browsing carries no ghost

        if (state == BuildState.Placing && !aimValid)
        {
            if (ghost.activeSelf) ghost.SetActive(false);
            return;
        }
        if (!ghost.activeSelf) ghost.SetActive(true);

        Vector2Int fp = heldDef.Footprint;
        bool valid = state != BuildState.Deleting && grid.CanPlace(currentCell, fp, rotation);
        if (valid && state == BuildState.Editing) lastValidRotation = rotation;

        ghost.transform.SetPositionAndRotation(
            grid.FootprintCenter(currentCell, fp, rotation),
            Quaternion.Euler(0f, rotation, 0f));
        Tint(valid ? validColor : invalidColor);
    }

    // ── Inputs ────────────────────────────────────────────────────

    // 1-4: pick a hotbar piece → (re)enter Placing with it.
    private void OnSlot(int index)
    {
        if (!active || (state != BuildState.Browsing && state != BuildState.Placing)) return;
        if (!service.SelectPiece(index)) { Debug.Log($"[BuildModeController] Hotbar slot {index + 1} is empty."); return; }

        heldDef = service.ActivePiece;
        isExistingLift = false;
        rotation = 0;
        BuildGhost(heldDef.Prefab);
        state = BuildState.Placing;
    }

    // E: select the aimed placed piece for editing (lift it off the grid).
    private void OnEdit()
    {
        if (!active || state != BuildState.Browsing || !aimValid) return;
        if (!service.TryLift(currentCell, out var def, out var rot) || def == null)
        {
            Debug.Log("[BuildModeController] Nothing to edit at the aimed cell.");
            return;
        }
        BeginLiftedSelection(def, rot, BuildState.Editing);
        lastValidRotation = rot;            // it was valid where it sat
    }

    // Right-click: target the aimed placed piece for deletion (lift it; F confirms).
    private void OnDelete()
    {
        if (!active || state != BuildState.Browsing || !aimValid) return;
        if (!service.TryLift(currentCell, out var def, out var rot) || def == null)
        {
            Debug.Log("[BuildModeController] Nothing to delete at the aimed cell.");
            return;
        }
        BeginLiftedSelection(def, rot, BuildState.Deleting);
    }

    private void BeginLiftedSelection(FurnitureDefinitionSO def, int rot, BuildState next)
    {
        heldDef          = def;
        isExistingLift   = true;
        originalCell     = currentCell;
        originalRotation = rot;
        rotation         = rot;
        BuildGhost(def.Prefab);
        state = next;
    }

    // Left-click: pin a NEW piece at the aimed cell (Placing → Editing). Only on a free cell.
    private void OnPin()
    {
        if (!active || state != BuildState.Placing || !aimValid) return;
        if (!grid.CanPlace(currentCell, heldDef.Footprint, rotation))
        {
            Debug.Log("[BuildModeController] Can't pin here — cell blocked.");
            return;
        }
        lastValidRotation = rotation;
        state = BuildState.Editing;         // currentCell is now frozen
    }

    // R: rotate 90° (Placing & Editing only).
    private void OnRotate()
    {
        if (!active) return;
        if (state == BuildState.Placing || state == BuildState.Editing)
            rotation = (rotation + 90) % 360;
    }

    // F: confirm / save, by state.
    private void OnConfirm()
    {
        if (!active) return;
        switch (state)
        {
            case BuildState.Placing:
                OnPin();                    // F also pins; press F again in Editing to confirm
                break;

            case BuildState.Editing:
                if (grid.CanPlace(currentCell, heldDef.Footprint, rotation))
                {
                    service.TryPlace(heldDef, currentCell, rotation);
                    GoBrowsing();           // per design: back to Browsing after a confirm
                }
                else
                {
                    rotation = lastValidRotation;   // cancel the colliding turn, back to green
                    Debug.Log("[BuildModeController] Invalid position — reverted to last valid rotation.");
                }
                break;

            case BuildState.Deleting:
                // The piece is already lifted (removed); confirming accepts the deletion.
                heldDef = null; isExistingLift = false;
                GoBrowsing();
                break;
        }
    }

    // Esc: cancel the current selection first (restoring a lifted piece); in Browsing it exits.
    private void OnCancel()
    {
        if (!active) return;
        if (state == BuildState.Browsing) { ExitBuildMode(); return; }

        RestoreLiftedIfAny();
        GoBrowsing();
    }

    // ── Helpers ───────────────────────────────────────────────────

    // Re-places an existing piece that was lifted for edit/delete but not committed.
    private void RestoreLiftedIfAny()
    {
        if (isExistingLift && heldDef != null)
            service.TryPlace(heldDef, originalCell, originalRotation);
        isExistingLift = false;
    }

    private void GoBrowsing()
    {
        DestroyGhost();
        heldDef = null;
        isExistingLift = false;
        state = BuildState.Browsing;
    }

    private void BuildGhost(GameObject prefab)
    {
        DestroyGhost();
        ghost = Instantiate(prefab, transform);
        ghost.name = "[Ghost] " + prefab.name;

        // A preview must not collide nor block the aim ray.
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;

        ghostRenderers.Clear();
        ghostRenderers.AddRange(ghost.GetComponentsInChildren<Renderer>());
        if (ghostMaterial != null)
            foreach (var r in ghostRenderers) r.sharedMaterial = ghostMaterial;

        if (mpb == null) mpb = new MaterialPropertyBlock();
    }

    private void DestroyGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        ghostRenderers.Clear();
    }

    // Per-renderer tint without cloning the shared ghost material.
    private void Tint(Color c)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        foreach (var r in ghostRenderers)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }
}
