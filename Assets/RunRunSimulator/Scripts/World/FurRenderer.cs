using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FurRenderer : MonoBehaviour
{
    [Header("Shell Config")]
    public Shader furShader;
    [Range(4, 64)] public int shellCount = 24;
    public float furLength = 0.12f;

    [Header("Appearance")]
    public Color baseColor     = new Color(0.55f, 0.35f, 0.15f);
    public Color tipColor      = new Color(1.00f, 0.90f, 0.65f);
    [Range(20, 200)] public float furDensity = 90f;
    [Range(0.05f, 0.5f)] public float strandThickness = 0.28f;

    [Header("Dynamics")]
    public float windStrength    = 0.04f;
    public float windSpeed       = 1.2f;
    public float gravityStrength = 0.18f;

    [Header("Lighting")]
    public Color rimColor  = new Color(0.8f, 0.6f, 1.0f);
    [Range(1, 8)] public float rimPower = 4f;
    [Range(0, 1)] public float specular   = 0.25f;
    [Range(0, 1)] public float smoothness = 0.30f;

    GameObject[] _shells;
    Material[]   _mats;
    bool         _built;

    void OnEnable()  => Build();
    void OnDisable() => Teardown();

    void Build()
    {
        if (_built) Teardown();

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || furShader == null) return;

        _shells = new GameObject[shellCount];
        _mats   = new Material[shellCount];

        for (int i = 0; i < shellCount; i++)
        {
            var go  = new GameObject($"_FurShell_{i}") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);

            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();

            var mat = new Material(furShader);
            SetProps(mat, i);
            mr.sharedMaterial = mat;

            _shells[i] = go;
            _mats[i]   = mat;
        }
        _built = true;
    }

    void SetProps(Material mat, int index)
    {
        mat.SetColor("_BaseColor",       baseColor);
        mat.SetColor("_TipColor",        tipColor);
        mat.SetFloat("_FurLength",       furLength);
        mat.SetFloat("_ShellIndex",      index);
        mat.SetFloat("_ShellCount",      shellCount);
        mat.SetFloat("_FurDensity",      furDensity);
        mat.SetFloat("_StrandThick",     strandThickness);
        mat.SetFloat("_WindStrength",    windStrength);
        mat.SetFloat("_WindSpeed",       windSpeed);
        mat.SetFloat("_GravityStrength", gravityStrength);
        mat.SetColor("_RimColor",        rimColor);
        mat.SetFloat("_RimPower",        rimPower);
        mat.SetFloat("_Specular",        specular);
        mat.SetFloat("_Smoothness",      smoothness);
    }

    void Teardown()
    {
        if (_shells != null)
        {
            foreach (var s in _shells) if (s) DestroyImmediate(s);
            _shells = null;
        }
        if (_mats != null)
        {
            foreach (var m in _mats) if (m) DestroyImmediate(m);
            _mats = null;
        }
        _built = false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!_built) return;
        if (_mats == null) return;
        for (int i = 0; i < _mats.Length && i < shellCount; i++)
            if (_mats[i]) SetProps(_mats[i], i);
    }
#endif
}
