---
tags: [script, combat, visual]
---

# CombatFeelDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatFeelDirector.cs`

**Responsabilidad (S46):** Único propietario de los MMFeedbacks (Feel) del replay de combate. `SerializedMonoBehaviour` de Odin que vive en la escena CombatVisualizerMM (GO CombatVisualizer). En lugar de duplicar 12 sistemas de partículas por prefab del MM, este director reproduce todos los feedbacks EN la posición del MM afectado usando `MMFeedbacks.PlayFeedbacks(Vector3)`. Obtiene posiciones via `CombatVisualizerService.PosOf(side, index)` — mismo patrón que `CombatCameraDirector` usa para `VCamOf`. **S47:** Tres nuevos toggles de mute (muteSoporte, muteMarcas, muteEstados) permiten silenciar secciones de feedbacks independientemente para testeo.

## Campos Públicos (TabGroup structure)

### Tab "Soporte"
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `muteSoporte` | `bool` | **S47 NEW** Gate para silenciar feedbacks de Shield + Heal (testeo rápido) |
| `shieldFeedback` | `MMFeedbacks` | Partículas sobre el aliado que recibe escudo |
| `healFeedback` | `MMFeedbacks` | Partículas sobre el aliado que recibe curación |

### Tab "Marcas"
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `muteMarcas` | `bool` | **S47 NEW** Gate para silenciar feedbacks de MarkApplied (testeo rápido) |
| `markFeedbacks` | `Dictionary<Element, MMFeedbacks>` | 4 elementos (uno por cada Element enum), feedback sobre quien recibe marca |

### Tab "Estados"
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `muteEstados` | `bool` | **S47 NEW** Gate para silenciar feedbacks de Reaction/estados (testeo rápido) |
| `stateFeedbacks` | `Dictionary<ElementalState, MMFeedbacks>` | 12 estados, feedback sobre quien detona reacción |

### Tab "Ajustes"
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `offset` | `Vector3` | Altura sumada a la posición antes de reproducir (default (0, 1, 0)) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe `OnPopup` y `OnUnitElement` |
| `OnDisable()` | Desuscribe eventos |
| `HandlePopup(CombatVisualPopup p)` | **S47** Checkea `muteSoporte` antes de reproducir Shield/Heal |
| `HandleUnitElement(CombatElementEventData d)` | **S47** Checkea `muteMarcas` (MarkApplied) o `muteEstados` (Reaction) según Kind |
| `PlayOn(MMFeedbacks, CombatVisualSide, int)` | Helper: resuelve posición del unit y reproduce |
| `Play(MMFeedbacks, Vector3)` | Reproduce feedback en posición (con offset) |

## Métodos Editor

| Método | Descripción |
|--------|-------------|
| `BuildFeedbackObjects()` | **#if UNITY_EDITOR** Botón: crea 18 GameObjects hijos (Feel_Shield, Feel_Heal, Feel_Mark_*, Feel_State_*) y wirealiza ref si no existen; idempotente |
| `EnsureChild(childName, current)` | Helper privado: busca/crea GameObject hijo, agrega MMF_Player si falta |

## Cambios S47

**Tres nuevos toggles de mute:**

```csharp
[TabGroup(Group, "Soporte")]
[SerializeField] private bool muteSoporte;

[TabGroup(Group, "Marcas")]
[SerializeField] private bool muteMarcas;

[TabGroup(Group, "Estados")]
[SerializeField] private bool muteEstados;
```

**Gates en HandlePopup y HandleUnitElement:**

