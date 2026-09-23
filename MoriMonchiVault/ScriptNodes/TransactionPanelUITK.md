---
tags: [script, ui, uitk, store]
---

# TransactionPanelUITK.cs

**Ruta:** `UI/TransactionPanelUITK.cs`

**Responsabilidad:** Panel UITK negociación de venta (3 columnas: cliente | retrato MM + info | oferta). Actualiza labels dinámicamente. S131: Edad muestra `CreatureDNA.AgeDays(GameClock.Instance.Day)` (días de juego).

## Display (Refresh) S131

```csharp
void Refresh()
{
    var mm = currentCreature;
    int today = GameClock.Instance?.Day ?? 1;
    int ageDays = mm.AgeDays(today);
    
    targetInfo.text = $"{Loc.Tr($"ui.gender.{mm.Gender}")} · {ageDays}d";
    // ... resto de labels
}
```

## Botones
| Botón | Acción |
|-------|--------|
| Accept | Acepta oferta + cierra |
| Counter | Pide más (si no contraofertó) |
| Reject | Rechaza + cierra |

## Cambio S131
- Edad dinámica via AgeDays(today)
- Integración con GameClock

## Conexiones (S131)
- [[GameClock]] — proporciona Day

## Notas (S131)
- Edad "Xd" (días).
