using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderGaitMonitor : MonoBehaviour
    {
        [SerializeField] private SpiderLegStepper[] legs;
        [SerializeField] private bool show = true;
        [SerializeField] private float panelWidth = 250f;

        private int[] steps;
        private float[] maxDrag;
        private float[] waitTimer;
        private float[] sumWait;
        private float[] maxWait;
        private bool[] wasStepping;

        private void Awake()
        {
            if (legs != null)
            {
                Alloc();
            }
        }

        private void Alloc()
        {
            int count = legs.Length;
            steps = new int[count];
            maxDrag = new float[count];
            waitTimer = new float[count];
            sumWait = new float[count];
            maxWait = new float[count];
            wasStepping = new bool[count];
        }

        public void ResetStats()
        {
            Alloc();
        }

        private void Update()
        {
            if (legs == null || steps == null || steps.Length != legs.Length) return;
            for (int i = 0; i < legs.Length; i++)
            {
                var leg = legs[i];
                if (leg == null) continue;
                float drag = leg.Drag;
                if (drag > maxDrag[i]) maxDrag[i] = drag;
                bool stepping = leg.IsStepping;
                if (stepping && !wasStepping[i])
                {
                    steps[i]++;
                    sumWait[i] += waitTimer[i];
                    if (waitTimer[i] > maxWait[i]) maxWait[i] = waitTimer[i];
                    waitTimer[i] = 0f;
                }
                else if (!stepping && leg.WantsStep)
                {
                    waitTimer[i] += Time.deltaTime;
                }
                wasStepping[i] = stepping;
            }
        }

        public string Report()
        {
            if (steps == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < legs.Length; i++)
            {
                var leg = legs[i];
                if (leg == null) continue;
                float avgWait = steps[i] > 0 ? sumWait[i] / steps[i] : 0f;
                sb.AppendLine(leg.name + " steps=" + steps[i] + " maxDrag=" + maxDrag[i].ToString("F2") + " avgWait=" + avgWait.ToString("F2") + " maxWait=" + maxWait[i].ToString("F2"));
            }
            return sb.ToString();
        }

        private void OnGUI()
        {
            if (!show || legs == null || steps == null) return;
            float scale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(Screen.width / scale - panelWidth - 10f, 10f, panelWidth, 30f + legs.Length * 78f), GUI.skin.box);
            GUILayout.Label("GAIT MONITOR");
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                var leg = legs[i];
                GUI.color = leg.IsStepping ? Color.red : (leg.WantsStep ? new Color(1f, 0.6f, 0f) : Color.green);
                GUILayout.Label(leg.name + (leg.IsStepping ? " PISANDO" : (leg.WantsStep ? " ESPERA" : " apoyada")));
                GUI.color = Color.white;
                float avg = steps[i] > 0 ? sumWait[i] / steps[i] : 0f;
                GUILayout.Label("  pasos " + steps[i] + " | drag " + leg.Drag.ToString("F2") + " (max " + maxDrag[i].ToString("F2") + ")");
                GUILayout.Label("  espera avg " + avg.ToString("F2") + "s | max " + maxWait[i].ToString("F2") + "s");
            }
            if (GUILayout.Button("Reset stats")) ResetStats();
            GUILayout.EndArea();
        }
    }
}
