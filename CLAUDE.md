# RunRunSimulator — MoriMonchis — CLAUDE.md

## 📚 Fuente de Verdad — Leer SIEMPRE primero

> **Antes de cualquier sesión de trabajo, consultar el Notion Wiki.** Contiene el GDD completo, todas las decisiones de diseño tomadas, el roadmap actualizado y las preguntas de diseño abiertas. Este CLAUDE.md es un resumen de referencia rápida; el Notion es el documento vivo y autoritativo.

| Recurso | URL |
|---------|-----|
| 🟣 **Notion Wiki (GDD + Decisiones)** | https://www.notion.so/36cac10136a781819b74e176ed7c00d9 |

**Secciones clave del Notion a revisar según la tarea:**
- Trabajando en genética/DNA → sección *Sistema Genético* + *Breeding — Detalle Completo*
- Trabajando en batalla → sección *Sistema de Batalla — Detalle* + *Sistema de Bidding*
- Trabajando en tienda/economía → sección *Tienda y Economía* + *Decoraciones*
- Dudas de lore → sección *Diseño de Juego (GDD)* + *MoriMonchis Honorarios*
- Decisiones recientes → sección *Decisiones de Diseño — Etapa 1 (Ronda 4)* y rondas anteriores
- Preguntas aún abiertas → secciones *Preguntas de Diseño Abiertas (Ronda N)*

---

## Qué es este proyecto
Simulador de tienda retro 3D ambientado en los años 80s. El jugador es el dueño de una tienda que se sumó a la tendencia "MoriMonchis": criaturas biológicas del tamaño de la palma de la mano (estética Gremlins + Furby + Tamagotchi). En la trastienda opera un club de peleas clandestino asíncrono. Referencia de género: los *Simulator games* del mercado actual (PowerWash Simulator, etc.) pero con genética y muerte permanente.

## Nombre oficial de las criaturas
- **Singular**: MoriMochi
- **Plural**: MoriMonchis
- En código interno usar `Creature` / `CreatureDNA` para generalidad. En UI, logs visibles al jugador y naming de assets usar MoriMochi/MoriMonchis.

