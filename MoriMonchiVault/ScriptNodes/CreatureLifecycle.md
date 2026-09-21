---
tags: [utility, static, lifecycle, events]
---

# CreatureLifecycle

**Ruta:** `Systems/Creatures/CreatureLifecycle.cs`

**Responsabilidad:** **Único punto de escritura** para `IsDead` y `BusyReason.Sold`. Orquesta transición de criatura del estante vivo al estante de idas (`Departed`), notifica por eventos. Centraliza la lógica de ciclo de vida.

## Métodos Públicos

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `Kill` | `CreatureDNA dna` | Marca `IsDead=true`, la depart, dispara `OnCreatureDeparted` + `OnRegistryChanged` |
| `Adopt` | `CreatureDNA dna` | Marca `BusyState=Sold`, timestamp venta, depart, dispara eventos |

## Flujo

**Kill:**
1. Valida `dna != null && registry != null`
2. `dna.IsDead = true`
3. `registry.Depart(id)` → mueve a estante `Departed`
4. Dispara `GameEvents.CreatureDeparted(dna)` (UI escucha: `InfoOverlayUITK`, `SocialGraphService`)
5. Dispara `GameEvents.RegistryChanged(registry)` (persist)

**Adopt (venta):**
1. Valida `dna != null && registry != null`
2. `dna.BusyState = BusyReason.Sold`
3. `dna.SaleDate = DateTime.UtcNow`
4. `registry.Depart(id)` → mueve a estante `Departed`
5. Dispara `GameEvents.CreatureDeparted(dna)` (suscriptores reaccionan a venta)
6. Dispara `GameEvents.RegistryChanged(registry)` (persist)

## Invariantes

- **Único punto de escritura:** `Kill` y `Adopt` son los ÚNICOS lugares donde escriben `IsDead` y `BusyReason.Sold`
- **Sin direct registry access:** consulta `GameManager.Instance.Registry`; falla silenciosamente si manager es null (seguro en tests)
- **Siempre dispara eventos:** toda transición notifica a listeners (UI, social graph, persist)

## Caso de uso

- **Bajada:** `ArenaRunDirector` → `ExpeditionBridge` → `CreatureLifecycle.Kill()` si pierde
- **Venta:** `CashRegister` → `CreatureLifecycle.Adopt()` si cliente compra
- **Muerte natural:** future (HC+)

## Vinculado a

[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureDNA]], [[CreatureRegistrySO]], [[GameManager]], [[GameEvents]], [[ExpeditionBridge]], [[CashRegister]]
