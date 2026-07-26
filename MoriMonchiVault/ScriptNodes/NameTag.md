---
tags: [script, world, ui, uitk]
---

# NameTag.cs

**Ruta:** `World/Creatures/NameTag.cs`

**Responsabilidad:** Label world-space UITK sobre criaturas. Billboard (opcional `uprightOnly`). Tres layouts: **store** (nombre + precio, para `IsForSale`), **pen** (glyph género + nombre + rol + etapa+días + contador crías + corazón/timer si incubando, elevación `penRaise` y escala `penScale` para no clipar el suelo) y **default** (nombre + estado busy/dead + intent + "[E] Acariciar" si reacción amistosa y jugador mirando). El nombre se colorea por género en `Bind()` (azul claro ♂ / rosa ♀) reutilizando helper `GenderColor`. Muestra "Petting..." 1.5s tras acariciar. `CountdownText` para huevo (mm:ss / "¡Listo! [E]"). Distancia de visibilidad configurable. Lee `LifeStageTable` de `BreedingController.Instance` para traducir `AgeDays` a etapa. **S65:** Nuevos textos para SleepingTogether ("Durmiendo juntos") y Fighting ("¡Peleando!"). **S68:** IntentText y RoleText eliminados — delegan en LocEnumMaps.

## Cambios S68 (Localization-ready)

**Eliminados los helpers privados:**
- `IntentText(CreatureIntent intent)` → ahora `LocEnumMaps.IntentName(agent.Intent)` (línea 273)
- Métodos de nombre de rol privados reemplazados → `LocEnumMaps.RoleName(dna.Role)` (línea 210)

**Líneas de localización agregadas:**
- Línea 188: `Loc.Tr("nametag.price", price)` (store layout)
- Línea 210: `LocEnumMaps.RoleName(dna.Role)` (pen layout)
- Línea 213: `Loc.Tr("nametag.stageage", LocEnumMaps.LifeStageName(...), ageDays)` (pen layout)
- Línea 262: `Loc.Tr("nametag.petting")` (pet hint cuando acariciando)
- Línea 262: `Loc.Tr("nametag.pethint")` (pet hint cuando disponible)
- Línea 273: `LocEnumMaps.IntentName(agent.Intent)` (default layout, intent interesante)
- Línea 284: `Loc.Tr("status.dead")` (dead status)
- Línea 287: `Loc.Tr("status.queued")` (busy queued status)
- Línea 288: `Loc.Tr("status.breeding")` (busy breeding status)
- Línea 321: `Loc.Tr("nametag.ready")` (egg ready)

## Campos Principales

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `showDistance` | `float` | Distancia (m) a la que el tag es visible (default 8) |
| `uprightOnly` | `bool` | Si true, solo rota en Y (text upright, no full billboard) |
| `penRaise` | `float` | Altura extra mientras penned (default 0.6) |
| `penScale` | `float` | Escala extra mientras penned (default 0.8) |
| `roleLabel` | `Label` | Query "role-label" (S39: antes personalityLabel) |

## Layouts de NameTag

### Store Layout
- Método `RefreshStore()`: lee `CustomerService.EstimateAverage(dna)` para el precio, muestra vía `Loc.Tr("nametag.price", price)`.
- Campo `priceLabel` (ocultado en layouts default/pen).
- Mostrado cuando `agent.IsForSale == true`.

### Pen Layout
- Método `RefreshPenned()`:
  - Glyph género (♂/♀) con color genérico
  - **Nombre de rol** vía `LocEnumMaps.RoleName()` ("Protector" / "Agresivo" / "Empático", S39)
  - Etapa de vida (etiqueta + días, e.g. "Adolescente · 5d")
  - Contador de crías ("X/MaxBreedCount")
  - Corazón + timer si incubando
- Mostrado cuando `agent.IsPenned == true`.

### Default Layout
- Método `RefreshDefault()`:
  - Estado (busy: "En cola", "Incubando"; dead: "Muerto") con color
  - Intent actual vía `LocEnumMaps.IntentName()` (Quieto, Paseando, Te sigue, Huye, Busca comida, Socializando [S64], Durmiendo juntos [S65], ¡Peleando! [S65], etc.)
  - "[E] Acariciar" si `IsInFriendlyReaction && IsPlayerFacingMe()`
  - "Petting..." transitorio si `IsBeingPetted`
- Mostrado por defecto (libre roaming).

## Helper Methods

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GenderGlyph(CreatureGender g)` | `string` | "♂" / "♀" / "?" |
| `GenderColor(CreatureGender g)` | `Color` | Azul claro (♂) / Rosa (♀) / Gris (?) |
| `StageText(int ageDays)` | `string` | "Etapa · XdY" via LocEnumMaps o "XdY" si tabla ausente |
| `CountdownText(long readyAtMs)` | `string` | "mm:ss" o "¡Listo! [E]" si due |
| `StatusOf(CreatureDNA dna)` | `(string, Color)` | ("Muerto" / "En cola" / "Incubando" / "", color) — ahora vía Loc.Tr |

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMonchiVault/Index/14 - Social V2]]
- [[BreedingController]] — resolve LifeStageTable en StageText
- [[CustomerService]] — price estimate en RefreshStore
- [[MoriMochiAgent]] — resolve intent + petting state
- [[CreatureDNA]] — source de datos (Role S39, AgeDays, BusyState, Gender, CustomName)
- [[Loc]] (wrapper localización)
- [[LocEnumMaps]] (traducciones enum)

## Conexiones

**Entrada:**
- `Bind(creature, agent)` — wiring inicial (llama ResolveElements + Refresh)
- `LateUpdate()` — distance gating, billboard (posición/rotación world-up), llamadas Refresh cada frame

**Salida:**
- UIDocument visual: labels actualizadas con datos DNA/agent live
- Billboard rotation hacia cámara (desacoplado del tumble del padre)

**S64 Compartido:**
- [[MonchiEmoteBubble]] — comparte mismo UIDocument, inserta Label en "tag-root"

## Notas

- **Odin:** Sin dependencia de Odin, puro UIToolkit.
- **S39 cambio:** Rol reemplaza Personalidad en display pen layout.
- **S64 cambio:** UIDocument compartido con MonchiEmoteBubble. La posición/rotación del GO es world-up.
- **S65 cambio:** IntentText extendido con SleepingTogether y Fighting.
- **S68 cambio:** IntentText y RoleText eliminados (delegados a LocEnumMaps); todas las strings del display ahora vía Loc.Tr/LocEnumMaps.
- **Live data:** Todos los valores (nombre, etapa, intent, estado) se leen cada frame de DNA/agent.
- **Query robusta:** Si UXML element no existe, el Label ref se deja null, y SetDisplay se cuida de nulls.
- **Backward compat:** Si BreedingController ausente, StageText solo muestra días.
