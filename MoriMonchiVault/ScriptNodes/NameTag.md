---
tags: [script, ui, world-ui, creature-display]
---

# NameTag.cs

**Ruta:** `World/Creatures/NameTag.cs`

**Responsabilidad:** Placa flotante UITK sobre MoriMochi. Muestra nombre, gender, role, life stage, intent, precio (si está en venta), timer de cría (si está criando). S97: Expone `ShowDistance` como propiedad pública. S98: Renderiza gestos y beating. S99: `ScreenSizeReferenceDistance` propiedad pública; colores de placa por team. S131: Calcula edad en días usando `CreatureDNA.AgeDays(GameClock.Instance.Day)` para actualización dinámica de etapa de vida.

## Campos Serializados

**Visibility:**
| Campo | Tipo | Propósito |
|-------|------|----------|
| `showDistance` | float | Distancia máxima para mostrar placa (default 8f) |
| `uprightOnly` | bool | Mantiene etiqueta vertical sin camera pitch (default true) |

**Pen Layout (breeding):**
| Campo | Tipo | Propósito |
|-------|------|----------|
| `penRaise` | float | Altura extra en m cuando penned (default 0.6) |
| `penScale` | float | Escala uniforme cuando penned (default 0.8) |
| `screenSizeReferenceDistance` | float | Escala adaptativa por distancia si > 0 (S99) |
| `allyNameColor` | Color | Color nombre si Team==Player (default verde pastel) |
| `rivalNameColor` | Color | Color nombre si Team==Rival (default rojo pastel) |
| `rivalRevealSpeed` | float | Velocidad fade-in de rival (default 4f) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `ShowDistance` | float | Getter/setter para `showDistance` (S97) |
| `ScreenSizeReferenceDistance` | float | Getter/setter para `screenSizeReferenceDistance` (S99) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Bind(CreatureDNA creature, MoriMochiAgent agent)` | Wireo de DNA y agente. Resuelve UXML, aplica nombre/color, llama Refresh |

## Métodos Privados (Refresh)

| Método | Descripción |
|--------|-------------|
| `Refresh()` | Selector: RefreshStore / RefreshPenned / RefreshDefault según estado |
| `RefreshStore()` | Muestra precio solo |
| `RefreshPenned()` | Muestra gender/role/stage/breed/heart+timer si criando |
| `RefreshDefault()` | Muestra nombre/status/intent/pet hint |
| `StageText(int ageDays)` | **(S131)** Calcula etapa de vida desde días. Usa `CreatureLifeStageTableSO` |
| `CountdownText(long readyAtTicks)` | Cuenta atrás hasta BreedReadyAt |
| `NameColor(CreatureDNA)` | Elige color por team (allyNameColor / rivalNameColor / GenderColor) |

## Ciclo de Vida (LateUpdate)

1. Adquiere cámara (lazy ref a Camera.main)
2. Calcula distancia a cámara, determina visibilidad
3. Si cambió visibilidad, actualiza DisplayStyle
4. **Refresh:** actualiza contenido dinámico
5. Aplica escala pen + screen-size scaling
6. Rota hacia cámara (LookRotation con upright option)

## S131: Cálculo de Etapa de Vida

**Nuevo flow:**

```csharp
// En RefreshPenned/RefreshDefault, calcula edad:
int ageDays = dna.AgeDays(GameClock.Instance != null ? GameClock.Instance.Day : 1);
stageLabel.text = StageText(ageDays);
```

**StageText implementación (estimada):**
```csharp
private string StageText(int ageDays)
{
    if (BreedingController.Instance?.LifeStageTable == null)
        return $"{ageDays}d";
    
    var stage = BreedingController.Instance.LifeStageTable.StageFor(ageDays);
    if (stage.HasValue)
        return Loc.Tr($"ui.lifestage.{stage}") + $", {ageDays}d";
    
    return $"{ageDays}d";
}
```

**Dependencias:**
- `CreatureDNA.AgeDays(int today)` — `Max(0, today - BirthDay)`
- `GameClock.Instance.Day` — día actual del juego
- `CreatureLifeStageTableSO.StageFor(ageDays)` — mapea edad → LifeStage

## Campos Internos

| Campo | Tipo | Propósito |
|-------|------|----------|
| `document` | UIDocument | Componente del mismo GO |
| `root` | VisualElement | Raíz UXML |
| `nameLabel`, `stageLabel`, `breedLabel`, `timerLabel` | Label | Labels principales |
| `agent`, `dna` | ref | Wireadas en Bind |
| `cam` | Transform | Camera.main para LOD |
| `shown` | bool | Bandera de visibilidad actual |
| `baseLocalPos`, `baseLocalScale` | Vector3 | Guardadas para restaurar |

## Cambios S131

**Integración con GameClock:**
- `StageText()` ahora lee `GameClock.Instance.Day` para calcular edad
- `AgeDays` es método de CreatureDNA: `dna.AgeDays(today)` = `Max(0, today - BirthDay)`
- Actualización cada frame: edad dinámica mientras pasan días

**Invariantes:**
- Si GameClock falta, fallback a día=1
- Si LifeStageTable falta, muestra solo "Xd"

## Vinculado a

- [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[CreatureDNA]] — lee `AgeDays(int)`
- [[MoriMochiAgent]] — referencia para lógica

**Sistemas:**
- [[GameClock]] — proporciona `Day` para cálculo de edad
- [[BreedingController]] — acceso a LifeStageTable para mapeo edad→etapa
- [[CreatureLifeStageTableSO]] — lookup etapa por edad
- [[CustomerService]] — cálculo de precio
- [[CreatureAvailability]] — determinación de estado

## Notas (S131 HC-4)

- **AgeDays dinámico:** Cada frame recalcula edad si GameClock loaded. La etapa cambia automáticamente cuando se alcanza threshold.
- **Fallback:** Sin GameClock, asume día=1; sin LifeStageTable, muestra solo "Xd".
- **Color dinámica:** allyNameColor/rivalNameColor por team (S99+).
- **Responsive:** LateUpdate adapta escala y posición en tiempo real (LOD + pen layout).
