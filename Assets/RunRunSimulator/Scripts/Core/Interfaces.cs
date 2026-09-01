using UnityEngine;
namespace MoriMonchiSimulator
{

public interface IInteractable
{
    void Interact();
}

public interface IUINavigable
{
    void OnUINavigate(Vector2 dir);

    void OnUISubmit();

    bool OnUICancel();
}

public interface IThrowable
{
    bool IsHeld { get; }

    void OnGrab(Transform holdAnchor);

    void OnRelease();

    void OnThrow(Vector3 force);

    void Knock(Vector3 force);
}
}
