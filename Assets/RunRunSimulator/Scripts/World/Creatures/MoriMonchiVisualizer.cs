using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Assembles the 3D model of a MoriMochi from part prefabs stored in a PartVisualBankSO.
//
// The six socket Transforms (bodySocket … mouthSocket) live as named children of modelRoot
// and define WHERE each slot attaches. Create them once with the [Setup] button.
// The body prefab is NOT responsible for knowing about arms/eyes/mouth — that was wrong.
// The Visualizer IS the socket map; each prefab just knows its own insertion joint.
//
// Lives on the agent root alongside MoriMochiAgent. MoriMonchiController wires them together.
public class MoriMonchiVisualizer : MonoBehaviour
{
    [Required, SerializeField] private Transform modelRoot;

    [Title("Sockets")]
    [Tooltip("Where the body prefab inserts. Created by the Setup button.")]
    [SerializeField] private Transform bodySocket;

    [Tooltip("Left arm socket.")]
    [SerializeField] private Transform armSocketL;

    [Tooltip("Right arm socket — R instance gets localScale.x flipped when isMirror = true.")]
    [SerializeField] private Transform armSocketR;

    [Tooltip("Left eye socket.")]
    [SerializeField] private Transform eyeSocketL;

    [Tooltip("Right eye socket.")]
    [SerializeField] private Transform eyeSocketR;

    [Tooltip("Mouth socket.")]
    [SerializeField] private Transform mouthSocket;

    private static readonly int BaseColorId   = Shader.PropertyToID("_BaseColor");
    private static readonly int Shade1ColorId = Shader.PropertyToID("_1st_ShadeColor");
    private static readonly int Shade2ColorId = Shader.PropertyToID("_2nd_ShadeColor");
    private static readonly int RimColorId    = Shader.PropertyToID("_RimLightColor");

    private FurTypeDatabaseSO furDatabase;

    // Root transforms of the instantiated parts — exposed for future procedural animation.
    [ShowInInspector, ReadOnly] public Transform   BodyTransform  { get; private set; }
    [ShowInInspector, ReadOnly] public Transform[] ArmTransforms  { get; private set; } = new Transform[2];
    [ShowInInspector, ReadOnly] public Transform[] EyeTransforms  { get; private set; } = new Transform[2];
    [ShowInInspector, ReadOnly] public Transform   MouthTransform { get; private set; }

    // ── Editor Setup ──────────────────────────────────────────────

