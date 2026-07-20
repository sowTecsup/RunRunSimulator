using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    public class SpiderPaletteApplier : MonoBehaviour
    {
        [SerializeField] private Transform[] bodyRoots;
        [SerializeField] private Transform[] faceRoots;
        [SerializeField] private Transform[] detailRoots;

        public void Apply(Color baseColor, Color secondaryColor)
        {
            Color accent = ColorGenetics.DeriveSecondary(secondaryColor);

            Tint(bodyRoots, baseColor);
            Tint(faceRoots, secondaryColor);
            Tint(detailRoots, accent);
        }

        public void ApplyFromDna(CreatureDNA dna)
        {
            if (dna == null) return;

            Apply(dna.BaseColor, dna.SecondaryColor);
        }

        public void ApplyMaterial(Material material)
        {
            if (material == null) return;
            Swap(bodyRoots, material);
            Swap(faceRoots, material);
        }

        private void Swap(Transform[] roots, Material material)
        {
            if (roots == null) return;
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    r.sharedMaterials = mats;
                }
            }
        }

        private void Tint(Transform[] roots, Color color)
        {
            if (roots == null) return;

            foreach (Transform root in roots)
            {
                if (root == null) continue;

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", color);
                    mpb.SetColor("_Color", color);
                    renderer.SetPropertyBlock(mpb);
                }
            }
        }
    }
}
