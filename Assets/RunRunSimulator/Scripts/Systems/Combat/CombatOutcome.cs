namespace MoriMonchiSimulator
{

public struct CombatOutcome
{
    public bool Won;
    public int HitsPlayer;
    public int HitsRival;
    public int Rounds;
    public int MaterialGained;
    public long CooldownUntilTicks;
}
}
