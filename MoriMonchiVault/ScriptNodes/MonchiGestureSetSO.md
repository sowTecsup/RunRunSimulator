---
tags: [script, data, scriptableobject, animation, creatures]
---

# MonchiGestureSetSO.cs

**Ruta:** `Data/MonchiGestureSetSO.cs`

**Responsabilidad:** Tabla de gestos (animaciones discretas) mapeados por intención. Define gestos de "entrada" (trigger breve), "mantención" (loop o sustain), fidgets ponderados por Boldness, y gesto de enfermedad. Consultada por `MonchiGestureDriver` cada frame para orquestar animaciones. **S100:** Agregado gesto "No" para Dazed (post-golpe).

## Estructura

**Nested class Fidget:**
```csharp
public class Fidget
{
    public string State;  // nombre del gesto (e.g., "No", "Yes", "Eat", "Roar")
    [Min(0f)] public float Weight = 1f;  // ponderación para random.choice
    [Range(0f, 1f)] public float MinBoldness = 0f;  // threshold: solo si Boldness >= este valor
}
```

**Campos Públicos:**
- `enterGestures` (Dictionary<CreatureIntent, string>, Odin) — gestos breves al cambiar intención (e.g., Taking → "Eat", Fighting → "Roar", Losing → "No", **Dazed → "No"**).
- `holdGestures` (Dictionary<CreatureIntent, string>, Odin) — gestos loop/sustain mientras dura intención (e.g., Resting → "Rest", SleepingTogether → "Rest").
- `SickGesture` (string, default "Sick") — gesto override si `CreatureCondition == Sick`.
- `FidgetInterval` (Vector2, default (4, 9)) — rango de segundos entre fidgets cuando está Idle/Wandering.
- `Fidgets` (List<Fidget>) — lista de gestos aleatorios ponderados, filtrados por Boldness actual.

## Métodos

- `TryEnterGesture(intent, out state) → bool` — busca gesto de entrada para intención; devuelve true si existe.
- `TryHoldGesture(intent, out state) → bool` — busca gesto de mantención para intención; devuelve true si existe.
- `PickFidget(boldness) → string` — selecciona aleatoriamente un fidget ponderado, filtrando por Boldness >= MinBoldness. Devuelve null si no hay válidos.
- `PopulateDefaults()` — **Botón Odin**: inicializa diccionarios y fidgets con ejemplares (Taking→"Eat", Fighting→"Roar", Losing→"No", **Dazed→"No"**; Resting/SleepingTogether→"Rest"; fidgets: No, Yes, Eat, Roar).

## Ciclo de Vida Típico (desde MonchiGestureDriver)

```csharp
OnEnable():
  nextFidget = Time.time + Random.Range(0, FidgetInterval.x)
  lastIntent = agent.Intent
  currentHold = ""

Update():
  intent = agent.Intent
  if (intent != lastIntent):
    if (set.TryEnterGesture(intent, out enterState))
      pendingEnter = enterState
    lastIntent = intent
  
  if (pendingEnter != "" && locomotion.PlayGesture(pendingEnter))
    pendingEnter = ""
  
  desiredHold = (CreatureCondition == Sick) ? SickGesture : (TryHoldGesture(intent) ? state : "")
  
  if (desiredHold != currentHold)
    actualizar currentHold via locomotion.HoldGesture()
  
  if (Time.time >= nextFidget && (intent == Idle || Wandering) && locomotion.IsStill)
    nextFidget = Time.time + Random.Range(FidgetInterval.x, FidgetInterval.y)
    fidget = PickFidget(agent.DNA.Boldness)
    if (fidget != null) locomotion.PlayGesture(fidget)
```

## Cambios S100: Dazed Gesture

**Línea 84 en PopulateDefaults():**
```csharp
if (!enterGestures.ContainsKey(CreatureIntent.Dazed)) enterGestures[CreatureIntent.Dazed] = "No";
```

**Razón:** Cuando [[AgentClash]] entra en fase Dazed (post-golpe, mareado), la criatura debería mostrar confusion/descontrol. El gesto "No" (cabezada de negación o espirales de confusión) refuerza visualmente el aturdimiento. Mismo gesto que Losing (para coherencia visual: pérdida/confusion).

**Consumo:**
- [[MonchiGestureDriver]] lee `agent.Intent` en Dazed y busca `TryEnterGesture(Dazed, out gesture)`
- Al transicionar a Dazed, dispara "No" automáticamente
- [[AgentClash.OnRecovered()]] termina Dazed y vuelve a Roaming/Clashing; gesture cambia automáticamente

## PopulateDefaults() — EnterGestures S100

| CreatureIntent | Gesto | Significado |
|---|---|---|
| Taking | "Eat" | Celebración de consumo |
| Fighting | "Roar" | Rugido de combate |
| Losing | "No" | Negación/confusión |
| **Dazed** | **"No"** | **Negación/aturdimiento (S100)** |

## Invariantes S100 + S98

- **Diccionarios Odin:** `[OdinSerialize] + SerializedScriptableObject` permite editar en Inspector sin generar wrapper classes. `[DictionaryDrawerSettings]` controla etiquetas de columnas (KeyLabel="Intent", ValueLabel="Enter Gesture").
- **Fidget weighted:** cada Fidget tiene peso (probabilidad) e intervalo de Boldness. `PickFidget()` filtra por Boldness actual del DNA y luego usa weighted random.
- **Sick override:** si `CreatureCondition == Sick`, se fuerza `SickGesture` sin respetar intención (toma prioridad).
- **Extensibilidad:** agregar intenciones nuevas a diccionarios en Inspector. `TryEnterGesture` devuelve false/empty string si no existe entrada → fachada segura.
- **S100:** Dazed mapea a "No" (gesto unificado con Losing para coherencia visual de incapacidad/confusión).

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (Parte 8: "Gestos y miradas")

## Conexiones

- [[MonchiGestureDriver]]
- [[MonchiLocomotionAnimator]]
- [[CreatureIntent]] (**S100:** incluye Clashing/Dazed)
- [[CreatureCondition]]
- [[CreatureDNA]]
- **S100:** [[AgentClash]] (genera Dazed intent que dispara gesto "No")
- **S100:** [[MonchiMoodDriver]] (Dazed → Mareado mood, coordinado con "No" gesto)
