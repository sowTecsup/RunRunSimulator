namespace MoriMonchiSimulator.CombatPrototype
{
    public class PlayerUnit : CombatUnit
    {
        public PlayerUnitDefinitionSO Definition;

        public override CombatUnit Clone()
        {
            PlayerUnit clone = new PlayerUnit();
            CopyBaseTo(clone);
            clone.Definition = Definition;
            return clone;
        }
    }
}
