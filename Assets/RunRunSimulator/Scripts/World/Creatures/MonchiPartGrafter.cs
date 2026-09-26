using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{
public static class MonchiPartGrafter
{
    private const float BoundsScale = 2.5f;

    public static void Graft(GameObject partPrefab, GameObject bodyInstance, List<SkinnedMeshRenderer> into)
    {
        if (partPrefab == null || bodyInstance == null || into == null)
            return;

        var partInstance = Object.Instantiate(partPrefab, bodyInstance.transform);
        partInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        partInstance.transform.localScale = Vector3.one;

        var bodyBones = new Dictionary<string, Transform>();
        foreach (var candidate in bodyInstance.GetComponentsInChildren<Transform>(false))
        {
            if (candidate.IsChildOf(partInstance.transform) || bodyBones.ContainsKey(candidate.name))
                continue;

            bodyBones[candidate.name] = candidate;
        }

        SkinnedMeshRenderer boundsSource = null;
        foreach (var candidate in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (candidate.transform.IsChildOf(partInstance.transform) || candidate.rootBone == null)
                continue;

            if (boundsSource == null || candidate.localBounds.size.sqrMagnitude > boundsSource.localBounds.size.sqrMagnitude)
                boundsSource = candidate;
        }

        foreach (var renderer in partInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var partBones = renderer.bones;
            var newBones = new Transform[partBones.Length];

            for (int i = 0; i < partBones.Length; i++)
            {
                var partBone = partBones[i];
                if (partBone == null)
                {
                    newBones[i] = null;
                    continue;
                }

                if (bodyBones.TryGetValue(partBone.name, out var bodyBone))
                {
                    newBones[i] = bodyBone;
                }
                else
                {
                    Debug.LogWarning($"[MonchiPartGrafter] Missing bone '{partBone.name}' on body for part '{partPrefab.name}'.");
                    newBones[i] = partBone;
                }
            }

            var mesh = Object.Instantiate(renderer.sharedMesh);
            var bindPoses = mesh.bindposes;
            for (int i = 0; i < bindPoses.Length; i++)
            {
                var partBone = partBones[i];
                var newBone = newBones[i];
                if (partBone == null || newBone == null)
                    continue;

                bindPoses[i] = newBone.worldToLocalMatrix * partBone.localToWorldMatrix * bindPoses[i];
            }
            mesh.bindposes = bindPoses;

            renderer.sharedMesh = mesh;
            renderer.bones = newBones;

            if (boundsSource != null)
            {
                renderer.rootBone = boundsSource.rootBone;
                renderer.localBounds = new Bounds(boundsSource.localBounds.center, boundsSource.localBounds.size * BoundsScale);
            }
            else if (renderer.rootBone != null && bodyBones.TryGetValue(renderer.rootBone.name, out var bodyRoot))
            {
                renderer.rootBone = bodyRoot;
            }

            renderer.gameObject.layer = bodyInstance.layer;
            renderer.transform.SetParent(bodyInstance.transform, true);
            into.Add(renderer);
        }

        partInstance.SetActive(false);
        Object.Destroy(partInstance);
    }
}
}