```csharp
private void HandlePopup(CombatVisualPopup p)
{
    if (muteSoporte) return;  // S47 NEW
    if (p.Kind == CombatPopupKind.Shield)    Play(shieldFeedback, p.Position);
    else if (p.Kind == CombatPopupKind.Heal) Play(healFeedback, p.Position);
}

private void HandleUnitElement(CombatElementEventData d)
{
    if (d.Kind == ElementEventKind.MarkApplied)
    {
        if (muteMarcas) return;  // S47 NEW
        if (markFeedbacks.TryGetValue(d.Element, out var mark)) PlayOn(mark, d.Side, d.Index);
        return;
    }

    if (d.Kind == ElementEventKind.Reaction)
    {
        if (muteEstados) return;  // S47 NEW
        if (Enum.TryParse<ElementalState>(d.ReactionName, out var state)
         && stateFeedbacks.TryGetValue(state, out var feedback))
            PlayOn(feedback, d.Side, d.Index);
    }
}
```

**Impacto:** Permite testear el replay sin feedbacks ruidosos, one-click toggle per-section.

## Flujo de Reproducción

1. `CombatVisualizerService.PlayProc()` emite `CombatVisualEvents.OnPopup` (Shield/Heal) o `CombatVisualEvents.OnUnitElement` (MarkApplied/Reaction)
2. `HandlePopup()` (si `!muteSoporte`) → `Play(feedback, p.Position + offset)`
3. `HandleUnitElement()` (si `!muteMarcas` / `!muteEstados`) → `PlayOn(feedback, side, index)`
4. `PlayOn()` resuelve posición via `CombatVisualizerService.PosOf(side, index)`
5. `Play()` → `MMFeedbacks.PlayFeedbacks(Vector3 worldPos + offset)`

**Timing:** Los feedbacks reproducen justo cuando el evento visual se emite (sin delay adicional).

## Identificación de Estados

Los 12 estados se identifican parseando `CombatElementEventData.ReactionName` contra `ElementalState` enum:

```csharp
if (Enum.TryParse<ElementalState>(d.ReactionName, out var state)
 && stateFeedbacks.TryGetValue(state, out var feedback))
    PlayOn(feedback, d.Side, d.Index);
```

**Nota:** Si se renombra una reacción en ElementTableSO (p.ej. "Vaporizado" → "Evaporado"), el parse fallará silenciosamente y no saldrá partícula. Los nombres de reacción y el enum deben estar sincronizados.

## Button Editor "Crear objetos de feedback y wirear"

Ubicado en tab "Ajustes", color verde (0.4, 1, 0.6).

**Comportamiento idempotente:**
- Busca/crea hijo "Feel_Shield" con `MMF_Player`, asigna ref si null
- Busca/crea hijo "Feel_Heal" con `MMF_Player`, asigna ref si null
- Para cada `Element` en enum: busca/crea "Feel_Mark_{Element}" con `MMF_Player`
- Para cada `ElementalState` en enum: busca/crea "Feel_State_{ElementalState}" con `MMF_Player`
- **Resultado:** 18 GameObjects hijos (1 shield + 1 heal + 4 marks + 12 states), todos con `MMF_Player` auto-wireados en los dicts

**Workflow típico:**
1. Crear escena vacía o reutilizar GO existente
2. Agregar componente CombatFeelDirector
3. Clic "Crear objetos de feedback y wirear"
4. Expande cada GameObject y configura `MMF_Player` en inspector (arrastrar loops de partículas, etc.)
5. Guardar escena

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — publisher OnPopup + OnUnitElement
- [[CombatVisualizerService]] — PosOf() (S46), PlayProc() emite eventos

## Conexiones

**Entrada:** Suscribe eventos estáticos de `CombatVisualEvents` (OnPopup, OnUnitElement)

**Salida:** Reproduce MMFeedbacks en posiciones mundo

**Comportamiento:** Scene-local (GO único en CombatVisualizerMM), no es singleton; SI hay varios, ambos responden (duplicado).

## Notas S47

- Tres mutes independientes (muteSoporte, muteMarcas, muteEstados) permiten aislar feedback problemáticos durante desarrollo
- Default: todos false (todos los feedbacks activos)
- Se persisten en .cs (valores en inspector), no en ScriptableObject
- Útil para testeo de coreografía sin ruido visual
