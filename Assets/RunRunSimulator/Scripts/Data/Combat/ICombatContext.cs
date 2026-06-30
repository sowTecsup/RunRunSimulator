namespace MoriMonchiSimulator
{

public interface ICombatContext
{
    void DamageOpponent(float amount, string source);
    void HealSelf(float amount, string source);
    void ApplyStatusToOpponent(ModifierEffectKind kind, int turns, int magnitude, string source);
    void ApplyStatusToSelf(ModifierEffectKind kind, int turns, int magnitude, string source);
    void StunOpponent(int turns);
}
}
