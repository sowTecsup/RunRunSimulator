using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class SelectionFacingPreview : MonoBehaviour
    {
        [SerializeField] private TargetingController targeting;
        [SerializeField] private CombatPrototypeManager manager;

        private Vector2Int _lastApplied;
        private int _lastUnitId = -1;

        private void OnEnable()
        {
            if (targeting != null) targeting.SelectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            if (targeting != null) targeting.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (manager == null || targeting == null) return;
            if (manager.Phase != CombatPhase.Planning) return;
            if (targeting.SelectedUnitId < 0 || targeting.SelectedAbilityIndex < 0) return;

            if (targeting.CurrentDirection == _lastApplied && targeting.SelectedUnitId == _lastUnitId) return;

            if (manager.Views != null && manager.Views.TryGetValue(targeting.SelectedUnitId, out var view))
            {
                view.SetFacingInstant(targeting.CurrentDirection);
                _lastApplied = targeting.CurrentDirection;
                _lastUnitId = targeting.SelectedUnitId;
            }
        }
    }
}
