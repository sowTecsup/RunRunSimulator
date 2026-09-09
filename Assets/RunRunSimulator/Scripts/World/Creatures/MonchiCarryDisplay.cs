using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class MonchiCarryDisplay : MonoBehaviour
    {
        [Required, SerializeField] private MoriMochiAgent agent;
        [Required, SerializeField] private MonchiVisualizer visualizer;
        [Required, AssetsOnly, SerializeField] private GameObject crystalPrefab;
        [SerializeField] private string boneName = "Spine1";
        [SerializeField] private float baseHeight = 0.45f;
        [SerializeField] private float stackStep = 0.2f;
        [SerializeField] private float jitter = 0.08f;
        [SerializeField, Min(1)] private int maxShown = 6;

        private Transform anchor;
        private readonly List<Transform> shown = new List<Transform>();

        private void LateUpdate()
        {
            int carried = Mathf.Min(agent.Carried, maxShown);

            if (anchor == null)
            {
                anchor = FindBone(visualizer.ModelRoot, boneName);
                shown.RemoveAll(t => t == null);
            }

            if (anchor == null)
            {
                for (int i = 0; i < shown.Count; i++)
                    if (shown[i] != null) shown[i].gameObject.SetActive(false);
                return;
            }

            while (shown.Count < carried)
            {
                Transform instance = Instantiate(crystalPrefab, transform).transform;
                shown.Add(instance);
            }

            for (int i = 0; i < shown.Count; i++)
            {
                Transform instance = shown[i];
                if (instance == null) continue;

                if (i >= carried)
                {
                    if (instance.gameObject.activeSelf) instance.gameObject.SetActive(false);
                    continue;
                }

                if (!instance.gameObject.activeSelf)
                {
                    instance.localScale = Vector3.one;
                    instance.gameObject.SetActive(true);
                }

                float jx = jitter * (i % 2 == 0 ? 1f : -1f) * (i % 3 == 0 ? 0.5f : 1f);
                float jz = jitter * ((i / 2) % 2 == 0 ? -0.6f : 0.6f);
                instance.position = anchor.position + transform.up * (baseHeight + stackStep * i) + transform.right * jx + transform.forward * jz;
                instance.rotation = transform.rotation * Quaternion.Euler(0f, 37f * i, 0f);
            }
        }

        private static Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindBone(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private void OnDisable()
        {
            for (int i = 0; i < shown.Count; i++)
                if (shown[i] != null) shown[i].gameObject.SetActive(false);
        }
    }
}
