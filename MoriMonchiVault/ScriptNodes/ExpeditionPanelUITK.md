---
tags: [script, ui, expedition, uitk]
---

# ExpeditionPanelUITK.cs

**Ruta:** `UI/ExpeditionPanelUITK.cs`

**Responsabilidad:** Panel UITK elegir elenco antes de bajar (3 criaturas). S131: Botón "Bajar" solo habilitado si `DayBlockDef.ExpeditionOpen == true` (bloques nocturnos). Escucha `GameEvents.OnDayBlockChanged`. Subtítulo muestra "ui.expedition.night_only" si expediciones cerradas.

## Cambios S131

**Suscripción a OnDayBlockChanged:**
```csharp
void OnEnable()
{
    GameEvents.OnDayBlockChanged += HandleBlockChanged;
}

void HandleBlockChanged(DayBlockDef block)
{
    bool canDepart = block?.ExpeditionOpen ?? false;
    departButton.SetEnabled(canDepart);
    
    if (!canDepart)
        subtitleLabel.text = Loc.Tr("ui.expedition.night_only");
}
```

**Botón Bajar:**
- Habilitado solo si ExpeditionOpen
- Grisado y deshabilitado de lo contrario

## Integración S131
- Reacciona a cambios de bloque horario
- Visuals dinámicos (habilita/deshabilita botón)

## Conexiones (S131)
- [[GameClock]] → dispara OnDayBlockChanged
- [[DayScheduleSO]] → proporciona bloques con ExpeditionOpen

## Notas (S131)
- "Noche" = bloques con ExpeditionOpen=true (típicamente 23:00-05:59).
- UI string "ui.expedition.night_only" para mensaje.
