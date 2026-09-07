namespace MoriMonchiSimulator
{

public static class ArenaOrderCatalog
{
    public static readonly string[] PillarLabels = { "BOTÍN", "ENCUENTRO", "EQUIPO" };

    private static readonly string[] LootLabels = { "Grande", "Pequeño" };
    private static readonly string[] ContactLabels = { "Escapar", "Enfrentar" };
    private static readonly string[] PostureLabels = { "Proteger", "Agresivo" };

    public static string ChoiceLabel(OrderPillar pillar, int choice)
    {
        string[] labels = pillar == OrderPillar.Loot ? LootLabels : pillar == OrderPillar.Contact ? ContactLabels : PostureLabels;
        return choice >= 0 && choice < labels.Length ? labels[choice] : "";
    }

    public static string ArchetypeShort(ArenaOrders o)
    {
        if (o.Contact == ContactChoice.Fight)
            return o.Posture == PostureChoice.Protect ? "Guardián" : "Cazador";
        return o.Posture == PostureChoice.Protect ? "Recolector" : "Señuelo";
    }

    public static string LootPlace(ArenaOrders o) => o.Loot == LootChoice.Big ? "del centro" : "de las vetas";

    public static string ArchetypeName(ArenaOrders o) => ArchetypeShort(o) + " " + LootPlace(o);

    public static string ArchetypeDescription(ArenaOrders o)
    {
        string where = o.Loot == LootChoice.Big ? "el cristal grande" : "las vetas chicas";
        if (o.Contact == ContactChoice.Fight)
        {
            if (o.Posture == PostureChoice.Protect)
                return "Se planta en " + where + " y embiste a quien se acerque. No recolecta ni persigue.";
            return "Ronda " + where + " cazando rivales cargados para que suelten. Si no hay a quién cazar, mina un poco.";
        }
        if (o.Posture == PostureChoice.Protect)
            return "Mina " + where + " y huye con la carga apenas ve un rival. Nunca pelea.";
        return "Se acerca a los rivales de " + where + ", los provoca y huye; se lleva lo que cae. No pelea ni mina.";
    }

    public static string PastVerb(ArenaOrders o)
    {
        if (o.Contact == ContactChoice.Fight)
            return o.Posture == PostureChoice.Protect ? "vigiló" : "cazó";
        return o.Posture == PostureChoice.Protect ? "recolectó" : "distrajo";
    }

    public static string PersonalityName(CreatureDNA dna, ExpeditionRulesSO rules)
    {
        if (dna == null || rules == null) return "Equilibrado";
        bool bold = dna.Boldness >= rules.BoldFightLock;
        bool shy = dna.Boldness <= rules.ShyFleeLock;
        bool social = dna.Sociability >= rules.SocialProtectLock;
        bool loner = dna.Sociability <= rules.LonerAggressiveLock;

        if (bold && loner) return "Osado solitario";
        if (bold && social) return "Osado sociable";
        if (shy && social) return "Tímido sociable";
        if (shy && loner) return "Tímido solitario";
        if (bold) return "Osado";
        if (shy) return "Tímido";
        if (social) return "Sociable";
        if (loner) return "Solitario";
        return "Equilibrado";
    }

    public static string CounterHint(ArenaOrders o)
    {
        if (o.Contact == ContactChoice.Fight)
            return o.Posture == PostureChoice.Protect
                ? "Frena cazadores y cubre a quien mina a su lado. Lo saca del puesto un señuelo."
                : "Vacía recolectores sin guardián; si lo tumban se retira. No toca a quien está custodiado y muerde el anzuelo de un señuelo.";
        return o.Posture == PostureChoice.Protect
            ? "Es quien puntúa. Con un guardián a menos de 6 m no huye. Lo caza un cazador."
            : "Arrastra guardianes y cazadores lejos de su puesto y espanta recolectores. Sin quien aproveche el hueco, no rinde.";
    }

    public static string TeamPlanName(System.Collections.Generic.IReadOnlyList<ArenaOrders> orders)
    {
        int guards = 0, hunters = 0, miners = 0, decoys = 0, big = 0;
        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            if (o.Loot == LootChoice.Big) big++;
            if (o.Contact == ContactChoice.Fight) { if (o.Posture == PostureChoice.Protect) guards++; else hunters++; }
            else { if (o.Posture == PostureChoice.Protect) miners++; else decoys++; }
        }

        int count = orders.Count;
        if (count == 0) return "";
        string where = big * 2 > count ? " del centro" : big * 2 < count ? " de las vetas" : " repartida";
        if (miners == 0) return "Sin recolector: solo puntúa lo que minen en ratos libres";
        if (hunters >= 2) return "Jauría" + where;
        if (guards >= 2) return "Doble guardia" + where;
        if (guards == 1 && hunters == 1) return "Fortín" + where;
        if (guards == 1 && decoys == 1) return "Guardia con señuelo" + where;
        if (guards == 1) return "Muralla" + where;
        if (hunters == 1 && decoys == 1) return "Emboscada" + where;
        if (hunters == 1) return "Cacería" + where;
        if (decoys >= 1) return "Engaño" + where;
        return big * 2 > count ? "Codicia" : "Hormiguero";
    }

    public static string RivalRead(CreatureDNA dna, ExpeditionRulesSO rules)
    {
        bool lockedContact = ArenaOrderRules.IsLocked(dna, rules, OrderPillar.Contact, out int contact);
        bool lockedPosture = ArenaOrderRules.IsLocked(dna, rules, OrderPillar.Posture, out int posture);
        if (lockedContact && lockedPosture)
            return ArchetypeShort(new ArenaOrders(LootChoice.Big, (ContactChoice)contact, (PostureChoice)posture)).ToLowerInvariant() + " seguro";
        if (lockedContact) return contact == (int)ContactChoice.Fight ? "guardián o cazador" : "recolector o señuelo";
        if (lockedPosture) return posture == (int)PostureChoice.Protect ? "guardián o recolector" : "cazador o señuelo";
        return "puede hacer cualquiera";
    }

    public static string UnlockRead(CreatureDNA dna, ExpeditionRulesSO rules)
    {
        bool lockedContact = ArenaOrderRules.IsLocked(dna, rules, OrderPillar.Contact, out int contact);
        bool lockedPosture = ArenaOrderRules.IsLocked(dna, rules, OrderPillar.Posture, out int posture);
        if (lockedContact && lockedPosture)
            return "desbloquea " + ArchetypeShort(new ArenaOrders(LootChoice.Big, (ContactChoice)contact, (PostureChoice)posture)).ToLowerInvariant();
        if (lockedContact) return contact == (int)ContactChoice.Fight ? "elige guardián o cazador" : "elige recolector o señuelo";
        if (lockedPosture) return posture == (int)PostureChoice.Protect ? "elige guardián o recolector" : "elige cazador o señuelo";
        return "elige las cuatro posturas";
    }

    public static string RoomText(ArenaRoomRead read)
    {
        string terrain = read.Obstacles >= 11 ? "cubierta (" + read.Obstacles + " obstáculos para escapar)" : read.Obstacles >= 7 ? "mixta (" + read.Obstacles + " obstáculos)" : "abierta (" + read.Obstacles + " obstáculos)";
        string veins = read.VeinCount == 0 ? "sin vetas" : read.VeinCount + " vetas chicas (" + read.VeinTotal + " en total, la más cercana a " + read.NearVeinDistance.ToString("0") + " m de tu salida)";
        return "Botín: cristal central de " + read.LodeValue + " a " + read.CenterDistance.ToString("0") + " m · " + veins + " · Sala " + terrain;
    }

    public static string LockReason(OrderPillar pillar, int forced)
    {
        if (pillar == OrderPillar.Contact)
            return forced == (int)ContactChoice.Fight ? "nunca huye" : "nunca pelea";
        if (pillar == OrderPillar.Posture)
            return forced == (int)PostureChoice.Protect ? "nunca deja al grupo" : "va a lo suyo";
        return "";
    }
}
}
