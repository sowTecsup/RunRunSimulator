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
- **Backend futuro**: Unity Gaming Services (UGS) — diseñar el DNA como string ligero desde ahora para facilitar serialización de red
- **Arte**: 3D, partes como FBX, ensamblaje estático con anchor points por BodyType

## Arquitectura actual (Etapa 1.1 ✅)
```
Scripts/
├── Enums.cs                          # Rarity, BodyType, TeethType
├── GameManager.cs                    # Lab de pruebas con Odin
├── CreatureGenerator.cs              # static class: GenerateRandom(db, Rarity?)
├── Data/
│   ├── CreatureDNA.cs               # string format: BODYSHAPE-ARM-EYE-MOUTH-RRGGBB
│   ├── CreatureDatabaseSO.cs        # Orquestador: refs a las 4 sub-DBs + validación
│   ├── Parts/
│   │   ├── BodyPart.cs              # abstract SerializedScriptableObject base
│   │   ├── ArmPart.cs
│   │   ├── EyePart.cs
│   │   ├── MouthPart.cs
│   │   └── BodyShapePart.cs
│   └── Databases/
│       ├── PartDatabaseSO.cs        # abstract generic PartDatabaseSO<T>
│       ├── ArmDatabaseSO.cs
│       ├── EyeDatabaseSO.cs
│       ├── MouthDatabaseSO.cs
│       └── BodyShapeDatabaseSO.cs
```

## Roadmap
| Etapa | Sub-etapa | Estado |
|-------|-----------|--------|
| 1 | 1.1 Arquitectura genética + DNA string | ✅ Completo |
| 1 | 1.2 Visualizador de criaturas (leer DNA → ensamblar Prefab 3D) | 🔲 Siguiente |
| 1 | 1.3 Sistema de Breeding (cruce de DNA, herencia, mutación) | 🔲 Pendiente |
| 2 | 2.1 Sistema de Estadísticas (HP, Fuerza, Velocidad desde partes) | 🔲 Pendiente |
| 2 | 2.2 Simulador de Batalla local → Battle Log | 🔲 Pendiente |
| 2 | 2.3 Integración Unity Services (async battles) | 🔲 Pendiente |
| 3 | 3.1 Tienda Local (NPCs, inventario, vitrinas) | 🔲 Pendiente |
| 3 | 3.2 Mercado Online (P2P via Unity Services) | 🔲 Pendiente |

## Reglas de código
1. **Desacoplamiento estricto**: cada sistema (genética, batalla, tienda) es independiente. Comunicación via interfaces o eventos, no referencias directas cruzadas.
2. **No comentar el QUÉ**: solo comentar el POR QUÉ cuando hay un invariante no obvio.
3. **Sin features adelantadas**: no implementar UGS, networking ni persistencia hasta Etapa 2.3.
4. **DNA como string ligero**: `CreatureDNA.ToStringID()` / `FromID()` son los puntos de entrada/salida para red. No romper este contrato.
5. **IDs de partes**: nunca pueden contener el carácter `-` (es el separador del DNA string).
6. **Odin siempre**: cualquier ScriptableObject con Diccionarios hereda de `SerializedScriptableObject`. Usar `[OdinSerialize]` explícitamente en campos de tipo no-serializable por Unity.

## Sistema de Herencia (Etapa 1.3 — reglas definitivas)
- Dado independiente por cada parte (no herencia de "paquete" completo de un padre)
- 50% herencia padres / 30% herencia abuelos — **configurables en un BreedingConfigSO**
- 20% mutación: parte completamente nueva del pool de la DB — **inamovible, no configurar**
- 20% de chance de heredar una parte en Nivel 1 de evolución (hijo normalmente nace en Nivel 0)
- Gender: padre con alto índice de batallas → más probabilidad de cría **Hembra**; bajo índice → **Macho**
- Máximo 4 crías de por vida / máximo 5 combates de por vida

## Part Sets (PartSet enum — Enums.cs)
Cada `BodyPart` tiene un campo `Set` que lo agrupa en un tema. Colores dinámicos en inspector con `[GUIColor]`.
Nombres actuales: `GooGang`, `BogBrigade`, `FuzzFactory`, `CosmicCreeps`, `NeonNightmares`, `CrunchCrew`, `GrimGlobs`, `SpudSquad`, `MoldMob`, `ZapZone`.
`PartDatabaseSO<T>.GetRandomPart()` acepta `PartSet?` como filtro opcional. `GetBySet(set)` devuelve lista filtrada.

## Gender (CreatureGender enum — Enums.cs)
`CreatureGender.Unknown` (generadas/silvestres), `Male`, `Female`.
**NO forma parte del DNA string** — es metadata de instancia, determinada en el momento del breeding.

## Anchor Points Estándar (Visualizador — Etapa 1.2)
- Estándar fijo: **2 arm anchors + 2 eye anchors + 1 mouth anchor** (formato 2-2-1)
- Partes = hijos del prefab con Transform propio (sin merge de mesh) — intercambiables en runtime
- Se requiere **preview en editor** (editor-time assembly al seleccionar un DNA)

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
```