## Stack Técnico
- **Motor**: Unity 3D (C#)
- **Inspector**: Odin Inspector — SIEMPRE usar `SerializedScriptableObject`, `[OdinSerialize]` para Diccionarios, y atributos de Odin para UI de editor (`[Title]`, `[BoxGroup]`, `[Button]`, `[TableList]`, `[Searchable]`, etc.)
- **Serialización**: Newtonsoft.Json — package `com.unity.nuget.newtonsoft-json` requerido para persistencia de criaturas
- **Backend futuro**: Unity Gaming Services (UGS) — el DNA string ligero ya está diseñado para serialización de red
- **Arte**: 3D, partes como FBX, ensamblaje con anchor points

---

## Arquitectura actual

```
Assets/RunRunSimulator/Scripts/
├── Enums.cs                          # Rarity, PartSet, CreatureGender, PartRole, Tier
├── Interfaces.cs
├── GameManager.cs                    # Lab: Generate / Mint / Breed + inspector Odin
├── CreatureGenerator.cs              # static: GenerateRandom(db, oddsTable?)
├── BreedingService.cs                # static: Breed() — traversal árbol genealógico
├── SaveSystem.cs                     # static: SaveDatabase / LoadDatabase (Newtonsoft.Json)
├── Data/
│   ├── CreatureDNA.cs                # Genética + Identidad (UniqueID, Stamp) + Linaje
│   ├── CreatureDatabase.cs           # Plain C# registry: Dictionary<string, CreatureDNA>
│   ├── CreatureDatabaseSO.cs         # SO orquestador: refs sub-DBs + validación de IDs
│   ├── CreaturePartData.cs
│   ├── PartNameBank.cs               # static: pools de nombres por (PartSet, PartRole)
│   ├── RarityOddsTableSO.cs          # SO: pesos por Rarity → Roll() independiente por slot
│   ├── InheritanceOddsTableSO.cs     # SO singleton: odds breeding + JSON hot-reload
│   ├── Parts/
│   │   ├── BodyPart.cs               # abstract SO: ID[ReadOnly], Name, Rarity, Tier, Set
│   │   ├── ArmPart.cs                # GetPartRole() = Arm
│   │   ├── EyePart.cs                # GetPartRole() = Eye
│   │   ├── MouthPart.cs              # GetPartRole() = Mouth
│   │   └── BodyShapePart.cs          # GetPartRole() = Body
│   └── Databases/
│       ├── PartDatabaseSO.cs         # abstract generic: IDPrefix + Sync All IDs + Roll All Names
│       ├── ArmDatabaseSO.cs          # IDPrefix = "A"  → A0, A1, A2…
│       ├── EyeDatabaseSO.cs          # IDPrefix = "E"  → E0, E1, E2…
│       ├── MouthDatabaseSO.cs        # IDPrefix = "M"  → M0, M1, M2…
│       └── BodyShapeDatabaseSO.cs    # IDPrefix = "BS" → BS0, BS1, BS2…
```

---

## Roadmap

| Etapa | Sub-etapa | Estado |
|-------|-----------|--------|
| 1 | 1.1 Arquitectura genética + DNA string + Databases de partes | ✅ Completo |
| 1 | 1.2 Visualizador de criaturas (leer DNA → ensamblar Prefab 3D) | 🔲 Siguiente |
| 1 | 1.3 Sistema de Breeding (herencia, linaje, registro, persistencia) | 🔶 En progreso |
| 2 | 2.1 Sistema de Estadísticas (HP, Fuerza, Velocidad desde partes) | 🔲 Pendiente |
| 2 | 2.2 Simulador de Batalla local → Battle Log | 🔲 Pendiente |
| 2 | 2.3 Integración Unity Services (async battles) | 🔲 Pendiente |
| 3 | 3.1 Tienda Local (NPCs, inventario, vitrinas) | 🔲 Pendiente |
| 3 | 3.2 Mercado Online (P2P via Unity Services) | 🔲 Pendiente |

**Etapa 1.3 — estado detallado:**

| Feature | Estado |
|---------|--------|
| `BreedingService.Breed()` con traversal genealógico | ✅ |
| `InheritanceOddsTableSO` con hot-reload JSON | ✅ |
| `CreatureDatabase` registro O(1) | ✅ |
| `SaveSystem` persistencia JSON completa | ✅ |
| `GameManager.MintRandomCreature()` y `BreedCreatures()` | ✅ |
| Validación límite máximo de crías (4) y combates (5) | 🔲 Pendiente |
| Género por battle-index del padre (actualmente 50/50) | 🔲 Pendiente (Etapa 2) |
| Bonus de rareza en la 4ª cría (última posible) | 🔲 Pendiente |
| Herencia del nivel Tier de las partes | 🔲 Pendiente |

---

## Reglas de código

1. **Desacoplamiento estricto**: cada sistema (genética, batalla, tienda) es independiente. Comunicación via interfaces o eventos, no referencias directas cruzadas.
2. **No comentar el QUÉ**: solo comentar el POR QUÉ cuando hay un invariante no obvio.
3. **Sin features adelantadas**: no implementar UGS ni mecánicas de batalla hasta Etapa 2. La persistencia local JSON es válida desde Etapa 1.3.
4. **DNA como string ligero**: `CreatureDNA.ToStringID()` / `FromID()` son el contrato de red — no romperlo. El timestamp es metadata de registro, no forma parte del genetic string.
5. **IDs de partes**: nunca pueden contener el carácter `-` (es el separador del DNA string).
6. **Odin siempre**: cualquier ScriptableObject con Diccionarios hereda de `SerializedScriptableObject`. Usar `[OdinSerialize]` explícitamente.
7. **Sin complejidad innecesaria**: no añadir campos, abstracciones ni features que no hayan sido pedidos explícitamente. Tres líneas similares son mejor que una abstracción prematura.

---

## Sistema de IDs de Partes

- Los IDs son **auto-generados** por la database. **No se editan manualmente** — `BodyPart.ID` es `[ReadOnly]`.
- Formato por tipo: `BS0`, `BS1`… / `A0`, `A1`… / `E0`, `E1`… / `M0`, `M1`…
- El botón **Sync All IDs** en cada database renumera TODO desde 0 y escribe el valor de vuelta en `part.ID`. Usar en setup inicial, nunca con DNA strings ya distribuidos en red.

---

## Sistema de Nombres de Partes (PartNameBank)

- `PartNameBank.cs` — clase estática, pools de 5 palabras por cada `(PartSet, PartRole)`.
- Botón **Roll Name** en cada `BodyPart` SO: genera nombre individual.
- Botón **Roll All Names** en cada database SO: genera nombres en bulk para todas las partes.
- Nombre de criatura = `"{body} {arm} {eye} {mouth}"` → `CreatureDNA.GetDisplayName(db)`.
- Las palabras están temáticamente ligadas al `PartSet` (GooGang = pegajoso, ZapZone = eléctrico, etc.).

---

## Identidad de Criaturas (CreatureDNA)

```
ToStringID() = "BS0-A3-E1-M2-FF00AA"              // genetic string — contrato de red
UniqueID     = "BS0-A3-E1-M2-FF00AA-{Ticks}"      // clave en el registro
BirthDate    = DateTime (UTC)
Stamp()      → setea Timestamp + BirthDate de forma atómica antes de registrar
```

- `MotherID`, `FatherID`, `ChildrenIDs` — referencias por `UniqueID` (no genetic strings)
- `Gender` — `Unknown` hasta mintearse. Se asigna 50/50 en `Mint` y en `Breed`.

---

## Sistema de Breeding (InheritanceOddsTableSO)

Probabilidades por defecto — configurables, normalizadas internamente:

| Origen | Peso por defecto |
|--------|-----------------|
| Padres directos | 40 |
| Abuelos | 20 |
| Bisabuelos | 10 |
| Mutación aleatoria | 20 |
| Base / entorno | 10 |

- Cada slot (body, arm, eye, mouth) hace su **propio roll independiente**.
- Mutación y Base → parte aleatoria del pool completo (sin filtro de rarity ni set).
- Si un ancestro no existe en el registro, cae automáticamente al fallback random.
- Hot-reload: botones **Save/Load JSON** en el SO ↔ `persistentDataPath/inheritance_odds.json`.
- Singleton: `InheritanceOddsTableSO.Current` (se setea en `OnEnable` del SO).

---

## Sistema de Rareza (RarityOddsTableSO)

- Pesos relativos configurables por `Rarity`. Por defecto: Common 60 / Uncommon 25 / Rare 10 / Epic 4 / Legendary 1.
- `CreatureGenerator.GenerateRandom(db, oddsTable)` → cada uno de los 4 slots hace su propio `oddsTable.Roll()`.
- Si no se asigna tabla, el generador elige sin filtro de rareza.

---

## Part Sets (PartSet enum)

Cada `BodyPart` tiene `Set` que agrupa partes en un tema visual/lore. 10 sets: `GooGang`, `BogBrigade`, `FuzzFactory`, `CosmicCreeps`, `NeonNightmares`, `CrunchCrew`, `GrimGlobs`, `SpudSquad`, `MoldMob`, `ZapZone`. Colores dinámicos en inspector con `[GUIColor]`.

---

## Tier (enum)

`Tier1 = 1`, `Tier2 = 2`, `Tier3 = 3`. Campo en `BodyPart`. Las partes nacen en Tier1. La evolución de Tier durante combate es Etapa 2.

---

## Gender (CreatureGender enum)

`Unknown` (sin registrar), `Male`, `Female`. **NO forma parte del DNA string.** GDD target: género basado en battle-index del padre — pendiente Etapa 2.

---

## Persistencia (SaveSystem)

| Archivo | Contenido | Formato |
|---------|-----------|---------|
| `creature_database.json` | Registro completo de criaturas + árbol genealógico | Newtonsoft.Json |
| `inheritance_odds.json` | Pesos de herencia para hot-reload | Newtonsoft.Json |

- `SaveDatabase()` se llama automáticamente en `Mint`, `Breed` y `OnApplicationQuit`.
- `LoadDatabase()` se llama en `Awake` del GameManager.
- `UnityEngine.Color` → hex string via custom `UnityColorConverter`.
- **Dependencia**: package `com.unity.nuget.newtonsoft-json` en Package Manager.

---

## Anchor Points Estándar (Visualizador — Etapa 1.2)

- Estándar fijo: **2 arm anchors + 2 eye anchors + 1 mouth anchor** (formato 2-2-1)
- Partes = hijos del prefab con Transform propio (sin merge de mesh) — intercambiables en runtime
- Se requiere **preview en editor** (editor-time assembly al seleccionar un DNA)

---

## Convenciones de CreateAssetMenu

```
RunRunSimulator/Parts/Arm
RunRunSimulator/Parts/Eye
RunRunSimulator/Parts/Mouth
RunRunSimulator/Parts/Body Shape
RunRunSimulator/Databases/Arm Database
RunRunSimulator/Databases/Eye Database
RunRunSimulator/Databases/Mouth Database
RunRunSimulator/Databases/Body Shape Database
RunRunSimulator/Creature Database (Orchestrator)
RunRunSimulator/Rarity Odds Table
RunRunSimulator/Inheritance Odds Table
```
