namespace MoriMonchiSimulator
{
    public static class LocEnumMaps
    {
        public static string RoleName(Role role) => Loc.Tr("role." + KeyOf(role));

        public static string ElementName(Element element) => Loc.Tr("element." + KeyOf(element));

        public static string EquipmentSlotName(EquipmentSlot slot) => Loc.Tr("equipslot." + KeyOf(slot));

        public static string PartRoleName(PartRole part) => Loc.Tr("part." + KeyOf(part));

        public static string LifeStageName(LifeStage stage) => Loc.Tr("stage." + KeyOf(stage));

        public static string IntentName(CreatureIntent intent) => Loc.Tr("intent." + KeyOf(intent));

        public static string GenderName(CreatureGender gender) => Loc.Tr("gender." + KeyOf(gender));

        public static string StatAbbrev(StatType stat) => Loc.Tr("stat." + KeyOf(stat));

        public static string RarityName(Rarity rarity) => Loc.Tr("rarity." + KeyOf(rarity));

        public static string FurnitureCategoryName(FurnitureCategory category) => Loc.Tr("furniturecategory." + KeyOf(category));

        static string KeyOf(System.Enum value) => value.ToString().ToLowerInvariant();
    }
}
