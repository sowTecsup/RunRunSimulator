using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class WorldLabelBillboard : MonoBehaviour
    {
        private Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }

            transform.rotation = _cam.transform.rotation;
        }
    }
}