    // Creates the six socket GameObjects as children of modelRoot and assigns the
    // serialized references. Safe to re-run — skips sockets that already exist.
    [Button("Setup", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void Setup()
    {
        if (modelRoot == null) { Debug.LogWarning("[MoriMonchiVisualizer] Assign modelRoot first."); return; }
        bodySocket  = GetOrCreateSocket("Body");
        armSocketL  = GetOrCreateSocket("Arm_L");
        armSocketR  = GetOrCreateSocket("Arm_R");
        eyeSocketL  = GetOrCreateSocket("Eye_L");
        eyeSocketR  = GetOrCreateSocket("Eye_R");
        mouthSocket = GetOrCreateSocket("Mouth");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[MoriMonchiVisualizer] Sockets ready under modelRoot.");
    }

    private Transform GetOrCreateSocket(string socketName)
    {
        var existing = modelRoot.Find(socketName);
        if (existing != null) return existing;

        var go = new GameObject(socketName);
        go.transform.SetParent(modelRoot, false);
        return go.transform;
    }

    // ── Runtime ───────────────────────────────────────────────────

    public void SetFurDatabase(FurTypeDatabaseSO furDb)
    {
        furDatabase = furDb;
    }

    public void RefreshFur(CreatureDNA dna) => ApplyFur(dna);

    // Builds (or rebuilds) the model for the given DNA. Safe to call on pool reuse.
    // Each socket's children are destroyed; sockets themselves are kept.
    public void Assemble(CreatureDNA dna, PartVisualBankSO bank)
    {
        ClearModel();

        if (bank == null)
        {
            Debug.LogWarning("[MoriMonchiVisualizer] No PartVisualBankSO — model will be empty.");
            return;
        }

        // Body (single, no mirror)
        var bodyPrefab = bank.GetBody(dna.BodyShapeID);
        if (bodyPrefab != null)
            BodyTransform = AttachPart(bodyPrefab, bodySocket, mirror: false);

        // Arms — same prefab, R instance mirrored when BodyPartJoint.isMirror is true.
        var armPrefab = bank.GetArm(dna.ArmID);
        ArmTransforms[0] = AttachPart(armPrefab, armSocketL, mirror: false);
        ArmTransforms[1] = AttachPart(armPrefab, armSocketR, mirror: armPrefab != null && armPrefab.isMirror);

        // Eyes
        var eyePrefab = bank.GetEye(dna.EyeID);
        EyeTransforms[0] = AttachPart(eyePrefab, eyeSocketL, mirror: false);
        EyeTransforms[1] = AttachPart(eyePrefab, eyeSocketR, mirror: eyePrefab != null && eyePrefab.isMirror);

        // Mouth (single, no mirror)
        MouthTransform = AttachPart(bank.GetMouth(dna.MouthID), mouthSocket, mirror: false);

        ApplyFur(dna);
    }

    // ── Private ───────────────────────────────────────────────────

    // Instantiates the prefab as a child of the socket, aligns its insertionJoint to
    // the socket origin, and optionally flips localScale.x for mirrored parts.
    private static Transform AttachPart(BodyPartJoint prefab, Transform socket, bool mirror)
    {
        if (prefab == null || socket == null) return null;

        var part = Object.Instantiate(prefab, socket);
        part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        // Shift so the insertionJoint sits exactly at the socket's origin.
        if (part.insertionJoint != null)
            part.transform.localPosition = -part.insertionJoint.localPosition;

        if (mirror)
        {
            var s = part.transform.localScale;
            s.x = -s.x;
            part.transform.localScale = s;
        }

        return part.transform;
    }

    // Destroy part instances under each socket, leaving the socket Transforms intact.
    private void ClearModel()
    {
        ClearSocket(bodySocket);
        ClearSocket(armSocketL);
        ClearSocket(armSocketR);
        ClearSocket(eyeSocketL);
        ClearSocket(eyeSocketR);
        ClearSocket(mouthSocket);

        BodyTransform  = null;
        ArmTransforms  = new Transform[2];
        EyeTransforms  = new Transform[2];
        MouthTransform = null;
    }

    private static void ClearSocket(Transform socket)
    {
        if (socket == null) return;
        for (int i = socket.childCount - 1; i >= 0; i--)
            Object.Destroy(socket.GetChild(i).gameObject);
    }

    private void ApplyFur(CreatureDNA dna)
    {
        var furMat = furDatabase != null
            ? furDatabase.GetMaterial(dna.FurType)
            : null;

        var palette = ColorGenetics.BuildFurPalette(dna.BaseColor, dna.SecondaryColor);

        var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(BaseColorId, palette.Base);
        mpb.SetColor(Shade1ColorId, palette.Shade1);
        mpb.SetColor(Shade2ColorId, palette.Shade2);
        mpb.SetColor(RimColorId, palette.Rim);

        foreach (var r in renderers)
        {
            if (furMat != null)
                r.sharedMaterial = furMat;
            r.SetPropertyBlock(mpb);
        }
    }

    // ── Gizmos ───────────────────────────────────────────────────

    // Draw each socket's position in scene view using distinct colors per slot type.
    // These gizmos are always visible (not just on selection) so you can inspect wiring
    // without entering play mode.
    private void OnDrawGizmos()
    {
        DrawSocket(bodySocket,  Color.white);
        DrawSocket(armSocketL,  new Color(0.4f, 0.8f, 1f));   // cyan — arms
        DrawSocket(armSocketR,  new Color(0.4f, 0.8f, 1f));
        DrawSocket(eyeSocketL,  new Color(1f, 0.65f, 0.2f));  // orange — eyes
        DrawSocket(eyeSocketR,  new Color(1f, 0.65f, 0.2f));
        DrawSocket(mouthSocket, new Color(0.9f, 0.3f, 0.6f)); // pink — mouth
    }

    private static void DrawSocket(Transform t, Color c)
    {
        if (t == null) return;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(t.position, 0.05f);
    }
}
}
