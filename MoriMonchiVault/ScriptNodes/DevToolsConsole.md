---
tags: [script, dev-tools, utility]
---

# DevToolsConsole

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Dev component para testeo. Buttons: Wallet, Furniture, Props, Genetics, Expedition, **Clock** (S131). Emite eventos para persistencia. Solo para desarrollo.

## BoxGroups / Buttons

### Dev Tools (Wallet + Furniture + Props)
| Button | Acción |
|--------|--------|
| `Add Dabloons (DEV)` | Suma `devDabloonsAmount` vía Wallet |
| `Reset Dabloons (DEV)` | Vuelve a 0 |
| `Clear Furniture Owned (DEV)` | Limpia lista |
| `Clear World Props (DEV)` | Limpia props |

### Genetics (DEV)
| Button | Acción |
|--------|--------|
| `Reroll Potentials (DEV)` | Cambia potenciales aleatorio |

### **Clock (DEV) — S131**
| Button | Acción |
|--------|--------|
| `Siguiente bloque (DEV)` | `GameClock.Instance?.AdvanceToNextBlock()` |
| `Siguiente día (DEV)` | `GameClock.Instance?.AdvanceToNextDay()` |

### Expedition (DEV)
| Button | Acción |
|--------|--------|
| `Salir de expedición (DEV)` | `ExpeditionBridge.Depart()` |

## Cambios S131

**Agregados:**
```csharp
[Button("Siguiente bloque (DEV)")]
private void AdvanceBlock() => GameClock.Instance?.AdvanceToNextBlock();

[Button("Siguiente día (DEV)")]
private void AdvanceDay() => GameClock.Instance?.AdvanceToNextDay();
```

**Propósito:** Testing rápido de bloque horario y cambios de día sin esperar tiempo real.

## Conexiones (S131)
- [[GameClock]] — AdvanceToNextBlock/AdvanceToNextDay

## Notas (S131 HC-4)
- Botones Clock solo aparecen si GameClock.Instance existe.
- Disparan eventos de día/bloque automáticamente.
