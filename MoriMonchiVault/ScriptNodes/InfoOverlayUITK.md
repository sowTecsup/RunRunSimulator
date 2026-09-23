---
tags: [script, ui, overlay]
---

# InfoOverlayUITK.cs

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible. S131: Label "reloj" muestra día/minuto de juego (ej "Día 5 · 09:45"). Escucha `OnDayBlockChanged` / `OnDayStarted` para actualizar. String localización "ui.overlay.clock".

## Display Clock (S131)

```csharp
void UpdateClock()
{
    if (GameClock.Instance == null) return;
    
    int day = GameClock.Instance.Day;
    float minute = GameClock.Instance.MinuteOfDay;
    int hour = (int)(minute / 60);
    int min = (int)(minute % 60);
    
    clockLabel.text = $"{Loc.Tr("ui.overlay.clock")} {day} · {hour:D2}:{min:D2}";
}
```

## Suscripciones S131

```csharp
void OnEnable()
{
    GameEvents.OnDayBlockChanged += HandleBlockChanged;
    GameEvents.OnDayStarted += HandleDayStarted;
}

void HandleBlockChanged(DayBlockDef block) => UpdateClock();
void HandleDayStarted(int day) => UpdateClock();
```

## Cambios S131
- Label reloj con día/hora/minuto de juego
- Actualización dinámica en cambios de día/bloque

## Conexiones (S131)
- [[GameClock]] — proporciona Day, MinuteOfDay
- [[GameEvents]] — OnDayBlockChanged, OnDayStarted

## Notas (S131)
- String "ui.overlay.clock" para localización.
- Formato "Día X · HH:MM".
