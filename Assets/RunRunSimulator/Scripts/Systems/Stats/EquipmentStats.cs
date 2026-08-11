namespace MoriMonchiSimulator
{
    public static class EquipmentStats
    {
        public static EffectiveStats Apply(EffectiveStats baseStats, CreatureDNA dna, EquipmentDatabaseSO db)
        {
            if (db == null || dna?.Equipped == null || dna.Equipped.Count == 0) return baseStats;

            var flat = new float[6];
            var add  = new float[6];
            var mul  = new float[6];
            for (int i = 0; i < 6; i++) mul[i] = 1f;

            foreach (var id in dna.Equipped.Values)
            {
                var item = db.GetByID(id);
                if (item?.Effects == null) continue;

                foreach (var effect in item.Effects)
                {
                    if (!(effect is StatModifierEffect sme) || sme.Modifiers == null) continue;
                    foreach (var m in sme.Modifiers)
                    {
                        int s = (int)m.Stat;
                        if (s < 0 || s > 5) continue;
                        switch (m.Type)
                        {
                            case ModifierType.Flat:        flat[s] += m.Value;             break;
                            case ModifierType.PercentAdd:  add[s]  += m.Value;             break;
                            case ModifierType.PercentMult: mul[s]  *= 1f + m.Value / 100f; break;
                        }
                    }
                }
            }

            return new EffectiveStats(
                Resolve(baseStats.Constitution, StatType.Constitution, flat, add, mul),
                Resolve(baseStats.Attack,       StatType.Attack,       flat, add, mul),
                Resolve(baseStats.Speed,        StatType.Speed,        flat, add, mul),
                Resolve(baseStats.Defense,      StatType.Defense,      flat, add, mul),
                Resolve(baseStats.Luck,         StatType.Luck,         flat, add, mul),
                Resolve(baseStats.Evasion,      StatType.Evasion,      flat, add, mul));
        }

        private static float Resolve(float baseVal, StatType stat, float[] flat, float[] add, float[] mul)
        {
            int s = (int)stat;
            float v = (baseVal + flat[s]) * (1f + add[s] / 100f) * mul[s];
            return v < 0f ? 0f : v;
        }
    }
}
