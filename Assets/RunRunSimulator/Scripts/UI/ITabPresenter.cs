namespace MoriMonchiSimulator
{

public interface ITabPresenter
{
    void Enter();
    bool Navigate(int h, int v);
    void Submit();
    bool Cancel();
    void ClearFocus();
    void Rebuild();
    void Teardown();
}
}
