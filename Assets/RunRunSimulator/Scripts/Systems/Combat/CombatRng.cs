namespace MoriMonchiSimulator
{

// Deterministic xorshift32 RNG for the combat simulator.
// Same seed → same sequence on any platform/runtime, so both clients replay
// identical combat. Never System.Random or UnityEngine.Random — always injected by parameter.
public class CombatRng
{
    private uint state;

    public CombatRng(int seed)
    {
        state = (uint)seed;
        if (state == 0u) state = 0x9E3779B9u;
    }

    public float NextFloat()
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state & 0xFFFFFFu) / 16777216f;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        return minInclusive + (int)(NextFloat() * (maxExclusive - minInclusive));
    }
}
}
