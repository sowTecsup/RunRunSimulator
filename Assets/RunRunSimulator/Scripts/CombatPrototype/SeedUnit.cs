namespace MoriMonchiSimulator.CombatPrototype
{
    public class SeedUnit : CombatUnit
    {
        public override CombatUnit Clone()
        {
            SeedUnit clone = new SeedUnit();
            CopyBaseTo(clone);
            return clone;
        }
    }
}
