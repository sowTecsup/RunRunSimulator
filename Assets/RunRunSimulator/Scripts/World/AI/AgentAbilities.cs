using UnityEngine;
namespace MoriMonchiSimulator
{

internal sealed class AgentAbilities
{
    private struct Slot { public AbilitySO Ability; public float ReadyAt; public float FiredAt; public float Charge; public bool Requested; public float ReadySince; }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private readonly Slot[] slots = new Slot[3];

    internal bool ManualSupers;
    internal float AutoFireDelay = 5f;

    internal AgentAbilities(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].FiredAt = -1f;
            slots[i].ReadySince = -1f;
        }
    }

    internal void Bind(AbilitySO[] set)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Ability = set != null && i < set.Length ? set[i] : null;
            slots[i].ReadyAt = 0f;
            slots[i].FiredAt = -1f;
            slots[i].Charge = 0f;
            slots[i].Requested = false;
            slots[i].ReadySince = -1f;
        }
        RefreshStats();
    }

    internal void RefreshStats()
    {
        ctx.Stats = ExpeditionStats.Resolve(ExpeditionRulesSO.Current, ctx.Occupation,
            new[] { slots[0].Ability, slots[1].Ability, slots[2].Ability });
    }

    internal int Count => 3;

    internal AbilitySO Ability(int i) => i >= 0 && i < slots.Length ? slots[i].Ability : null;

    internal bool HasRequestedSuper
    {
        get
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Requested && slots[i].Charge >= 1f) return true;
            return false;
        }
    }

    internal float Charge01(int i)
    {
        if (i < 0 || i >= slots.Length) return 1f;
        var ability = slots[i].Ability;
        if (ability == null) return 1f;
        if (ability.Kind == AbilityKind.Damage && ability.Role == AbilityRole.Super)
            return Mathf.Clamp01(slots[i].Charge);
        if (Time.time >= slots[i].ReadyAt) return 1f;
        return 1f - Mathf.Clamp01((slots[i].ReadyAt - Time.time) / Mathf.Max(0.01f, ability.Cooldown));
    }

    internal float FiredAt(int i) => i >= 0 && i < slots.Length ? slots[i].FiredAt : -1f;

    internal bool IsReady(int i)
    {
        if (i < 0 || i >= slots.Length) return false;
        var ability = slots[i].Ability;
        if (ability == null) return false;
        if (ability.Kind == AbilityKind.Damage && ability.Role == AbilityRole.Super)
            return slots[i].Charge >= 1f;
        return Time.time >= slots[i].ReadyAt;
    }

    internal bool IsRequested(int i) => i >= 0 && i < slots.Length && slots[i].Requested;

    internal void Request(int i)
    {
        if (i < 0 || i >= slots.Length) return;
        var ability = slots[i].Ability;
        if (ability == null || ability.Kind != AbilityKind.Damage || ability.Role != AbilityRole.Super) return;
        if (!IsReady(i)) return;
        slots[i].Requested = true;
    }

    internal void AddCharge(AbilityChargeSource source)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var ability = slots[i].Ability;
            if (ability == null || ability.Kind != AbilityKind.Damage || ability.Role != AbilityRole.Super) continue;
            slots[i].Charge = Mathf.Clamp01(slots[i].Charge + ability.ChargeFor(source));
            if (slots[i].Charge >= 1f && slots[i].ReadySince < 0f)
                slots[i].ReadySince = Time.time;
        }
    }

    internal ClashMoveSO TryFireDamage(float dist, int rivalsNearby, bool allowBack)
    {
        int superDistanceIndex = -1;
        int superRivalsIndex   = -1;
        int superAnyIndex      = -1;

        for (int i = 0; i < slots.Length; i++)
        {
            var ability = slots[i].Ability;
            if (ability == null || ability.Kind != AbilityKind.Damage || ability.Move == null) continue;
            if (ability.Role != AbilityRole.Super) continue;
            if (!ability.Triggers(AbilityTrigger.RivalInReach)) continue;
            if (!IsReady(i)) continue;
            if (dist > ability.Move.Range) continue;
            if (dist < ability.MinDistance) continue;
            if (rivalsNearby < ability.MinRivalsNearby) continue;
            if (!allowBack && ability.Slot == ClashSlot.Back) continue;
            bool canAutoFire = !ManualSupers || slots[i].Requested ||
                (slots[i].ReadySince >= 0f && Time.time - slots[i].ReadySince >= AutoFireDelay);
            if (!canAutoFire) continue;

            if (ability.MinRivalsNearby > 0) { superRivalsIndex = i; break; }
            if (ability.MinDistance > 0f && superDistanceIndex < 0) superDistanceIndex = i;
            if (superAnyIndex < 0) superAnyIndex = i;
        }

        int chosenSuper = superRivalsIndex >= 0 ? superRivalsIndex : superDistanceIndex >= 0 ? superDistanceIndex : superAnyIndex;
        if (chosenSuper >= 0)
        {
            var superAbility = slots[chosenSuper].Ability;
            slots[chosenSuper].Charge = 0f;
            slots[chosenSuper].Requested = false;
            slots[chosenSuper].ReadySince = -1f;
            slots[chosenSuper].FiredAt = Time.time;
            return superAbility.Move;
        }

        int distanceIndex = -1;
        int rivalsIndex   = -1;
        int anyIndex      = -1;

        for (int i = 0; i < slots.Length; i++)
        {
            var ability = slots[i].Ability;
            if (ability == null || ability.Kind != AbilityKind.Damage || ability.Move == null) continue;
            if (ability.Role != AbilityRole.Basic) continue;
            if (!ability.Triggers(AbilityTrigger.RivalInReach)) continue;
            if (Time.time < slots[i].ReadyAt) continue;
            if (dist > ability.Move.Range) continue;
            if (dist < ability.MinDistance) continue;
            if (rivalsNearby < ability.MinRivalsNearby) continue;
            if (!allowBack && ability.Slot == ClashSlot.Back) continue;

            if (ability.MinRivalsNearby > 0) { rivalsIndex = i; break; }
            if (ability.MinDistance > 0f && distanceIndex < 0) distanceIndex = i;
            if (anyIndex < 0) anyIndex = i;
        }

        int chosen = rivalsIndex >= 0 ? rivalsIndex : distanceIndex >= 0 ? distanceIndex : anyIndex;
        if (chosen < 0) return null;

        var chosenAbility = slots[chosen].Ability;
        slots[chosen].FiredAt = Time.time;
        slots[chosen].ReadyAt = Time.time + chosenAbility.Cooldown;
        return chosenAbility.Move;
    }

    internal void TickMobility()
    {
        bool fleeing = owner.Intent == CreatureIntent.Fleeing || owner.Intent == CreatureIntent.Retreating;
        bool chasing = owner.Intent == CreatureIntent.Chasing || owner.Intent == CreatureIntent.Hunting;

        for (int i = 0; i < slots.Length; i++)
        {
            var ability = slots[i].Ability;
            if (ability == null || ability.Kind != AbilityKind.Mobility) continue;
            if (Time.time < slots[i].ReadyAt) continue;

            bool shouldFire = (ability.Triggers(AbilityTrigger.Fleeing) && fleeing) ||
                               (ability.Triggers(AbilityTrigger.Chasing) && chasing);
            if (!shouldFire) continue;

            ctx.SpeedMultiplier   = ability.SpeedMultiplier;
            ctx.SpeedBoostUntil   = Time.time + ability.BoostSeconds;
            slots[i].FiredAt = Time.time;
            slots[i].ReadyAt = Time.time + ability.Cooldown;
        }
    }

    internal void ResetForReuse()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].ReadyAt = 0f;
            slots[i].FiredAt = -1f;
            slots[i].Charge = 0f;
            slots[i].Requested = false;
            slots[i].ReadySince = -1f;
        }
        RefreshStats();
    }
}
}
