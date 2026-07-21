---
tags: [script, world, facade]
---

# MoriMonchiController.cs

**Ruta:** `World/Creatures/MoriMonchiController.cs`

**Responsabilidad:** Fachada que cablea [[MoriMochiAgent]] (brain) + [[MonchiVisualizer]] (assembly 3D modelo Suriyun S57) sin que ambos se conozcan. Ambos componentes en el mismo GameObject root, refs serializadas set una sola vez en el prefab (sin GetComponent en runtime). **S57:** `Initialize(dna, profileTable, player, bank, furDb)` inicializa el agente con **RoleWorldProfileSO**, pasa furDb al visualizer vía `SetFurDatabase()`. Si `bank == null`, hace RefreshLook (refresco liviano, sin Assemble) y retorna — contrato del REUSO de prewarmed en Acquire: el modelo ya fue ensamblado en el prewarm con bank real y el visualizer conserva su banco. Si `bank != null`, hace SetBank+Assemble (prewarm inicial y cold spawn). Método `Rebind(dna, profileTable, furDb)`: delega `agent.Rebind()` + `visualizer.SetFurDatabase()` + `visualizer.RefreshLook()` (refresco liviano sin re-ensamblar, para reloads rápidos). `Launch()` y `PrepareForPool()` son passthrough al agente. **S57d:** Propiedad pública `Visualizer` expone MonchiVisualizer para lecturas (consumida por [[MonchiLivePortrait]] para acceder a ModelRoot sin GetComponentInChildren).

**S57 ACTUALIZADO:** 
- `Initialize()` ahora acepta `MonchiVisualBankSO bank` (en lugar de legacy PartVisualBankSO)
- Si bank null: RefreshLook sin Assemble, retorna
- Si bank present: SetBank + Assemble (instancia body FBX)
- Reuso de prewarmed (Acquire): pasa bank=null = "no re-ensambles" — el visualizer CONSERVA el banco que guardó cuando el prewarm ensambló con bank real (bug S57 cazado en Play: SetBank(null) incondicional pisaba ese banco y mataba moods/shiny)
- Prewarm inicial y cold spawn: pasan bank real, Initialize hace SetBank+Assemble completo

**S57d ACTUALIZADO:**
- `public MonchiVisualizer Visualizer => visualizer;` — expone visualizer para acceso a ModelRoot desde MonchiLivePortrait

**Vinculado a:** [[Index/10 - Visualization]]

**Conexiones:** [[MoriMochiAgent]], [[MonchiVisualizer]], [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]], [[MoriMochiSpawner]], [[RoleWorldProfileSO]], [[MonchiLivePortrait]]

## Método Initialize (S57)

```csharp
public void Initialize(
    CreatureDNA         dna,
    RoleWorldProfileSO  profileTable,
    Transform           player,
    MonchiVisualBankSO  bank,        // null en prewarm, no null en cold spawn
    FurTypeDatabaseSO   furDb)
{
    agent.Initialize(dna, profileTable, player);
    visualizer.SetFurDatabase(furDb);
    
    if (bank == null)
    {
        visualizer.RefreshLook(dna);  // lightweight refresco, sin Assemble
        return;
    }
    
    visualizer.SetBank(bank);
    visualizer.Assemble(dna);         // instancia body FBX
}
```

**Responsabilidades:**
1. Wiring del agente (comportamiento)
2. Setup fur database del visualizer
3. Setup visual bank (si present)
4. Ensamblaje 3D (si hay banco; si no, refresco liviano)

**Prewarm inicial y cold spawn (bank real):**
- `Initialize(dna, profileTable, player, bank=monchiBank, furDb)` → SetBank + Assemble (el prewarm SIEMPRE ensambla, mientras el GO está inactivo)

**Reuso de prewarmed (Acquire, bank=null intencional):**
- `Initialize(dna, profileTable, player, bank=null, furDb)` → RefreshLook + retorna — "no re-ensambles, el modelo ya está armado"; el visualizer CONSERVA el banco guardado en el prewarm

## Método Rebind (S57)

```csharp
public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
{
    agent.Rebind(dna, profileTable);
    visualizer.SetFurDatabase(furDb);
    visualizer.RefreshLook(dna);  // liviana: retinta, no re-instancia
}
```

**Responsabilidades:**
1. Re-vinculación del agente (nuevas DNA + profile)
2. Setup fur database
3. Refresco visual (retintado, cambio de mood si aplica)
4. **NO re-ensambla** (caro): solo aplica colores + materiales existentes

## Campos Serializados

