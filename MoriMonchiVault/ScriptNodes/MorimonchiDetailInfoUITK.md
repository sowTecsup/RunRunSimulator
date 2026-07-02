---
tags: [script, ui, creature-detail]
---

# MorimonchiDetailInfoUITK

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel modal detalle de criatura (5 tabs: Info/Combate/Linaje/Descendencia/Equipo). **Tab Info:** stats base con bonus de partes (6: CON/ATK/SPD/DEF/LCK/EVA), identidad, personalidad, partes, progresión. **Tab Combate (S33+S34):** historial con tarjetas compact — badge, fecha, rival con propietario, **swatch color + stats compact de ambos + chips de tiers, comentario y boton replay**. **Tab Linaje/Descendencia:** árbol genealógico. **Tab Equipo (S26-28):** dos columnas — izquierda ScrollView con card por slot, derecha swatch MM + stats Base→Final. `IUINavigable` (A/D cambian tabs). **S33:** Tab Equipo es clickeable → abre `EquipmentBackpackUITK` popup. **S34:** Tab Combate rediseñada con tiers visuales y replay button.

## Tabs

| Tab | Contenido |
|-----|----------|
| **Info** | Stats (base+equipment), identidad, personalidad, partes, progresión |
| **Combate (S34)** | Historial con tarjetas compactas — badge, fecha, rival, **swatch + stats compactos, tiers, comentario, boton replay** |
| **Linaje** | Árbol genealógico (padres/abuelos) |
| **Descendencia** | Árbol de crías |
| **Equipo** | Slots equipables (clickeable), stats Base→Final con delta |

## Cambios S33

**Tab Combate reescrita:**
- `BuildCombatCard(record)` construye tarjeta con badge, opponent row, stats snapshots
- `BuildStatsColumn()` muestra 6 stats desde CombatFighterSnapshot
- `AddCombatStatLabel()` agrega labels de stat a columna
- Fallback para records viejos (SelfStats == null): "Combate antiguo — sin stats"

**Tab Equipo → Popup:**
- Click en card → `backpack.Open()` abre popup mochila
- `OnRegistryChanged()` re-Populate en place al equipar

## Cambios S34

**Tab Combate rediseño completo:**

```csharp
private VisualElement BuildCombatCard(CreatureDNA dna, CombatRecord rec)
{
    bool won  = rec.Outcome == CombatOutcome.Won;
    bool draw = rec.Outcome == CombatOutcome.Draw;

    var card = new VisualElement();
    card.AddToClassList("combat-card");
    card.AddToClassList(draw ? "combat-card--draw" : won ? "combat-card--win" : "combat-card--lose");

    // Header: badge + fecha
    var header = new VisualElement();
    header.AddToClassList("combat-card__header");
    var badge = new Label(BadgeText(rec.Outcome));
    badge.AddToClassList("combat-card__badge");
    badge.AddToClassList(draw ? "combat-card__badge--draw" : won ? "combat-card__badge--win" : "combat-card__badge--lose");
    header.Add(badge);
    var date = new Label($"{rec.Date.ToLocalTime():dd/MM/yyyy HH:mm}");
    date.AddToClassList("combat-card__date");
    header.Add(date);
    card.Add(header);

    // Rival row: "vs rival · de {jugador}|local"
    string owner = string.IsNullOrEmpty(rec.OpponentPlayerName) ? " · local" : $" · de {rec.OpponentPlayerName}";
    var opponent = new Label($"vs {rec.OpponentName}{owner}");
    opponent.AddToClassList("combat-card__opponent");
    card.Add(opponent);

    // Body: 2 columnas Tú/Rival si stats disponibles
    if (rec.SelfStats != null && rec.OpponentStats != null)
    {
        var body = new VisualElement();
        body.AddToClassList("combat-card__body");
        body.Add(BuildCombatColumn("Tú", rec.SelfStats, dna.BaseColor));
        body.Add(BuildCombatColumn("Rival", rec.OpponentStats, new Color(0.22f, 0.22f, 0.28f)));
        card.Add(body);
    }
    else
    {
        var noStats = new Label("Combate antiguo — sin stats registradas");
        noStats.AddToClassList("combat-card__nostats");
        card.Add(noStats);
    }

    // Comentario
    var comment = new Label(CommentText(rec));
    comment.AddToClassList("combat-card__comment");
    card.Add(comment);

    // Footer con boton replay
    var footer = new VisualElement();
    footer.AddToClassList("combat-card__footer");
    var play = new Button(() => CombatReplayRequest.Request(dna, rec)) { text = "▶" };
    play.AddToClassList("combat-card__play");
    play.SetEnabled(CombatReplayRequest.CanReplay(dna, rec, registry));
    footer.Add(play);
    card.Add(footer);

    return card;
}
```

**BuildCombatColumn (S34 versión compacta):**

```csharp
private static VisualElement BuildCombatColumn(string title, CombatFighterSnapshot s, Color fallbackColor)
{
    var col = new VisualElement();
    col.AddToClassList("combat-card__colstats");

    // Header con swatch color + titulo
    var head = new VisualElement();
    head.AddToClassList("combat-card__colhead");
    var swatch = new VisualElement();
    swatch.AddToClassList("combat-card__swatch");
    swatch.style.backgroundColor = SnapshotColor(s, fallbackColor);  // ColorHex del snapshot
    head.Add(swatch);
    var titleLabel = new Label(title);
    titleLabel.AddToClassList("combat-card__col-title");
    head.Add(titleLabel);
    col.Add(head);

    // 2 lineas compactas de stats
    var line1 = new Label($"HP {s.MaxHp:0} · ATK {s.Attack:0} · SPD {s.Speed:0}");
    line1.AddToClassList("combat-card__stat");
    col.Add(line1);

    var line2 = new Label($"DEF {s.Defense:0} · LCK {s.Luck:0} · EVA {s.Evasion:0}");
    line2.AddToClassList("combat-card__stat");
    col.Add(line2);

    // Chips de tiers >1
    AddTierChips(col, s);

    return col;
}
```

