using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stateless service — called by GameManager. All state lives in the passed-in databases.
public static class BreedingService
{
    public const int MaxBreedCount = 4;

    // Validates parents, builds a child DNA by inheriting from the genealogical tree,
    // wires up MotherID / FatherID on the child, and increments BreedCount on both parents.
    // Returns null if any validation fails.
    public static CreatureDNA Breed(
        string                 motherID,
        string                 fatherID,
        CreatureRegistrySO     registry,
        CreatureDatabaseSO     partDb,
        InheritanceOddsTableSO odds)
    {
        if (!registry.TryGet(motherID, out var mother))
        {
            Debug.LogError($"[BreedingService] Mother ID '{motherID}' not found in registry.");
            return null;
        }
        if (!registry.TryGet(fatherID, out var father))
        {
            Debug.LogError($"[BreedingService] Father ID '{fatherID}' not found in registry.");
            return null;
        }
        if (mother.IsDead || father.IsDead)
        {
            Debug.LogError("[BreedingService] Cannot breed: one or both creatures are dead.");
            return null;
        }
        if (mother.Gender != CreatureGender.Female || father.Gender != CreatureGender.Male)
        {
            Debug.LogError("[BreedingService] Breeding requires one Female (mother) and one Male (father).");
            return null;
        }
        if (mother.BreedCount >= MaxBreedCount)
        {
            Debug.LogError($"[BreedingService] Mother has reached max breeds ({MaxBreedCount}).");
            return null;
        }
        if (father.BreedCount >= MaxBreedCount)
        {
            Debug.LogError($"[BreedingService] Father has reached max breeds ({MaxBreedCount}).");
            return null;
        }

        var child = new CreatureDNA
        {
            BodyShapeID  = ResolveSlot(PartRole.Body,  motherID, fatherID, registry, partDb, odds),
            ArmID        = ResolveSlot(PartRole.Arm,   motherID, fatherID, registry, partDb, odds),
            EyeID        = ResolveSlot(PartRole.Eye,   motherID, fatherID, registry, partDb, odds),
            MouthID      = ResolveSlot(PartRole.Mouth, motherID, fatherID, registry, partDb, odds),
            PrimaryColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f),
            Gender       = Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female,
            MotherID     = motherID,
            FatherID     = fatherID,
            BaseHP       = InheritStat(mother.BaseHP,     father.BaseHP),
            BaseAttack   = InheritStat(mother.BaseAttack, father.BaseAttack),
            BaseSpeed    = InheritStat(mother.BaseSpeed,  father.BaseSpeed),
        };

        mother.BreedCount++;
        father.BreedCount++;

        return child;
    }

    // ── Private helpers ───────────────────────────────────────────

    private static string ResolveSlot(
        PartRole               role,
        string                 motherID,
        string                 fatherID,
        CreatureRegistrySO     registry,
        CreatureDatabaseSO     partDb,
        InheritanceOddsTableSO odds)
    {
        var slot = odds.Roll();

        // levels=0 → parents themselves, levels=1 → grandparents, levels=2 → great-grandparents
        string partID = slot switch
        {
            InheritanceOddsTableSO.Slot.Parent           => PickFromLevel(role, 0, motherID, fatherID, registry),
            InheritanceOddsTableSO.Slot.Grandparent      => PickFromLevel(role, 1, motherID, fatherID, registry),
            InheritanceOddsTableSO.Slot.GreatGrandparent => PickFromLevel(role, 2, motherID, fatherID, registry),
            _                                            => null  // Mutation and Base → random
        };

        // Fallback: any random part from the full pool (covers Mutation, Base, and empty ancestry)
        return partID ?? RandomPartID(role, partDb);
    }

    // Walks the genealogical tree 'levels' generations above [motherID, fatherID],
    // collects all ancestors at that level, then picks one random part ID from among them.
    // Returns null if no ancestors exist at that depth (wild-caught lineage).
    private static string PickFromLevel(
        PartRole         role,
        int              levels,
        string           motherID,
        string           fatherID,
        CreatureRegistrySO registry)
    {
        var generation = ExpandGenerations(new[] { motherID, fatherID }, levels, registry);

        var candidates = generation
            .Select(id => registry.TryGet(id, out var c) ? SlotPartID(c, role) : null)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // Climbs the family tree 'levels' steps from the origin IDs.
    private static List<string> ExpandGenerations(
        IEnumerable<string> origins,
        int                 levels,
        CreatureRegistrySO    registry)
    {
        var current = origins.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

        for (int i = 0; i < levels; i++)
        {
            var next = new List<string>();
            foreach (var id in current)
                if (registry.TryGet(id, out var c))
                {
                    if (!string.IsNullOrEmpty(c.MotherID)) next.Add(c.MotherID);
                    if (!string.IsNullOrEmpty(c.FatherID)) next.Add(c.FatherID);
                }
            current = next;
        }
        return current;
    }

    private static string SlotPartID(CreatureDNA dna, PartRole role) => role switch
    {
        PartRole.Body  => dna.BodyShapeID,
        PartRole.Arm   => dna.ArmID,
        PartRole.Eye   => dna.EyeID,
        PartRole.Mouth => dna.MouthID,
        _              => ""
    };

    private static string RandomPartID(PartRole role, CreatureDatabaseSO partDb) => role switch
    {
        PartRole.Body  => partDb.BodyShapes?.GetRandomPart()?.ID ?? "",
        PartRole.Arm   => partDb.Arms?.GetRandomPart()?.ID       ?? "",
        PartRole.Eye   => partDb.Eyes?.GetRandomPart()?.ID       ?? "",
        PartRole.Mouth => partDb.Mouths?.GetRandomPart()?.ID     ?? "",
        _              => ""
    };

    // 50/50 inherit from mother or father, then apply a random delta of -1, 0, or +1.
    // Result is clamped to a minimum of 1.
    private static float InheritStat(float motherStat, float fatherStat)
    {
        float inherited = Random.value < 0.5f ? motherStat : fatherStat;
        int   delta     = Random.Range(-1, 2);   // -1, 0, or +1 with equal probability
        return Mathf.Max(1f, inherited + delta);
    }
}
