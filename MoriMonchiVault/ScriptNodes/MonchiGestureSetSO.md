---
tags: [script, data, scriptableobject, animation, creatures]
---

# MonchiGestureSetSO.cs

**Ruta:** `Data/MonchiGestureSetSO.cs`

**Responsabilidad:** Tabla de gestos (animaciones discretas) mapeados por intención. Define gestos de "entrada" (trigger breve), "mantención" (loop), fidgets ponderados por Boldness, y gesto de enfermedad. Consultada por `MonchiGestureDriver` cada frame para orquestar animaciones. **S101:** Diccionario expandible para nuevos intents de ocupaciones (Carrying, Securing, Guarding, Hunting, Taunting). PopulateDefaults agrega `Taunting → "Roar"` y `Securing → "Yes"` como gestos de entrada (ya poblados en asset). Puede dejarlos unmapped (fachada segura).

## Estructura

**Nested class Fidget:**
```csharp
public class Fidget
{
    public string State;  // nombre del gesto
    [Min(0f)] public float Weight = 1f;  // ponderación
    [Range(0f, 1f)] public float MinBoldness = 0f;  // threshold
}
```

**Campos Públicos:**
- `enterGestures` (Dictionary<CreatureIntent, string>, Odin) — gestos breves al cambiar intención
- `holdGestures` (Dictionary<CreatureIntent, string>, Odin) — gestos loop/sustain
- `SickGesture` (string, default "Sick") — override si Sick
- `FidgetInterval` (Vector2, default (4, 9)) — rango segundos entre fidgets
- `Fidgets` (List<Fidget>) — lista de gestos aleatorios ponderados

## Métodos

- `TryEnterGesture(intent, out state) → bool` — busca gesto de entrada para intención
- `TryHoldGesture(intent, out state) → bool` — busca gesto de mantención
- `PickFidget(boldness) → string` — selecciona fidget ponderado
- `PopulateDefaults()` — **Botón Odin**: inicializa diccionarios y fidgets

## PopulateDefaults() — EnterGestures S101

| CreatureIntent | Gesto | Significado | Cuando |
|---|---|---|---|
| Taking | "Eat" | Celebración de consumo | Minando mineral |
| Fighting | "Roar" | Rugido de combate | Social Fighting |
| Losing | "No" | Negación/confusión | Rival toma su mineral |
| Dazed | "No" | Negación/aturdimiento | Post-golpe en Clashing |
| **Taunting** | **"Roar"** | **Provocación/desafío** | **Decoy en Approach/Taunt** |
| **Securing** | **"Yes"** | **Celebración de depósito** | **Deposita material en salida** |
| (Carrying, Guarding, Hunting) | unmapped | usan default o idle gesture | según ocupación |

**S101 Detalles:**
- `Taunting → "Roar"` — Decoy provoca rival, emota Molesto, gesticula Roar (mismo que combate)
- `Securing → "Yes"` — Gather tras minar y cargar, deposita en salida con gesto Yes (celebración)
- **Nota:** Carrying (mientras carga material desde Mining) no tiene gesto de entrada (sigue locomotion normal)
- **Nota:** Guarding (vigilancia) no tiene gesto de entrada (sigue locomotion)
- **Nota:** Hunting (Break persiguiendo rival) no tiene gesto de entrada (sigue locomotion, puede ser Clashing si entra en combate)

**Código PopulateDefaults() S101:**
```csharp
if (!enterGestures.ContainsKey(CreatureIntent.Taking)) enterGestures[CreatureIntent.Taking] = "Eat";
if (!enterGestures.ContainsKey(CreatureIntent.Fighting)) enterGestures[CreatureIntent.Fighting] = "Roar";
if (!enterGestures.ContainsKey(CreatureIntent.Losing)) enterGestures[CreatureIntent.Losing] = "No";
if (!enterGestures.ContainsKey(CreatureIntent.Dazed)) enterGestures[CreatureIntent.Dazed] = "No";
if (!enterGestures.ContainsKey(CreatureIntent.Taunting)) enterGestures[CreatureIntent.Taunting] = "Roar";  // S101 NUEVO
if (!enterGestures.ContainsKey(CreatureIntent.Securing)) enterGestures[CreatureIntent.Securing] = "Yes";  // S101 NUEVO
```

## HoldGestures (sin cambios S101)

```csharp
if (!holdGestures.ContainsKey(CreatureIntent.Resting)) holdGestures[CreatureIntent.Resting] = "Rest";
if (!holdGestures.ContainsKey(CreatureIntent.SleepingTogether)) holdGestures[CreatureIntent.SleepingTogether] = "Rest";
```

- Resting/SleepingTogether: "Rest" (loop dormida)
- Sin mapeos para ocupación (Carrying/Guarding/Hunting/Taunting/Securing son transitorios, no loops)

## Ciclo de Vida Típico (desde MonchiGestureDriver)

```csharp
Update():
  intent = agent.Intent  // puede ser Taking, Losing, Clashing, Dazed, Carrying, Securing, Taunting, Guarding, Hunting, etc.
  if (intent != lastIntent):
    if (set.TryEnterGesture(intent, out enterState))
      pendingEnter = enterState  // toca gesto breve si existe
    lastIntent = intent
  
  desiredHold = (Sick) ? SickGesture : (TryHoldGesture(intent) ? state : "")
  // si Carrying/Securing/Taunting/Guarding/Hunting no están mapeados → desiredHold = ""
  
  // continúa con locomotion normal (walk/idle) si desiredHold está vacío
```

**Ejemplo S101:**
- Agent en Taking (minando) → TryEnterGesture(Taking) → "Eat" pendiente
- Intent cambia a Securing (deposita) → TryEnterGesture(Securing) → "Yes" pendiente
- Intent cambia a Carrying (cargando) → TryEnterGesture(Carrying) → unmapped, devuelve false, sigue locomotion
- Intent = Taunting (Decoy provoca) → TryEnterGesture(Taunting) → "Roar" pendiente

## Invariantes S101 + S100

- **Diccionarios Odin:** extensibles en Inspector; TryEnterGesture/TryHoldGesture devuelven false/empty si no existe entrada (fachada segura).
- **S101 nuevos mapeados por defecto:** Taunting → "Roar", Securing → "Yes"; resto (Carrying, Guarding, Hunting) sin gesto por defecto (pueden agregarse en Inspector si se necesita customización).
- **Ocupación → Intent:** Cada ocupación genera sus propios intents (Gather → Taking/Carrying/Securing, Guard → Guarding, Break → Hunting, Decoy → Taunting).
- **Fidget weighted:** cada Fidget tiene peso (probabilidad) e intervalo de Boldness.
- **Sick override:** toma prioridad si CreatureCondition == Sick.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (Parte 8: "Gestos y miradas")

## Conexiones

- [[MonchiGestureDriver]]
- [[MonchiLocomotionAnimator]]
- [[CreatureIntent]] (keys: S101 nuevos: Carrying, Securing, Guarding, Hunting, Taunting)
- [[AgentExpedition]] (genera nuevos intents)
- [[CreatureCondition]] (Sick override)