**SnapshotColor (S34):**

```csharp
private static Color SnapshotColor(CombatFighterSnapshot s, Color fallback) =>
    !string.IsNullOrEmpty(s.ColorHex) && ColorUtility.TryParseHtmlString("#" + s.ColorHex, out var c)
        ? c
        : fallback;
```

Mapea ColorHex del snapshot a Color, fallback al color base UI (dna.BaseColor o gris para rival).

**AddTierChips (S34):**

```csharp
private static void AddTierChips(VisualElement col, CombatFighterSnapshot s)
{
    var chips = new VisualElement();
    chips.AddToClassList("combat-card__chips");

    AddTierChip(chips, "Cuerpo", s.BodyTier);
    AddTierChip(chips, "Brazo",  s.ArmTier);
    AddTierChip(chips, "Ojo",    s.EyeTier);
    AddTierChip(chips, "Boca",   s.MouthTier);

    if (chips.childCount > 0) col.Add(chips);
}

private static void AddTierChip(VisualElement chips, string partEs, int tier)
{
    if (tier <= 1) return;  // Sólo mostrar si tier >= 2
    var chip = new Label($"{partEs} T{tier}");
    chip.AddToClassList("combat-card__tierchip");
    chips.Add(chip);
}
```

Renderiza chips "Brazo T2", "Ojo T3", etc. solo si tier > 1.

**CommentText (S34):**

```csharp
private static string CommentText(CombatRecord rec)
{
    if (rec.Outcome == CombatOutcome.Won)
        return string.IsNullOrEmpty(rec.EvolvedSlot) ? "Victoria — sin mejora" : $"Se mejoró {PartEs(rec.EvolvedSlot)}";
    if (rec.Outcome == CombatOutcome.Lost)
        return rec.Died ? "Murió en combate" : "Regresó derrotado";
    return "Empate — sin consecuencias";
}
```

Línea de comentario contextual bajo los stats.

**Boton Replay (S34):**

```csharp
var play = new Button(() => CombatReplayRequest.Request(dna, rec)) { text = "▶" };
play.AddToClassList("combat-card__play");
play.SetEnabled(CombatReplayRequest.CanReplay(dna, rec, registry));
footer.Add(play);
```

Boton ▶ en footer, habilitado si `CanReplay()`. Llama `CombatReplayRequest.Request()` para cargar escena visualizador.

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `MorimonchiDetailInfoUITK.cs` | Núcleo, Info, Combat (S34), Equipo |
| `MorimonchiDetailInfoUITK.Trees.cs` | Tabs Linaje/Descendencia |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `database` | `CreatureDatabaseSO` | Partes |
| `equipmentDatabase` | `EquipmentDatabaseSO` | Items |
| `equipmentPalette` | `EquipmentPaletteSO` | Colores rareza/slot |
| `backpack` | `EquipmentBackpackUITK` | S33 Popup mochila equipo |
| `sortingOrder` | `int` | Orden de rendering |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `current` | `CreatureDNA` | S33 Criatura actualmente mostrada |
| `registry` | `CreatureRegistrySO` | Registry al momento de Show(), para replay |

## Métodos Privados (S34 nuevos/modificados)

| Método | Descripción |
|--------|-------------|
| `BuildCombatHistory(dna)` | Itera CombatHistory newest-first, agrega vía BuildCombatCard |
| `BuildCombatCard(dna, record)` | **S34 reescrito** Card compacta con stats+tiers+comentario+replay |
| `BuildCombatColumn(title, snapshot, fallback)` | **S34** 2 lineas stats + swatch color + chips tiers |
| `SnapshotColor(snapshot, fallback)` | **S34** Resuelve ColorHex del snapshot |
| `AddTierChips(col, snapshot)` | **S34** Agrega chips de tiers >1 |
| `AddTierChip(chips, part, tier)` | **S34** Helper para 1 chip |
| `CommentText(record)` | **S34** Linea descriptiva del outcome |
| `AddEquipCard(dna, slot)` | S33 Click abre popup backpack |
| `OnRegistryChanged(_)` | S33 Suscriptor: re-Populate |

## Vinculado a

- [[Index/05 - UI System]]
- [[CreatureGridUITK]] — abre este panel
- [[EquipmentBackpackUITK]] — S33 popup
- [[CombatReplayRequest]] — S34 replay request button
- [[CreatureDNA]] — fuente de datos
- [[CombatStats]], [[EquipmentStats]] — cálculos
- [[CombatRecord]], [[CombatFighterSnapshot]] — S33/S34 stats + tiers
- [[GameEvents]] — suscriptor

## Conexiones

**Entrada:**
- `UIManager.OnCreatureSelected` → `Show()` + `Populate()`
- `GameEvents.OnRegistryChanged` → `OnRegistryChanged()` → re-Populate

**Salida:**
- UI visual (5 tabs, cards, combat replay)
- `CombatReplayRequest.Request()` → carga escena combate (S34)

## Notas

- **S34 Combat tab:** Tarjetas compactas con swatch color (ColorHex), 2 lineas stats, chips tiers, comentario contextual
- **Newest first:** Historial ordenado por fecha descendente (combates recientes primero)
- **Boton replay:** ▶ en footer, valida CanReplay antes de habilitar, llama Request si clickeado
- **Backward compat:** Records viejos (sin ColorHex/tiers) muestran gracefully con fallbacks
- **Registry validación:** Replay button revalidado en cada render para captar cambios de registry (rival vendido/muerto, etc.)
