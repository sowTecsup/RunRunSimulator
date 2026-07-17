using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderDevPanel : MonoBehaviour
    {
        [SerializeField] private SpiderTuningSO tuning;
        [SerializeField] private SpiderRagdollMode ragdollMode;
        [SerializeField] private SpiderBodyController controller;
        [SerializeField] private SpiderJump jump;
        [SerializeField] private Rigidbody rootBody;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float panelWidth = 290f;
        [SerializeField] private bool show = true;

        private Vector3 spawnPos;
        private Quaternion spawnRot;

        private void Awake()
        {
            if (spawnPoint != null) { spawnPos = spawnPoint.position; spawnRot = spawnPoint.rotation; }
            else if (ragdollMode != null) { spawnPos = ragdollMode.transform.position; spawnRot = ragdollMode.transform.rotation; }
        }

        private void OnGUI()
        {
            if (!show || tuning == null) return;

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(10f, 10f, panelWidth, Screen.height / scale - 20f), GUI.skin.box);
            GUILayout.Label("<b>SPIDER PLAYGROUND</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(4f);

            GUILayout.Label("Cuerpo");
            tuning.moveSpeed = Row("Velocidad", tuning.moveSpeed, 0.1f, 4f);
            tuning.turnSpeed = Row("Giro", tuning.turnSpeed, 30f, 270f);
            tuning.rideHeight = Row("Altura", tuning.rideHeight, 0.2f, 1.1f);

            GUILayout.Space(6f);
            GUILayout.Label("Patas");
            tuning.stepDistance = Row("Largo de paso", tuning.stepDistance, 0.05f, 0.6f);
            tuning.stepHeight = Row("Alto de paso", tuning.stepHeight, 0.02f, 0.4f);
            tuning.stepDuration = Row("Duracion", tuning.stepDuration, 0.04f, 0.5f);
            tuning.footSplay = Row("Apertura", tuning.footSplay, 0.3f, 2f);
            tuning.stepOvershoot = Row("Anticipacion", tuning.stepOvershoot, 0f, 0.3f);
            tuning.anticipation = Row("Prediccion", tuning.anticipation, 0f, 0.5f);
            tuning.maxTwist = Row("Torsion max", tuning.maxTwist, 5f, 60f);

            GUILayout.Space(6f);
            GUILayout.Label("Cuerpo vivo");
            tuning.idleAmount = Row("Idle", tuning.idleAmount, 0f, 1f);
            tuning.bobAmount = Row("Bob caminar", tuning.bobAmount, 0f, 0.06f);
            tuning.leanAmount = Row("Inclinacion", tuning.leanAmount, 0f, 1f);
            tuning.elasticAmount = Row("Elasticidad", tuning.elasticAmount, 0f, 1f);
            tuning.jumpImpulse = Row("Salto", tuning.jumpImpulse, 1f, 6f);

            GUILayout.Space(6f);
            GUILayout.Label("Ragdoll");
            tuning.launchImpulse = Row("Impulso", tuning.launchImpulse, 1f, 12f);

            GUILayout.Space(8f);
            bool isRag = ragdollMode != null && ragdollMode.IsRagdoll;
            GUI.color = isRag ? Color.red : Color.green;
            GUILayout.Label(isRag ? "MODO: RAGDOLL" : "MODO: WALK");
            GUI.color = Color.white;

            if (controller != null && GUILayout.Button(controller.AutoWalk ? "Auto-caminar: ON" : "Auto-caminar: OFF", GUILayout.Height(22f)))
            {
                controller.AutoWalk = !controller.AutoWalk;
            }
            if (jump != null && GUILayout.Button("Saltar! (Space)", GUILayout.Height(26f))) jump.Jump();
            if (GUILayout.Button(isRag ? "Volver a caminar" : "Activar ragdoll", GUILayout.Height(26f)))
            {
                if (ragdollMode != null) ragdollMode.SetRagdoll(!isRag);
            }
            if (GUILayout.Button("Lanzar!", GUILayout.Height(26f))) Launch();
            if (GUILayout.Button("Reset", GUILayout.Height(22f))) ResetSpider();

            GUILayout.Space(6f);
            GUILayout.Label("WASD para mover", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            GUILayout.EndArea();

#if UNITY_EDITOR
            if (GUI.changed) UnityEditor.EditorUtility.SetDirty(tuning);
#endif
        }

        private float Row(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(96f));
            float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120f));
            GUILayout.Label(v.ToString("F2"), GUILayout.Width(38f));
            GUILayout.EndHorizontal();
            return v;
        }

        private void Launch()
        {
            if (ragdollMode == null || rootBody == null) return;
            ragdollMode.SetRagdoll(true);
            Vector3 dir = (ragdollMode.transform.forward * 0.35f + Vector3.up).normalized;
            rootBody.linearVelocity = dir * tuning.launchImpulse;
            rootBody.angularVelocity = new Vector3(2f, 1f, 3f);
        }

        private void ResetSpider()
        {
            if (ragdollMode == null) return;
            ragdollMode.SetRagdoll(false);
            ragdollMode.transform.position = spawnPos;
            ragdollMode.transform.rotation = spawnRot;
            if (rootBody != null)
            {
                rootBody.linearVelocity = Vector3.zero;
                rootBody.angularVelocity = Vector3.zero;
            }
        }
    }
}
