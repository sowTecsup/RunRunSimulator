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
    [Tooltip("FLOOR layers the placement ray hits to find the cell under the crosshair (Placing). Set this to your floor layer so the ray passes through furniture.")]
    [SerializeField] private LayerMask floorMask = ~0;
    [Tooltip("FURNITURE layers the selection ray hits to pick a placed piece (Edit / Delete).")]
    [SerializeField] private LayerMask furnitureMask;
    [Tooltip("OBSTACLE layers that block placement by PHYSICAL overlap (walls, fixed scenery, props not on the grid). The ghost turns red if its footprint box overlaps any collider here. Do NOT include the floor layer.")]
    [SerializeField] private LayerMask obstacleMask;
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
    private float currentY;                  // floor Y under currentCell — follows aim, frozen once pinned
    private bool floorFlat;                  // is the floor under currentCell flat enough to build on
    private float ghostHalfHeight = 0.5f;    // half-height of the ghost mesh, for the obstacle box

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

        // Only Placing follows the floor under the crosshair. Editing/Deleting are fixed at the
        // selected cell, and selection (Edit/Delete) does its own furniture raycast on demand.
        if (state == BuildState.Placing)
        {
            // The camera ray picks the cell (XZ); the floor Y + slope come from a vertical probe
            // at that cell, so the preview sits exactly where the spawner will re-seat it.
            aimValid = Physics.Raycast(aimTransform.position, aimTransform.forward,
                                       out RaycastHit hit, aimDistance, floorMask, QueryTriggerInteraction.Ignore);
            if (aimValid)
            {
                currentCell = grid.WorldToCell(hit.point);
                aimValid = grid.TrySampleFloor(currentCell, heldDef.Footprint, rotation, out currentY, out floorFlat);
            }
        }

        if (ghost == null) return;          // Browsing carries no ghost

        if (state == BuildState.Placing && !aimValid)
        {
            if (ghost.activeSelf) ghost.SetActive(false);
            return;
        }
        if (!ghost.activeSelf) ghost.SetActive(true);

        Vector2Int fp = heldDef.Footprint;
        bool valid = state != BuildState.Deleting && PlacementValid();
        if (valid && state == BuildState.Editing) lastValidRotation = rotation;

        Vector3 pos = grid.FootprintCenter(currentCell, fp, rotation);
        pos.y = currentY;                   // snap base to the real floor (irregular terrain)
        ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
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

    // E: select the placed piece UNDER THE CROSSHAIR for editing (aim at the mesh, lift it).
    private void OnEdit()
    {
        if (!active || state != BuildState.Browsing) return;
        if (!TryPickFurnitureCell(out var cell)) { Debug.Log("[BuildModeController] No furniture under the crosshair to edit."); return; }
        if (!service.TryLift(cell, out var def, out var rot) || def == null) return;

        BeginLiftedSelection(def, rot, cell, BuildState.Editing);
        lastValidRotation = rot;            // it was valid where it sat
    }

    // Right-click: target the placed piece UNDER THE CROSSHAIR for deletion (lift it; F confirms).
    private void OnDelete()
    {
        if (!active || state != BuildState.Browsing) return;
        if (!TryPickFurnitureCell(out var cell)) { Debug.Log("[BuildModeController] No furniture under the crosshair to delete."); return; }
        if (!service.TryLift(cell, out var def, out var rot) || def == null) return;

        BeginLiftedSelection(def, rot, cell, BuildState.Deleting);
    }

    private void BeginLiftedSelection(FurnitureDefinitionSO def, int rot, Vector2Int cell, BuildState next)
    {
        heldDef          = def;
        isExistingLift   = true;
        originalCell     = cell;
        originalRotation = rot;
        rotation         = rot;
        currentCell      = cell;            // ghost sits here (frozen while Editing/Deleting)
        grid.TrySampleFloor(cell, def.Footprint, rot, out currentY, out floorFlat);
        BuildGhost(def.Prefab);
        state = next;
    }

    // Raycasts the FURNITURE layers; if it hits a placed piece, returns its anchor cell (from
    // the PlacedFurnitureMarker the spawner stamps). Lets you select by pointing at the mesh.
    private bool TryPickFurnitureCell(out Vector2Int cell)
    {
        cell = default;
        if (!Physics.Raycast(aimTransform.position, aimTransform.forward,
                             out RaycastHit hit, aimDistance, furnitureMask, QueryTriggerInteraction.Ignore))
            return false;

        var marker = hit.collider.GetComponentInParent<PlacedFurnitureMarker>();
        if (marker == null) return false;
        cell = marker.AnchorCell;
        return true;
    }

    // Left-click: pin a NEW piece at the aimed cell (Placing → Editing). Only on a free cell.
    private void OnPin()
    {
        if (!active || state != BuildState.Placing || !aimValid) return;
        if (!PlacementValid())
        {
            Debug.Log("[BuildModeController] Can't pin here — cell blocked, sloped, or overlapping an obstacle.");
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
                if (PlacementValid())
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

    // Single source of "can the held piece sit at currentCell": free cells + flat floor +
    // no physical overlap with an obstacle. Used by the tint, OnPin and OnConfirm so they agree.
    private bool PlacementValid()
    {
        if (heldDef == null) return false;
        Vector2Int fp = heldDef.Footprint;
        return grid.CanPlace(currentCell, fp, rotation)
            && floorFlat
            && !OverlapsObstacle(currentCell, fp, rotation);
    }

    // Physical overlap test: an oriented box over the footprint (XZ from the grid, height from the
    // ghost mesh) against obstacleMask. The ghost's own colliders are disabled, and a lifted piece
    // is already despawned, so neither self-triggers. A small inset avoids catching flush neighbours.
    private bool OverlapsObstacle(Vector2Int cell, Vector2Int fp, int rot)
    {
        if (obstacleMask == 0) return false;
        Vector2Int r = (Mathf.Abs(rot % 180) == 0) ? fp : new Vector2Int(fp.y, fp.x);
        Vector3 half = new Vector3(r.x * grid.CellSize * 0.5f - 0.02f, ghostHalfHeight,
                                   r.y * grid.CellSize * 0.5f - 0.02f);
        Vector3 center = grid.FootprintCenter(cell, fp, rot);
        center.y = currentY + ghostHalfHeight;
        return Physics.CheckBox(center, half, Quaternion.Euler(0f, rot, 0f), obstacleMask,
                                QueryTriggerInteraction.Ignore);
    }

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

        // Mesh half-height drives the obstacle overlap box (footprint gives XZ, this gives Y).
        if (ghostRenderers.Count > 0)
        {
            Bounds b = ghostRenderers[0].bounds;
            for (int i = 1; i < ghostRenderers.Count; i++) b.Encapsulate(ghostRenderers[i].bounds);
            ghostHalfHeight = Mathf.Max(0.05f, b.extents.y);
        }

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
