using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

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

    private FurnitureDefinitionSO heldDef;
    private int rotation;
    private int lastValidRotation;

    private bool isExistingLift;
    private Vector2Int originalCell;
    private int originalRotation;

    private Vector2Int currentCell;
    private bool aimValid;
    private float currentY;
    private bool floorFlat;
    private float ghostHalfHeight = 0.5f;

    private GameObject ghost;
    private readonly List<Renderer> ghostRenderers = new List<Renderer>();
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

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

        if (active) ExitBuildMode();
    }

    private void Toggle()
    {
        if (active) { ExitBuildMode(); return; }
        if (uiFocused) return;
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
        RestoreLiftedIfAny();
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

    private void Update()
    {
        if (!active) return;

        if (state == BuildState.Placing)
        {
            aimValid = Physics.Raycast(aimTransform.position, aimTransform.forward,
                                       out RaycastHit hit, aimDistance, floorMask, QueryTriggerInteraction.Ignore);
            if (aimValid)
            {
                currentCell = grid.WorldToCell(hit.point);
                aimValid = grid.TrySampleFloor(currentCell, heldDef.Footprint, rotation, out currentY, out floorFlat);
            }
        }

        if (ghost == null) return;

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
        pos.y = currentY;
        ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        Tint(valid ? validColor : invalidColor);
    }

    private void OnSlot(int index)
    {
        if (!active || (state != BuildState.Browsing && state != BuildState.Placing)) return;
        if (!service.SelectPiece(index)) { Debug.Log($"[BuildModeController] Hotbar slot {index + 1} is empty."); return; }
        StartPlacing(service.ActivePiece);
    }

    public void SelectPieceFromBrowser(FurnitureDefinitionSO def)
    {
        if (!active) return;
        if (state != BuildState.Browsing && state != BuildState.Placing) return;
        if (!service.SetActivePiece(def)) return;
        StartPlacing(def);
    }

    private void StartPlacing(FurnitureDefinitionSO def)
    {
        if (def == null) return;
        heldDef = def;
        isExistingLift = false;
        rotation = 0;
        BuildGhost(heldDef.Prefab);
        state = BuildState.Placing;
    }

    private void OnEdit()
    {
        if (!active || state != BuildState.Browsing) return;
        if (!TryPickFurnitureCell(out var cell)) { Debug.Log("[BuildModeController] No furniture under the crosshair to edit."); return; }
        if (!service.TryLift(cell, out var def, out var rot) || def == null) return;

        BeginLiftedSelection(def, rot, cell, BuildState.Editing);
        lastValidRotation = rot;
    }

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
        currentCell      = cell;
        grid.TrySampleFloor(cell, def.Footprint, rot, out currentY, out floorFlat);
        BuildGhost(def.Prefab);
        state = next;
    }

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

    private void OnPin()
    {
        if (!active || state != BuildState.Placing || !aimValid) return;
        if (!PlacementValid())
        {
            Debug.Log("[BuildModeController] Can't pin here — cell blocked, sloped, or overlapping an obstacle.");
            return;
        }
        lastValidRotation = rotation;
        state = BuildState.Editing;
    }

    private void OnRotate()
    {
        if (!active) return;
        if (state == BuildState.Placing || state == BuildState.Editing)
            rotation = (rotation + 90) % 360;
    }

    private void OnConfirm()
    {
        if (!active) return;
        switch (state)
        {
            case BuildState.Placing:
                OnPin();
                break;

            case BuildState.Editing:
                if (PlacementValid())
                {
                    service.TryPlace(heldDef, currentCell, rotation);
                    GoBrowsing();
                }
                else
                {
                    rotation = lastValidRotation;
                    Debug.Log("[BuildModeController] Invalid position — reverted to last valid rotation.");
                }
                break;

            case BuildState.Deleting:
                heldDef = null; isExistingLift = false;
                GoBrowsing();
                break;
        }
    }

    private void OnCancel()
    {
        if (!active) return;
        if (state == BuildState.Browsing) { ExitBuildMode(); return; }

        RestoreLiftedIfAny();
        GoBrowsing();
    }

    private bool PlacementValid()
    {
        if (heldDef == null) return false;
        Vector2Int fp = heldDef.Footprint;
        return grid.CanPlace(currentCell, fp, rotation)
            && floorFlat
            && !OverlapsObstacle(currentCell, fp, rotation);
    }

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

        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;

        ghostRenderers.Clear();
        ghostRenderers.AddRange(ghost.GetComponentsInChildren<Renderer>());
        if (ghostMaterial != null)
            foreach (var r in ghostRenderers) r.sharedMaterial = ghostMaterial;

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
}
