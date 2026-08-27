using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatInputController : MonoBehaviour
    {
        [SerializeField] private CombatPrototypeManager manager;
        [SerializeField] private TargetingController targeting;
        [SerializeField] private CombatBoardBuilder builder;

        private Vector2 _lastMousePosition;
        private Vector2Int? _dragOrigin;

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame)
            {
                manager.RestartEncounter();
            }

            if (manager.Phase != CombatPhase.Planning)
            {
                _dragOrigin = null;
            }

            if (manager.Phase != CombatPhase.Planning && manager.Phase != CombatPhase.Setup) return;

            if (kb.f1Key.wasPressedThisFrame) targeting.SelectUnit(0);
            if (kb.f2Key.wasPressedThisFrame) targeting.SelectUnit(1);
            if (kb.f3Key.wasPressedThisFrame) targeting.SelectUnit(2);

            if (kb.digit1Key.wasPressedThisFrame) targeting.SelectAbility(0);
            if (kb.digit2Key.wasPressedThisFrame) targeting.SelectAbility(1);
            if (kb.digit3Key.wasPressedThisFrame) targeting.SelectAbility(2);

            if (kb.qKey.wasPressedThisFrame) targeting.Rotate(-1);
            if (kb.eKey.wasPressedThisFrame) targeting.Rotate(1);

            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) manager.ConfirmAction();
            if (kb.tabKey.wasPressedThisFrame) manager.NewBeat();
            if (kb.backspaceKey.wasPressedThisFrame) manager.UndoLast();

            if (mouse == null) return;

            Vector2 mousePosition = mouse.position.ReadValue();

            if (mousePosition != _lastMousePosition)
            {
                _lastMousePosition = mousePosition;

                if (_dragOrigin == null && TryRaycastCell(mousePosition, out Vector2Int cell) && builder.Board.InBounds(cell))
                {
                    targeting.SetCursor(cell);
                }
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (TryRaycastCell(mousePosition, out Vector2Int cell) && builder.Board.InBounds(cell))
                {
                    if (manager.Phase == CombatPhase.Setup)
                    {
                        manager.PlaceAt(cell);
                        return;
                    }

                    if (TrySelectPlayerAt(cell))
                    {
                        _dragOrigin = null;
                    }
                    else
                    {
                        _dragOrigin = cell;
                        targeting.SetCursor(cell);
                    }
                }

                manager.HideBrief();
            }

            if (_dragOrigin != null && mouse.leftButton.isPressed)
            {
                if (TryRaycastCell(mousePosition, out Vector2Int dragCell) && dragCell != _dragOrigin.Value)
                {
                    targeting.SetDirection(AbilityTargeting.DominantCardinal(_dragOrigin.Value, dragCell));
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame && _dragOrigin != null)
            {
                manager.ConfirmAction();
                _dragOrigin = null;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (TryRaycastCell(mousePosition, out Vector2Int cell))
                {
                    manager.ShowBriefAt(cell, mousePosition);
                }
            }
        }

        private bool TrySelectPlayerAt(Vector2Int cell)
        {
            List<PlayerUnit> players = manager.Canonical.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Alive && players[i].Cell == cell)
                {
                    targeting.SelectUnit(i);
                    return true;
                }
            }

            return false;
        }

        private bool TryRaycastCell(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                return false;
            }

            cell = builder.Board.WorldToCell(hit.point);
            return true;
        }
    }
}
