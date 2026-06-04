---
tags: [memory-bank, persistence, identity, save, events]
---

# 07 — Persistence & Identity

> Relacionados: [[04 - UGS & Cloud]] (sync con cloud), [[02 - Genetics & Breeding]] (CreatureDNA fields), [[03 - Combat]] (Busy state persistence).

## Arquitectura orientada a eventos (GameEvents)

Bus estático central (`GameEvents.cs`, namespace global). Publicadores y suscriptores dependen **solo del bus**, nunca uno del otro.

**Razón clave**: un `event` de C# solo lo puede disparar la clase que lo declara → un bus neutral permite que *cualquiera* dispare y *cualquiera* escuche, y el suscriptor no necesita referenciar al publicador.

**Filosofía**: **los eventos transportan la data.** El payload lleva lo que el suscriptor necesita (el `registry`, el `CombatResult`, etc.) para que no tenga que volver a buscarlo en un singleton.

### Tabla de eventos

| Evento | Payload | Quién dispara | Quién escucha |
|--------|---------|---------------|---------------|
| `OnRegistryChanged` | `CreatureRegistrySO` | toda mutación de gameplay (mint, breed, combate, enqueue/dequeue, hatch) | `GameManager.Persist` → save+push · `CreatureGridView` → refresh |
| `OnRegistryReloaded` | `CreatureRegistrySO` | `CloudSyncService` tras pull/reset | `CreatureGridView` → refresh (**solo UI, sin push** — la data vino del cloud) |
| `OnCreatureMinted` | `CreatureDNA` | `GameManager.MintRandomCreature` | (hook libre: logging/UI futuro) |
| `OnCombatCompleted` | `CombatResult` | `CombatController` + `CombatPanelUITK` (combate local) | (hook libre: battle-log UI) |
| `OnCombatLogged` | `CombatLogEntry` | `AsyncCombatService.ApplyResult` (async) | `CombatPanelUITK` → cachea para la tab Resultados |
| `OnBreedingCompleted` | `mother, father, child` | `BreedingController` + `AsyncBreedingService.HatchLocally` | (hook libre) |

**Reglas técnicas:**

- **Helper estático por evento**: `RegistryChanged(so) => OnRegistryChanged?.Invoke(so)` — call site corto, sin `?.Invoke` repetido.
- **Un solo evento de mutación con payload** (no dos en paralelo): evita disparar dos veces por mutación.
- **Desuscribir en `OnDisable`** (regla 9 de código).
- **Cambio de comportamiento**: el combate local también pushea al cloud (antes solo guardaba local), al pasar por `OnRegistryChanged`. El push es no-op si no hay sesión.

### Gap async (RESUELTO)

El path async (`PollResultsAsync` aplica `CloudCombatResult`) no disparaba `OnCombatCompleted`, así que un battle-log se perdía los combates async. Hoy `AsyncCombatService.ApplyResult` dispara `OnCombatLogged(CombatLogEntry)` (POV de la criatura, con el log) antes de borrar la copia cloud → el `CombatPanelUITK` lo cachea para la tab Resultados.

## Identidad de Criaturas (resumen — detalle en [[02 - Genetics & Breeding]])

```
ToStringID() = "BS0-A3-E1-M2-FF00AA"              // genetic string — contrato de red (inmutable)
UniqueID     = "BS0-A3-E1-M2-FF00AA-{Ticks}"      // clave en el registro
BirthDate    = DateTime (UTC)
Stamp()      → setea Timestamp + BirthDate de forma atómica antes de registrar
```

- `MotherID`, `FatherID`, `ChildrenIDs` — referencias por `UniqueID` (no genetic strings).
- ⚠️ Los IDs de partes NUNCA pueden contener `-` (separador del DNA string).

## Persistencia local — SaveSystem