| Campo | Tipo | Atributos | Descripción |
|-------|------|-----------|-------------|
| `agent` | `MoriMochiAgent` | [Required] | Ref al brain IA |
| `visualizer` | `MonchiVisualizer` | [Required] | Ref al assembly 3D (S57: MonchiVisualizer, antes MoriMonchiVisualizer) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `DNA` | `CreatureDNA` | Getter → `agent.DNA` |
| `Agent` | `MoriMochiAgent` | Getter → agent ref (acceso directo para spawn pipeline) |
| `Visualizer` | `MonchiVisualizer` | Getter → visualizer ref (S57d: consumida por MonchiLivePortrait para ModelRoot access) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Initialize(dna, profileTable, player, bank, furDb)` | `void` | Wiring inicial: agent + visualizer. Si bank=null, refresco liviano. Si bank!=null, Assemble completo. |
| `Rebind(dna, profileTable, furDb)` | `void` | Re-vinculación rápida (reloads): agent + refresh visual sin re-ensamblar |
| `Launch(launchPos, launchVelocity)` | `void` | Passthrough → `agent.Launch()` (cannon spawn) |
| `PrepareForPool()` | `void` | Passthrough → `agent.PrepareForPool()` (pre-pool cleanup) |

## Cambios principales

**S39 (Role-based profiles):**
- Antes: `Initialize/Rebind(dna, PersonalityProfileSO profileTable, ...)`
- Ahora: `Initialize/Rebind(dna, RoleWorldProfileSO profileTable, ...)`
- Impacto: Llamadores (MoriMochiSpawner) pasan RoleWorldProfileSO en lugar de PersonalityProfileSO

**S57 (Suriyun model + MonchiVisualizer):**
- Antes: `Initialize(dna, profileTable, player, PartVisualBankSO bank, furDb)` → siempre Assemble
- Ahora: `Initialize(dna, profileTable, player, MonchiVisualBankSO bank, furDb)` → Assemble solo si bank!=null
- Antes: `visualizer: MoriMonchiVisualizer` (legacy, para combat replay)
- Ahora: `visualizer: MonchiVisualizer` (Suriyun model, mundo 3D)
- Contrato bank=null (solo el reuso de prewarmed en Acquire lo usa): RefreshLook sin Assemble, conservando el banco guardado — el prewarm inicial y el cold spawn pasan bank real y ensamblan completo
- Impacto: Optimización pooling; banco centralizado MonchiVisualBankSO

**S57d (Visualizer property access):**
- Antes: MonchiLivePortrait hacía GetComponentInChildren para acceder ModelRoot
- Ahora: `public MonchiVisualizer Visualizer => visualizer;` expone acceso directo
- Impacto: MonchiLivePortrait accede a Visualizer.ModelRoot sin búsqueda de componentes

## Patrón de Arquitectura

**Decoupling vía facade:**
- MoriMochiAgent ignora visualizer (comunica vía GameEvents + properties públicas)
- MonchiVisualizer ignora agent (lee DNA + profile pasadas en Initialize/Rebind)
- MoriMonchiController es el único punto de coordinación (ctor no hay, es facade sin lógica)

**Pooling-friendly (S57):**
- Initialize con bank real: setup completo (agent + Assemble) — prewarm inicial y cold spawn
- Initialize con bank=null: reuso de prewarmed — agent completo + RefreshLook sin re-ensamblar (el modelo ya existe)
- Rebind: refresco mínimo (DNA + colors, no ensamblaje)
- PrepareForPool: clean agent state (stations, pen, etc.)

**Access pattern (S57d):**
- Visualizer property permite a otros sistemas acceder a MonchiVisualizer sin romper encapsulación
- Caso de uso: MonchiLivePortrait accede ModelRoot vía `controller.Visualizer.ModelRoot`

## Vinculado a

- [[Index/06 - Player & World]], [[Index/10 - Visualization]]

## Conexiones

**Componentes en este GameObject:**
- [[MoriMochiAgent]] — behavior brain
- [[MonchiVisualizer]] — assembly visual (S57)

**Datos & servicios:**
- [[CreatureDNA]] — DNA viva
- [[RoleWorldProfileSO]] — perfil comportamiento
- [[MonchiVisualBankSO]] — banco visual Suriyun (S57, opcional en Initialize)
- [[FurTypeDatabaseSO]] — tipos pelaje
- [[MonchiMoodSetSO]] — emociones visuales (S57)

**Ciclo de vida:**
- [[MoriMochiSpawner]] → Prewarm: Instantiate + Initialize(bank=MonchiVisualBank) ensambla inactivo → luego Acquire reusa con Initialize(bank=null) que solo refresca
- [[MoriMochiSpawner]] → Cold spawn: Acquire + Initialize(bank=MonchiVisualBank) completo
- [[GameManager]] → OnRegistryReloaded → `Rebind()` en spawned controllers

**Acceso desde otros sistemas (S57d):**
- [[MonchiLivePortrait]] → `controller.Visualizer.ModelRoot` para aislamiento por layer

## Notas

- **Fachada pura:** Sin lógica de juego; solo wiring y passthrough.
- **S39 cambio:** ProfileTable → RoleWorldProfileSO (separación Role comportamiento vs Role combate).
- **S57 cambio:** Visualizer → MonchiVisualizer (Suriyun model); bank → MonchiVisualBankSO; Initialize dual-path (prewarm null, cold full).
- **S57d cambio:** Propiedad Visualizer pública para acceso a ModelRoot sin GetComponent.
- **Rebind optimization:** Evita re-instanciar partes (caro); solo re-aplica colores + materiales existentes vía RefreshLook.
