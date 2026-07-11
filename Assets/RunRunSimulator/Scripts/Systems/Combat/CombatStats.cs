using UnityEngine;

namespace MoriMonchiSimulator
{
    public static class CombatStats
    {
        public const float BaseHpCombatMultiplier = 5f;

        public static EffectiveStats GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db)
            => GetEffectiveStats(dna, db, null);

        public static EffectiveStats GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db, RoleTableSO roles)
        {
            float con = dna.BaseConstitution;
            float atk = dna.BaseAttack;
            float spd = dna.BaseSpeed;

            if (roles != null)
            {
                var p = roles.GetProfile(dna.Role);
                con = Mathf.Clamp(dna.BaseConstitution + p.ConMod, CreatureGenerator.StatMin, CreatureGenerator.StatMax);
                atk = Mathf.Clamp(dna.BaseAttack + p.AtkMod,       CreatureGenerator.StatMin, CreatureGenerator.StatMax);
                spd = Mathf.Clamp(dna.BaseSpeed + p.SpdMod,        CreatureGenerator.StatMin, CreatureGenerator.StatMax);
            }

            AccumulatePart(db.GetBodyShape(dna.BodyShapeID), dna.BodyTier,  ref con, ref atk, ref spd);
            AccumulatePart(db.GetArm(dna.ArmID),             dna.ArmTier,   ref con, ref atk, ref spd);
            AccumulatePart(db.GetEye(dna.EyeID),             dna.EyeTier,   ref con, ref atk, ref spd);
            AccumulatePart(db.GetMouth(dna.MouthID),         dna.MouthTier, ref con, ref atk, ref spd);

            return new EffectiveStats(con, atk, spd, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        }

        private static void AccumulatePart(BodyPart part, Tier tier, ref float con, ref float atk, ref float spd)
        {
            if (part == null) return;
            int bonus = (int)tier - 1;
            con += part.HP     + bonus;
            atk += part.Attack + bonus;
            spd += part.Speed  + bonus;
        }
    }
}
