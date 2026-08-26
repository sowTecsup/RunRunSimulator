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

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame)
            {
                manager.RestartEncounter();
            }

            if (manager.Phase != CombatPhase.Planning) return;

            if (kb.f1Key.wasPressedThisFrame) targeting.SelectUnit(0);
            if (kb.f2Key.wasPressedThisFrame) targeting.SelectUnit(1);
            if (kb.f3Key.wasPressedThisFrame) targeting.SelectUnit(2);

            if (kb.digit1Key.wasPressedThisFrame) targeting.SelectAbility(0);
            if (kb.digit2Key.wasPressedThisFrame) targeting.SelectAbility(1);
            if (kb.digit3Key.wasPressedThisFrame) targeting.SelectAbility(2);

            if (kb.wKey.wasPressedThisFrame) targeting.MoveCursor(new Vector2Int(0, 1));
            if (kb.sKey.wasPressedThisFrame) targeting.MoveCursor(new Vector2Int(0, -1));
            if (kb.aKey.wasPressedThisFrame) targeting.MoveCursor(new Vector2Int(-1, 0));
            if (kb.dKey.wasPressedThisFrame) targeting.MoveCursor(new Vector2Int(1, 0));

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

                if (TryRaycastCell(mousePosition, out Vector2Int cell) && builder.Board.InBounds(cell))
                {
                    targeting.SetCursor(cell);
                }
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (TryRaycastCell(mousePosition, out Vector2Int cell) && builder.Board.InBounds(cell))
                {
                    if (!TrySelectPlayerAt(cell))
                    {
                        targeting.SetCursor(cell);
                        manager.ConfirmAction();
                    }
                }

                manager.HideBrief();
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
