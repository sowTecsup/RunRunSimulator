namespace MoriMonchiSimulator.CombatPrototype
{
    public class EnemyUnit : CombatUnit
    {
        public EnemyDefinitionSO Definition;
        public bool HasReacted;
        public bool WasHitThisBeat;
        public EnemyIntent Intent;

        public override CombatUnit Clone()
        {
            EnemyUnit clone = new EnemyUnit();
            CopyBaseTo(clone);
            clone.Definition = Definition;
            clone.HasReacted = HasReacted;
            clone.WasHitThisBeat = WasHitThisBeat;
            clone.Intent = Intent != null ? Intent.Clone() : null;
            return clone;
        }
    }
}
