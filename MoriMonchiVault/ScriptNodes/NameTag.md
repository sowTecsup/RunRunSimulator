---
tags: [script, world, ui]
---

# NameTag

**Ruta:** `World/Creatures/NameTag.cs`

**Responsabilidad:** Label world-space UITK sobre criaturas. Billboard (opcional `uprightOnly`). Tres layouts: **store** (nombre + precio, para `IsForSale`), **pen** (glyph género + nombre + **rol** + etapa+días + contador crías + corazón/timer si incubando, elevación `penRaise` y escala `penScale` para no clipar el suelo) y **default** (nombre + etapa+días + estado busy/dead + intent + "[E] Acariciar" si reacción amistosa y jugador mirando). El nombre se colorea por género en `Bind()` (azul claro ♂ / rosa ♀) reutilizando helper `GenderColor`. Muestra "Petting..." 1.5s tras acariciar. `CountdownText` para huevo (mm:ss / "¡Listo! [E]"). Distancia de visibilidad configurable. Lee `LifeStageTable` de `BreedingController.Instance` para traducir `AgeDays` a etapa.

## Cambios S39

**Rol display (antes Personalidad):**
- Campo `roleLabel` (antes `personalityLabel`)
- Método `RoleText(Role r)` (antes `PersonalityText(Personality p)`)
- Query UXML: `"role-label"` (antes `"personality-label"`)
- Retorna: "Protector", "Agresivo", "Empático" (Role enum, S37/S39)

**RefreshPenned() actualizado:**
```csharp
if (roleLabel != null)
{
    roleLabel.text = RoleText(dna.Role);  // ahora Role, no Personality
    SetDisplay(roleLabel, true);
}
```

## Layouts de NameTag

### Store Layout
- Método `RefreshStore()`: lee `CustomerService.EstimateAverage(dna)` para el precio, muestra "X D".
- Campo `priceLabel` (ocultado en layouts default/pen).
- Mostrado cuando `agent.IsForSale == true`.

### Pen Layout
- Método `RefreshPenned()`: 
  - Glyph género (♂/♀) con color genérico
  - **Nombre de rol** ("Protector" / "Agresivo" / "Empático", S39)
  - Etapa de vida (etiqueta + días, e.g. "Adolescente · 5d")
  - Contador de crías ("X/MaxBreedCount")
  - Corazón + timer si incubando
- Mostrado cuando `agent.IsPenned == true`.

### Default Layout
- Método `RefreshDefault()`:
  - Etapa + días
  - Estado (busy: "En cola", "Incubando"; dead: "Muerto") con color
  - Intent actual (Quieto, Paseando, Te sigue, Huye, Busca comida, etc.)
  - "[E] Acariciar" si `IsInFriendlyReaction && IsPlayerFacingMe()`
  - "Petting..." transitorio si `IsBeingPetted`
- Mostrado por defecto (libre roaming).

## Campos Principales

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `showDistance` | `float` | Distancia (m) a la que el tag es visible (default 8) |
| `uprightOnly` | `bool` | Si true, solo rota en Y (text upright, no full billboard) |
| `penRaise` | `float` | Altura extra mientras penned (default 0.6) |
| `penScale` | `float` | Escala extra mientras penned (default 0.8) |
| `roleLabel` | `Label` | Query "role-label" (S39: antes personalityLabel) |

## Helper Methods

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `RoleText(Role r)` | `string` | "Protector" / "Agresivo" / "Empático" (S39) |
| `GenderGlyph(CreatureGender g)` | `string` | "♂" / "♀" / "?" |
| `GenderColor(CreatureGender g)` | `Color` | Azul claro (♂) / Rosa (♀) / Gris (?) |
| `StageText(int ageDays)` | `string` | "Etapa · XdY" o "XdY" si tabla ausente |
| `CountdownText(long readyAtMs)` | `string` | "mm:ss" o "¡Listo! [E]" si due |
| `IntentText(CreatureIntent intent)` | `string` | "Quieto", "Paseando", "Te sigue", etc. |
| `StatusOf(CreatureDNA dna)` | `(string, Color)` | ("Muerto" / "En cola" / "Incubando" / "", color) |

## Vinculado a

- [[Index/06 - Player & World]]
- [[BreedingController]] — resolve LifeStageTable en StageText
- [[CustomerService]] — price estimate en RefreshStore
- [[MoriMochiAgent]] — resolve intent + petting state
- [[CreatureDNA]] — source de datos (Role S39, AgeDays, BusyState, Gender, CustomName)
- [[Role]] — enum Protector/Agresivo/Empático

## Conexiones

**Entrada:**
- `Bind(creature, agent)` — wiring inicial (llama ResolveElements + Refresh)
- `LateUpdate()` — distance gating, billboard, llamadas Refresh cada frame

**Salida:**
- UIDocument visual: labels actualizadas con datos DNA/agent live
- Billboard rotation hacia cámara

## Notas

- **Odin:** Sin dependencia de Odin, puro UIToolkit.
- **S39 cambio:** Rol reemplaza Personalidad en display pen layout. RoleText() retorna nombre legible del Role enum.
- **Live data:** Todos los valores (nombre, etapa, intent, estado) se leen cada frame de DNA/agent; no hay cache (Refresh deja de ejecutarse si no visible).
- **Query robusta:** Si UXML element no existe, el Label ref se deja null, y SetDisplay se cuida de nulls.
- **Backward compat:** Si BreedingController ausente, StageText solo muestra días (sin etapa).
