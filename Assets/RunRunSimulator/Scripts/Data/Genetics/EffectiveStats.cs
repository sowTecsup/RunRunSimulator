namespace MoriMonchiSimulator
{
public readonly struct EffectiveStats
{
    public readonly float Constitution;
    public readonly float Attack;
    public readonly float Speed;
    public readonly float Defense;
    public readonly float Luck;
    public readonly float Evasion;
    public EffectiveStats(float con, float atk, float spd, float def, float lck, float eva)
    { Constitution = con; Attack = atk; Speed = spd; Defense = def; Luck = lck; Evasion = eva; }
}
}
