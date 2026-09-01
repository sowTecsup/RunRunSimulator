using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ControllerPool
{
    private readonly Queue<MoriMonchiController> pool = new Queue<MoriMonchiController>();
    private readonly MoriMonchiController prefab;

    public ControllerPool(MoriMonchiController prefab) => this.prefab = prefab;

    public int Count => pool.Count;

    public MoriMonchiController Get(Vector3 pos)
    {
        MoriMonchiController controller = null;
        while (controller == null && pool.Count > 0) controller = pool.Dequeue();

        if (controller == null)
        {
            if (prefab == null)
            {
                Debug.LogError("[ControllerPool] No creature prefab assigned.");
                return null;
            }
            return Object.Instantiate(prefab, pos, Quaternion.identity);
        }

        controller.transform.SetPositionAndRotation(pos, Quaternion.identity);
        controller.gameObject.SetActive(true);
        return controller;
    }

    public void Return(MoriMonchiController controller)
    {
        if (controller == null) return;
        controller.PrepareForPool();
        controller.gameObject.SetActive(false);
        pool.Enqueue(controller);
    }
}
}
