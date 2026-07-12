---
tags: [script, world, facade]
---

# MoriMonchiController

**Ruta:** `World/Creatures/MoriMonchiController.cs`

**Responsabilidad:** Facade que cablea `MoriMochiAgent` (brain) + `MoriMonchiVisualizer` (3D assembly) sin que ambos se conozcan. Ambos componentes en el mismo GameObject root. `Initialize(dna, profileTable, player, bank, furDb)` inicializa el agente con **`RoleWorldProfileSO`** (S39 cambio: antes `PersonalityProfileSO`), pasa furDb al visualizer vía `SetFurDatabase()`, ensambla visual via `Assemble(dna, bank)`. **Nuevo passthrough `Rebind(dna, profileTable, furDb)`**: delega `agent.Rebind()` + aplica `visualizer.RefreshFur()` (refresco liviano sin re-ensamblar). `Launch()` y `PrepareForPool()` passthrough al agente. Propiedad pública `Agent` expone MoriMochiAgent.

## Método Initialize (S39 cambio)

```csharp
public void Initialize(
    CreatureDNA        dna,
    RoleWorldProfileSO profileTable,  // S39: was PersonalityProfileSO
    Transform          player,
    PartVisualBankSO   bank,
    FurTypeDatabaseSO  furDb)
{
    agent.Initialize(dna, profileTable, player);  // pasa RoleWorldProfileSO

    visualizer.SetFurDatabase(furDb);

    if (bank == null) return;
    visualizer.Assemble(dna, bank);
}
```

## Método Rebind (S39 cambio)

```csharp
public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
{
    agent.Rebind(dna, profileTable);  // pasa RoleWorldProfileSO
    visualizer.SetFurDatabase(furDb);
    visualizer.RefreshFur(dna);
}
```

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `agent` | `MoriMochiAgent` | [Required] Ref a brain |
| `visualizer` | `MoriMonchiVisualizer` | [Required] Ref a assembly visual |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `DNA` | `CreatureDNA` | Getter → `agent.DNA` |
| `Agent` | `MoriMochiAgent` | Getter → agent ref (acceso directo) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Initialize(dna, profileTable, player, bank, furDb)` | `void` | Wiring inicial: agent + visualizer |
| `Rebind(dna, profileTable, furDb)` | `void` | **S39** Re-vinculación rápida sin re-ensamblar (reloads) |
| `Launch(launchPos, launchVelocity)` | `void` | Passthrough → `agent.Launch()` |
| `PrepareForPool()` | `void` | Passthrough → `agent.PrepareForPool()` |

## Cambios S39

**Initialize firma:**
- Antes: `Initialize(dna, PersonalityProfileSO profileTable, ...)`
- Ahora: `Initialize(dna, RoleWorldProfileSO profileTable, ...)` — RoleWorldProfileSO reemplaza PersonalityProfileSO

**Rebind firma:**
- Antes: `Rebind(dna, PersonalityProfileSO profileTable, furDb)`
- Ahora: `Rebind(dna, RoleWorldProfileSO profileTable, furDb)` — RoleWorldProfileSO

**Impacto:** Llamadores (MoriMochiSpawner) pasan RoleWorldProfileSO en lugar de PersonalityProfileSO.

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMochiAgent]] — brain
- [[MoriMonchiVisualizer]] — assembly visual
- [[MoriMochiSpawner]] — creador via Initialize
- [[RoleWorldProfileSO]] — profile de rol (S39, reemplaza PersonalityProfileSO)
- [[PartVisualBankSO]] — partes visuales
- [[FurTypeDatabaseSO]] — tipos pelaje

## Conexiones

**Entrada:**
- `MoriMochiSpawner.Spawn()` → `controller.Initialize(dna, roleWorldProfiles, player, bank, furDb)`
- `MoriMochiSpawner.Rebind()` → `controller.Rebind(dna, roleWorldProfiles, furDb)` (OnRegistryReloaded)

**Salida:**
- NavMesh movement (agent)
- Visual 3D (visualizer)
- Propiedades public DNA/Agent para otros sistemas

## Notas

- **Facade pattern:** Decoupling completo entre agent y visualizer. Cada uno ignora al otro.
- **S39 cambio:** ProfileTable ahora es RoleWorldProfileSO (datos comportamiento mundo), no PersonalityProfileSO.
- **Rebind optimization:** No re-ensambla mesh (caro); solo re-inyecta DNA + perfil + RefreshFur (rápido).
- **Passthrough:** Launch/PrepareForPool solo forwarden al agent; visualizer no participa.
