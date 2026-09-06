---
tags: [script, data, scriptableobject, animation, creatures]
---

# MonchiGestureSetSO.cs

**Ruta:** `Data/MonchiGestureSetSO.cs`

**Responsabilidad:** Tabla de gestos (animaciones discretas) por CreatureIntent. Diccionarios Odin de "entrada" (trigger breve) y "mantención" (loop). Fidgets ponderados por Boldness. Consultada por MonchiGestureDriver. **S103:** Agrega `Reporting → "Yes"` (gesto al reportar veta explorada).

**Campos Públicos:**
- `enterGestures` (Dict<CreatureIntent, string>) — gestos de entrada (breves)
- `holdGestures` (Dict<CreatureIntent, string>) — gestos loop/sustain
- `SickGesture` (string) = "Sick" — override si enfermo
- `FidgetInterval` (Vector2) = (4, 9) — rango segundos entre fidgets
- `Fidgets` (List<Fidget>) — aleatorios ponderados

**Nested Class Fidget:**
- State (string)
- Weight [Min(0)] = 1
- MinBoldness [Range(0,1)] = 0

**Métodos Públicos:**
- `TryEnterGesture(intent, out state) → bool` — busca gesto de entrada
- `TryHoldGesture(intent, out state) → bool` — busca gesto loop
- `PickFidget(boldness) → string` — selecciona ponderado
- `PopulateDefaults() [Button]` — inicializa con defaults

**PopulateDefaults() — EnterGestures (S103 ACTUALIZADO):**

| CreatureIntent | Gesto | Significado |
|---|---|---|
| Taking | "Eat" | Consume al minar |
| Fighting | "Roar" | Combate social |
| Losing | "No" | Rival toma mineral |
| Dazed | "No" | Post-golpe |
| Taunting | "Roar" | Decoy provoca |
| Securing | "Yes" | Deposita material |
| **Reporting** | **"Yes"** | **Scout reporta veta** |

**S103 Cambios:**
- Agrega `Reporting → "Yes"` en PopulateDefaults()
- PopulateDefaults() actualizado:
  ```csharp
  if (!enterGestures.ContainsKey(CreatureIntent.Reporting)) 
    enterGestures[CreatureIntent.Reporting] = "Yes";  // S103 NUEVO
  ```
- AgentScout emite EmitEmote(Curioso/Feliz) + gesto Yes si reporte fresco
- Gestos Carrying, Guarding, Hunting, Exploring no mapeados (locomotion normal)

**HoldGestures:**
- Resting, SleepingTogether → "Rest"

**Invariantes:**
- Diccionarios Odin extensibles, fachada segura (TryXxx retorna false si unmapped)
- Reporting comparte gesto "Yes" con Securing (celebración)
- Fidget weighted por Boldness

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MonchiGestureDriver]], [[AgentScout]], [[AgentExpedition]], [[CreatureIntent]], [[CreatureCondition]]
