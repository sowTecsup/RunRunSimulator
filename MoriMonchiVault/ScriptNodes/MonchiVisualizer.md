---
tags: [script, visual, component]
---

# MonchiVisualizer.cs

**Ruta:** `World/Creatures/MonchiVisualizer.cs`

**Responsabilidad:** Visualizador del modelo Suriyun. Instancia body FBX por BodyShapeID, mapea renderers (Face, Wings, Arms, etc.), aplica tintado por ColorGenetics.BuildHarmony. `SetMood()` swapea material Face. **S61:** `Assemble()` ahora hace `SetActive(false)` a los hijos viejos antes de `Object.Destroy()` — Destroy es diferido a fin de frame y el fotomatón renderiza en el mismo frame, causando superposición del cuerpo viejo en headshots batch. **S110:** Nuevos métodos `SetRimOverride()` y `ClearRimOverride()` para controlar rim light genético (anulable con color/power/mask de rival).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `SetBank(MonchiVisualBankSO)` | Asigna banco visual |
| `SetFurDatabase(FurTypeDatabaseSO)` | Asigna database de pelajes |
| `Assemble(CreatureDNA dna)` | Instancia body, mapea renderers, aplica look; desactiva hijos viejos antes de destruir |
| `RefreshLook(CreatureDNA dna)` | Retinta sin re-instanciar |
| `SetMood(MonchiMood)` | Swapea material Face |
| `SetRimOverride(Color color, float power, float insideMask)` | **S110 NUEVO** anula rim light genético con color/power/mask de rival |
| `ClearRimOverride()` | **S110 NUEVO** restaura rim light genético |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Animator` | `Animator` | Animator del body |
| `ModelRoot` | `Transform` | Raíz del modelo |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `bank` | `MonchiVisualBankSO` | Visual bank |
| `furDatabase` | `FurTypeDatabaseSO` | Database de pelajes |
| `bodyInstance` | `GameObject` | Instancia del body prefab |
| `animator` | `Animator` | Animator del body |
| `faceRenderer` | `SkinnedMeshRenderer` | Renderer del rostro |
| `tintRenderers` | `List<SkinnedMeshRenderer>` | Renderers a teñir (alas, cuernos, espalda, etc.) |
| `currentDna` | `CreatureDNA` | DNA vigente |
| `currentMood` | `MonchiMood` | Mood vigente |
| `rimOverride` | `bool` | **S110 NUEVO** si se aplica override de rim |
| `rimOverrideColor` | `Color` | **S110 NUEVO** color override |
| `rimOverridePower` | `float` | **S110 NUEVO** power override |
| `rimOverrideInsideMask` | `float` | **S110 NUEVO** inside mask override |

## Cambios S61

**Assemble() línea 40-45:**
```csharp
for (int i = modelRoot.childCount - 1; i >= 0; i--)
{
    var child = modelRoot.GetChild(i).gameObject;
    child.SetActive(false);              // NUEVO: desactiva antes de destruir
    Object.Destroy(child);
}
```

**Contexto:**
- Destroy() es una operación diferida que se ejecuta al fin del frame actual
- El fotomatón (headshot batch render) renderiza en el MISMO frame antes de que Destroy() se ejecute
- Sin SetActive(false), el body viejo sigue visible en el render, superponiéndose al cuerpo nuevo
- Con SetActive(false), el renderer se desactiva inmediatamente, saliendo de la vista del fotomatón

**Impacto:**
- Evita ghosting visual en headshots batch (artefactos de dos cabezas/cuerpos superpuestos)
- La instancia aún existe en memoria hasta fin de frame, pero es invisible

## Cambios S93

- **Removido:** método `SetGhost(float alpha)` (fue descartado; ghosting visual de cadáveres se maneja en otro lado o no se soporta más)

## Cambios S107

- Sin cambios en lógica del script
- MonchiTurntable (S107 NUEVO) usa MonchiVisualizer en sus booths para renderizar spinning 3D

## Cambios S110

**SetRimOverride(Color, float, float) — NUEVO (línea 109-115):**
```csharp
public void SetRimOverride(Color color, float power, float insideMask)
{
    rimOverride = true;
    rimOverrideColor = color;
    rimOverridePower = power;
    rimOverrideInsideMask = insideMask;
    ApplyLook();
}
```
- Activa override y guarda valores
- Llama `ApplyLook()` para retintar con nuevos valores de rim
- Usado por MonchiTeamRim cuando agent.Team == Rival

**ClearRimOverride() — NUEVO (línea 118-123):**
```csharp
public void ClearRimOverride()
{
    if (!rimOverride) return;
    rimOverride = false;
    ApplyLook();
}
```
- Desactiva override
- Llama `ApplyLook()` para restaurar rim genético
- Usado por MonchiTeamRim cuando agent.Team != Rival

**Tint() método — MODIFICADO para aplicar override (línea 176-195):**
- Si `rimOverride`:
  - Escribe en MPB: `_RimLightColor = rimOverrideColor`
  - Escribe en MPB: `_RimLight_Power = rimOverridePower`
  - Escribe en MPB: `_RimLight_InsideMask = rimOverrideInsideMask`
  - Escribe en MPB: `_Is_LightColor_RimLight = 0f` (switch a modo luz)
- Sino: usa rim genético `Lerp(color, Color.white, 0.65f)`

**Impacto S110:**
- Rivales tienen rim rojo (1, 0.3, 0.22) para identidad visual clara
- Aliados conservan rim genético por color de pelaje
- Override se aplica en tintado, no globalizado (cada criatura teñida independientemente)

## Invariantes

- Assemble() desactiva visualmente los hijos viejos inmediatamente (SetActive), luego los destruye diferido
- Fotomatón renderiza en el mismo frame; desactivar antes de Destroy evita ghosting
- Previene artefactos visuales en headshot batch (dos criaturas superpuestas)
- Override de rim es toggle (bool rimOverride) sin estado gradual — on/off nítido

## Notas S61

- Assemble() desactiva hijos viejos inmediatamente, destroye diferido
- Fotomatón renderiza en mismo frame; desactivar antes de Destroy evita ghosting
- Previene artefactos visuales en headshot batch

## Notas S107

- MonchiTurntable crea booths con MonchiVisualizer instanciado dinámicamente
- SetBank() y SetFurDatabase() llamados desde MonchiTurntable.Awake()
- Assemble() llamado desde MonchiTurntable.Show() cuando preview debe renderizar

## Notas S110

- MonchiTeamRim (presentador) monitorea agent.Team y llama SetRimOverride/ClearRimOverride
- Rim override solo afecta rivales (ExpeditionTeam.Rival)
- Color/power/insideMask editables en MonchiTeamRim para tuning
- ApplyLook() recalcula todo el tintado; no es performance-critical (una vez per assembly o team change)

## Vinculado a

- [[Index/10 - Visualization]]
- [[Index/23 - Arena Sandbox y Expedicion]]
- [[MonchiVisualBankSO]], [[ColorGenetics]]
- [[MonchiTeamRim]] — (S110 NUEVO) llamador de SetRimOverride/ClearRimOverride

## Conexiones

**Entrada:**
- Assemble/RefreshLook: CreatureDNA
- SetMood: MonchiMoodDriver
- SetRimOverride/ClearRimOverride: MonchiTeamRim (S110)

**Salida:**
- Modelo visual world-space
- Material Face swapped por mood
- MPB de rim light (genético u override)