| Archivo | Contenido | Formato |
|---------|-----------|---------|
| `creature_database_<playerId>.json` | Registro completo de criaturas + árbol genealógico | Newtonsoft.Json |
| `sync_meta.json` | Timestamps de seguridad para detección de rollback/edición manual | JSON |

### Reglas críticas

- `SaveDatabase(registry)` **NO** se llama directo desde gameplay: las mutaciones disparan `GameEvents.RegistryChanged(registry)` y `GameManager.Persist` hace el save+push (ver tabla de eventos).
- El único save directo es el flush de `OnApplicationQuit` y los de `CloudSyncService` (capa de sync).
- `LoadInto(registry)` se llama en login (`CloudSyncService.OnSignedInComplete`) — popula el SO desde JSON antes del pull.
- `UnityEngine.Color` → hex string via custom `UnityColorConverter`.

### Scoping por playerId

- El archivo pasa de `creature_database.json` a `creature_database_<playerId>.json` después del sign-in.
- Si existe el unscoped pero no el scoped, hay **migración automática** la primera vez.
- Permite testing con múltiples cuentas/instancias sin que se pisen.

### Dependencia

Package `com.unity.nuget.newtonsoft-json` `3.2.1` en Package Manager (namespace `Newtonsoft.Json`, **NO** `Unity.Plastic.Newtonsoft.Json`).

## CreatureRegistrySO

- SO asset asignado en `GameManager → Setup`.
- JSON es la **única fuente de verdad**; el SO es vista visual `[ReadOnly]`.
- `Dictionary<string, CreatureDNA>` (`InfoBox` warning + botón `Sync`).

## CreatureDatabaseSO (orquestador)

- Refs sub-DBs (Arm, Eye, Mouth, BodyShape).
- Validación de IDs (no `-`).

## GameManager — único dueño de persistencia

- Escucha `OnRegistryChanged` → `Persist` (save + push fire-and-forget).
- Source of truth de los assets compartidos (getters Registry/Database/RarityOddsTable/InheritanceOddsTable/CombatConfig/PersonalityProfiles).
- Lab: Generate / Mint (asigna Personality random) + Fill Random Breeders / Fighters.

### Needs (stats runtime) — persistencia diferida, anti-saturación

`CreatureDNA.Needs` (`NeedsState`: Health/Energy/Affect) son stats que el `MoriMochiAgent` muta **cada frame en memoria** (decay, recarga en estaciones, gasto en breeding/combate). Para NO saturar Cloud Save con micro-updates por frame:

- Los cambios de needs **NUNCA disparan `OnRegistryChanged`** (eso pushea en cada mutación). Mutan el objeto `CreatureDNA` directo (el agente comparte la referencia del registro).
- Viajan en el flush normal: `GameManager.FlushToCloud()` (= `SaveDatabase` + `PushToCloud`), llamado en **`OnApplicationQuit`** y **`OnApplicationPause(true)`** (minimizar/background — señal confiable en mobile).
- `FlushToCloud()` es **público** para que lo llamen el logout y un "guardar partida" explícito. ⚠️ **Pendiente**: cablearlo en el logout de `CloudSyncService` (hoy no está enganchado).
- Como `NeedsState` vive dentro de `CreatureDNA`, se serializa con el registro **sin tocar `SaveSystem`/`CloudSync`**. Detalle del sistema en [[06 - Player & World]].

## Archivos clave

```
Assets/RunRunSimulator/Scripts/Core/
├── GameManager.cs                    # Único dueño de persistencia. Source of truth de SO assets
├── GameEvents.cs                     # Bus estático global. Eventos con payload
└── SaveSystem.cs                     # static: SaveDatabase / LoadInto / Serialize (scoped por playerId, migración automática)

Assets/RunRunSimulator/Scripts/Data/
├── CreatureDNA.cs                    # (ver [[02 - Genetics & Breeding]])
├── CreatureRegistrySO.cs             # SO registry
└── CreatureDatabaseSO.cs             # SO orquestador
```
