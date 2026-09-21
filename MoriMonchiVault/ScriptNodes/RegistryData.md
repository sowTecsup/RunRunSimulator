---
tags: [data, genetics, serializable]
---

# RegistryData

**Ruta:** `Data/Genetics/RegistryData.cs`

**Responsabilidad:** Contenedor puro para serializar el registro completo en guardado V3. Divide criaturas en dos diccionarios: vivas (`Alive`) y idas (`Departed`). Embudo entre JSON y `CreatureRegistrySO`. Sin métodos, sin lógica.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Alive` | `Dictionary<string, CreatureDNA>` | Criaturas activas del jugador |
| `Departed` | `Dictionary<string, CreatureDNA>` | Idas (muertas/vendidas), hasta `MaxDeparted = 300` |

## Ciclo de Vida

**Guardado:**
1. `GameManager.Persist()` → `CreatureRegistrySO.GetData()` → `RegistryData` con Alive + Departed
2. `SaveSystem.SaveDatabase()` → serializa `RegistryData` a JSON (`"registry"` key)

**Carga:**
1. `SaveSystem.LoadInto(registry)` deseriatliza JSON → `RegistryData`
2. `CreatureRegistrySO.LoadFrom(data)` recibe `RegistryData`, puebla ambos diccionarios, reconcilia

## Vinculado a

[[Index/07 - Persistence & Identity]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CreatureRegistrySO]], [[SaveSystem]], [[GameManager]]
