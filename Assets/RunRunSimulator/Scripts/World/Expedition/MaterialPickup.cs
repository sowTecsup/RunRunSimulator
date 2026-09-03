using UnityEngine;

namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Perceivable))]
public class MaterialPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int value = 1;

    public int Value => value;
    public bool Taken { get; private set; }

    internal void SetValue(int newValue) => value = Mathf.Max(1, newValue);

    public bool TryTake(out int taken)
    {
        if (Taken) { taken = 0; return false; }
        Taken = true;
        taken = value;
        gameObject.SetActive(false);
        return true;
    }
}
}
