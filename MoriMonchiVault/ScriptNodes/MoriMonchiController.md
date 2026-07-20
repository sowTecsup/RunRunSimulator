---
tags: [script, world, facade]
---

# MoriMonchiController

**Ruta:** `World/Creatures/MoriMonchiController.cs`

**Responsabilidad:** Facade que cablea `MoriMochiAgent` (brain) + `MoriMonchiVisualizer` (3D assembly) sin que ambos se conozcan. Ambos componentes en el mismo GameObject root. `Initialize(dna, profileTable, player, bank, furDb)` inicializa el agente con **`RoleWorldProfileSO`** (S39 cambio: antes `PersonalityProfileSO`), pasa furDb al visualizer vía `SetFurDatabase()`, ensambla visual via `Assemble(dna, bank)`. Nuevo passthrough `Rebind(dna, profileTable, furDb)`: delega `agent.Rebind()` + aplica `visualizer.RefreshFur()` (refresco liviano sin re-ensamblar). `Launch()` y `PrepareForPool()` passthrough al agente. Propiedad pública `Agent` expone MoriMochiAgent. **NUEVO (S52):** Soporte opcional `spiderVisual` (SpiderPaletteApplier): si != null en Initialize/Rebind, aplica color genético vía `ApplyFromDna()` + material de `FurTypeDatabaseSO`, y **salta el `Assemble()`** (contrato alternativo para prototipos/modelos experimentales).

## Método Initialize

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

    // S52: soporte opcional prototipo/alternativo
    if (spiderVisual != null)
    {
        if (furDb != null) spiderVisual.ApplyMaterial(furDb.GetMaterial(dna.FurType));
        spiderVisual.ApplyFromDna(dna);
        return;  // salta Assemble, contrato visual alternativo
    }

    if (bank == null) return;
    visualizer.Assemble(dna, bank);  // contrato estándar
}
```

## Método Rebind

```csharp
public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
{
    agent.Rebind(dna, profileTable);  // pasa RoleWorldProfileSO
    visualizer.SetFurDatabase(furDb);

    // S52: soporte opcional prototipo/alternativo
    if (spiderVisual != null)
    {
        if (furDb != null) spiderVisual.ApplyMaterial(furDb.GetMaterial(dna.FurType));
        spiderVisual.ApplyFromDna(dna);
        return;  // salta RefreshFur, contrato visual alternativo
    }

    visualizer.RefreshFur(dna);  // contrato estándar
}
```

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `agent` | `MoriMochiAgent` | [Required] Ref a brain |
| `visualizer` | `MoriMonchiVisualizer` | [Required] Ref a assembly visual |
| `spiderVisual` | `Prototype.SpiderPaletteApplier` | [S52 NEW] Ref opcional a aplicador color alternativo (prototipo/experimental) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `DNA` | `CreatureDNA` | Getter → `agent.DNA` |
| `Agent` | `MoriMochiAgent` | Getter → agent ref (acceso directo) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Initialize(dna, profileTable, player, bank, furDb)` | `void` | Wiring inicial: agent + visualizer (o spiderVisual si está presente) |
| `Rebind(dna, profileTable, furDb)` | `void` | **S39** Re-vinculación rápida sin re-ensamblar (reloads); **S52** también soporta spiderVisual alternativo |
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

## Cambios S52

**Initialize/Rebind lógica condicional:**
- Si `spiderVisual != null` (proto alternativo): aplica color genético directo vía `SpiderPaletteApplier`, salta `Assemble()` / `RefreshFur()` estándar
- Si `spiderVisual == null` (comportamiento estándar intacto): usa visualizer + bank como siempre
- Rollback actual: `spiderVisual` ref está null en todos los MoriMonchis (contrato estándar activo)

**Impacto:** Contrato base inmutable; soporte experimental para modelos alternativos sin quebrar flujo estándar. Permite prototipos de visualización sin tocar MoriMonchiVisualizer.

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMochiAgent]] — brain
- [[MoriMonchiVisualizer]] — assembly visual estándar
- [[SpiderPaletteApplier]] — aplicador color alternativo (S52)
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
- Visual 3D (visualizer o spiderVisual)
- Propiedades public DNA/Agent para otros sistemas

## Notas

- **Facade pattern:** Decoupling completo entre agent y visualizer. Cada uno ignora al otro.
- **S39 cambio:** ProfileTable ahora es RoleWorldProfileSO (datos comportamiento mundo), no PersonalityProfileSO.
- **S52 cambio:** Soporte para contrato visual alternativo vía `spiderVisual` (null → estándar intacto).
- **Rebind optimization:** No re-ensambla mesh (caro); solo re-inyecta DNA + perfil + RefreshFur (rápido).
- **Passthrough:** Launch/PrepareForPool solo forwarden al agent; visualizer no participa.
