---
tags: [script, world, facade]
---

# MoriMonchiController.cs

**Ruta:** `World/Creatures/MoriMonchiController.cs`

**Responsabilidad:** Fachada que cablea [[MoriMochiAgent]] (brain) + [[MoriMonchiVisualizer]] (assembly 3D) sin que ambos se conozcan. Ambos componentes en el mismo GameObject root, refs serializadas set una sola vez en el prefab (sin GetComponent en runtime). `Initialize(dna, profileTable, player, bank, furDb)` inicializa el agente con **RoleWorldProfileSO** (S39 cambio: antes PersonalityProfileSO), pasa furDb al visualizer vía `SetFurDatabase()`, ensambla 3D via `Assemble(dna, bank)` (si bank != null). Método `Rebind(dna, profileTable, furDb)`: delega `agent.Rebind()` + `visualizer.RefreshFur()` (refresco liviano sin re-ensamblar, para reloads rápidos). `Launch()` y `PrepareForPool()` son passthrough al agente. Propiedad pública `Agent` expone MoriMochiAgent para lecturas. **S55 cambio:** El experimento spider fue eliminado del proyecto; `spiderVisual` field y sus dos bloques condicionales se borraron.

## Método Initialize

```csharp
public void Initialize(
    CreatureDNA        dna,
    RoleWorldProfileSO profileTable,    // S39: was PersonalityProfileSO
    Transform          player,
    PartVisualBankSO   bank,
    FurTypeDatabaseSO  furDb)
{
    agent.Initialize(dna, profileTable, player);
    
    visualizer.SetFurDatabase(furDb);
    
    if (bank == null) return;
    visualizer.Assemble(dna, bank);
}
```

**Responsabilidades:**
1. Wiring del agente (comportamiento)
2. Setup fur database del visualizer
3. Ensamblaje 3D (si hay banco de partes)

## Método Rebind

```csharp
public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
{
    agent.Rebind(dna, profileTable);
    visualizer.SetFurDatabase(furDb);
    visualizer.RefreshFur(dna);
}
```

**Responsabilidades:**
1. Re-vinculación del agente (nuevas DNA + profile)
2. Refresco visual (colores genéticos)
3. **NO re-ensambla** (caro): solo aplica colores + materiales existentes

## Campos Serializados

| Campo | Tipo | Atributos | Descripción |
|-------|------|-----------|-------------|
| `agent` | `MoriMochiAgent` | [Required] | Ref al brain IA |
| `visualizer` | `MoriMonchiVisualizer` | [Required] | Ref al assembly 3D |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `DNA` | `CreatureDNA` | Getter → `agent.DNA` |
| `Agent` | `MoriMochiAgent` | Getter → agent ref (acceso directo para spawn pipeline) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Initialize(dna, profileTable, player, bank, furDb)` | `void` | Wiring inicial: agent + visualizer |
| `Rebind(dna, profileTable, furDb)` | `void` | Re-vinculación rápida (reloads): agent + refresh visual sin re-ensamblar |
| `Launch(launchPos, launchVelocity)` | `void` | Passthrough → `agent.Launch()` (cannon spawn) |
| `PrepareForPool()` | `void` | Passthrough → `agent.PrepareForPool()` (pre-pool cleanup) |

## Cambios principales

**S39 (Role-based profiles):**
- Antes: `Initialize/Rebind(dna, PersonalityProfileSO profileTable, ...)`
- Ahora: `Initialize/Rebind(dna, RoleWorldProfileSO profileTable, ...)`
- Impacto: Llamadores (MoriMochiSpawner) pasan RoleWorldProfileSO en lugar de PersonalityProfileSO

**S55 (Eliminación spider):**
- Antes: Dos bloques condicionales `if (spiderVisual != null)` en Initialize/Rebind soportaban contrato visual alternativo
- Ahora: Eliminados. Contrato visual única: MoriMonchiVisualizer + PartVisualBankSO
- Impacto: Código más simple, desaparece soporte experimental

## Patrón de Arquitectura

**Decoupling vía facade:**
- MoriMochiAgent ignora visualizer (comunica vía GameEvents + properties públicas)
- MoriMonchiVisualizer ignora agent (lee DNA + profile pasadas en Initialize/Rebind)
- MoriMonchiController es el único punto de coordinación (ctor no hay, es facade sin lógica)

**Pooling-friendly:**
- Initialize: full setup (model + agent)
- Rebind: refresco mínimo (DNA + colors, no ensamblaje)
- PrepareForPool: clean agent state (stations, pen, etc.)

## Vinculado a

- [[Index/06 - Player & World]]

## Conexiones

**Componentes en este GameObject:**
- [[MoriMochiAgent]] — behavior brain
- [[MoriMonchiVisualizer]] — assembly visual

**Datos & servicios:**
- [[CreatureDNA]] — DNA viva
- [[RoleWorldProfileSO]] — perfil comportamiento (S39)
- [[PartVisualBankSO]] — partes visuales (opcional, si bank != null en Initialize)
- [[FurTypeDatabaseSO]] — tipos pelaje

**Ciclo de vida:**
- [[MoriMochiSpawner]] → instancia via `Instantiate()`, llama `Initialize()`, luego `Launch()`, después `PrepareForPool()`
- [[GameManager]] → OnRegistryReloaded → `Rebind()` en spawned controllers

## Notas

- **Fachada pura:** Sin lógica de juego; solo wiring y passthrough.
- **S39 cambio:** ProfileTable → RoleWorldProfileSO (separación Role comportamiento vs Role combate).
- **S55 cambio:** Eliminación del campo spiderVisual y soporte experimental. Contrato único: visualizer + bank.
- **Rebind optimization:** Evita re-instanciar partes (caro); solo re-aplica colores + materiales existentes.
