---
tags: [script, world, visual, presentation, feedback]
---

# MonchiMiningFeedback.cs

**Ruta:** `World/Creatures/MonchiMiningFeedback.cs`

**Responsabilidad:** Presentador que activa/desactiva efectos visuales de minería en tiempo real. Lee `agent.Intent == CreatureIntent.Taking` en `LateUpdate()` y dispara o detiene un `MMF_Player` serializado. Vinculado en el prefab del agente; el hijo `Feedbacks/OnMining` contiene `MMF_Particles` sobre `FX_MiningDust` que toma muestras de un `FX_DustGround` en loop anidado.

## Responsabilidad Específica

- Monitorea cambio de intención de minería (Taking ↔ no Taking)
- Al entrar en Taking: `onMining.PlayFeedbacks()`
- Al salir de Taking: `onMining.StopFeedbacks()`
- OnDisable: limpia (stop si estaba activo)
- Sin lógica de juego; solo presenta

## Campos Públicos/Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `agent` | `MoriMochiAgent` [Required] | Ref al agente de la criatura |
| `onMining` | `MMF_Player` [Required] | Player de feedback (hijo `Feedbacks/OnMining`) |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `mining` | `bool` | Estado anterior (cache) |

## Métodos

- `LateUpdate()` — detecta transición de intención, dispara/detiene feedback
- `OnDisable()` — limpia (stop + reset mining flag)

## Flujo

1. `LateUpdate()` calcula `now = agent.Intent == CreatureIntent.Taking`
2. Si `now != mining`:
   - Actualiza `mining = now`
   - Si onMining no nulo: llama `PlayFeedbacks()` (now=true) o `StopFeedbacks()` (now=false)
3. Al desactivar el GameObject: `OnDisable()` para garantizar que no siga sonando

## Integración

- Hijo del GameObject del agente (MoriMonchiController)
- Prefab del agente incluye child `Feedbacks/OnMining` con `MMF_Player` + `MMF_Particles`
- Se activa pasivamente cuando criatura cambia intent a Taking (excavar/recolectar)
- Se desactiva cuando intent sale de Taking

## Invariantes

- Sin comprobaciones de IA ni lógica; puro presentation
- Juice (Feel) vive en el prefab (MMF_Player); C# solo toca el play/stop
- OnDisable garantiza limpieza (no deja loops sonando)

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[MoriMochiAgent]] — lee Intent
- [[CreatureIntent]] — valores de enum

## Conexiones

**Entrada:**
- `MoriMochiAgent.Intent`

**Salida:**
- Efecto visual MMFeedbacks (FX_MiningDust en loop)

