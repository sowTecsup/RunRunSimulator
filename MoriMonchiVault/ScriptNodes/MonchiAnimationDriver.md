---
tags: [script, world, abstract, interface]
---

# MonchiAnimationDriver.cs

**Ruta:** `World/MonchiAnimationDriver.cs`

**Responsabilidad:** Contrato permanente (abstract MonoBehaviour) que define la interfaz de animación de combate para MoriMonchis. Cada modelo visual implementa su propio driver. Consumido por `CombatVisualizer`. Métodos: `IsBusy` (propiedad, indica si hay corrutina activa), `MoveTo(destination, onArrived)` (aproximación), `PlayAttack(targetPosition, onImpact, onFinished)` (ataque con callbacks en impacto y fin), `PlayHit(intensity)` (golpe recibido), `PlayBuff(onFinished)` (mejora aliado), `PlayDefeat()` (derrota), `PlayVictory()` (victoria), `PlayIdle()` (reposo/reset). **S59:** `SetTimeScale(float)` virtual (default no-op) — implementadores pueden escalar animaciones/tiempos según playback speed.

## Métodos Abstractos

| Método | Tipo | Descripción |
|--------|------|-------------|
| `IsBusy` | Propiedad (bool) | Verdadero si hay animación/movimiento activo |
| `MoveTo(Vector3 dest, Action onArrived)` | void | Camina a destino, dispara onArrived al llegar |
| `PlayAttack(Vector3 targetPos, Action onImpact, Action onFinished)` | void | Ataque direccionado; dispara onImpact al golpear, onFinished al terminar |
| `PlayHit(float intensity)` | void | Reacción a golpe recibido (squash, retroceso, etc.) |
| `PlayBuff(Action onFinished)` | void | Mejora aliado; dispara onFinished al terminar |
| `PlayDefeat()` | void | Animación de derrota (ragdoll, colapso, etc.) |
| `PlayVictory()` | void | Animación de victoria (saltos, celebración, etc.) |
| `PlayIdle()` | void | Reposo total; interrumpe todo y vuelve a postura neutra |

## Métodos Virtuales (S59+)

| Método | Tipo | Descripción |
|--------|------|-------------|
| `SetTimeScale(float value)` | **S59 NEW** virtual void | Escala animaciones/tiempos según playback speed (0.25–4). Default no-op; implementadores sobreescriben para escalar Animator.speed, corrutinas, etc. |

## Contrato para Implementadores

- Cada implementación (e.g., `DragonAnimationDriver`, futuros drivers para modelos definitivos) debe:
  - Manejar interrupciones vía `PlayIdle()` o nuevo `PlayX()` (interrupción limpia)
  - Mantener `IsBusy` sincronizado con corrutinas activas
  - Invocar callbacks en el momento exacto (onImpact durante golpe, onArrived al llegar)
  - Limpiarse en `OnDisable()` (desuscribir, parar corrutinas)
  - **S59:** Implementar `SetTimeScale(float)` para escalar animations (Animator.speed, Wait segundos vía Scaled())

## Cambios S59

**Método SetTimeScale(float) aditivo:**
- Línea 16: `public virtual void SetTimeScale(float value) { }`
- Virtual con implementación vacía (default no-op)
- Llamado por `CombatVisualizerService.PushTimeScale()` (línea 207–211) cuando cambia `playbackSpeed` o en `BeginRoutine()` con Speed inicial
- Propósito: Sincronizar animaciones con replay speed (0.25x lento, 4x rápido)

**Implementación en DragonAnimationDriver (S59):**
- Línea 51–57:
  ```csharp
  public override void SetTimeScale(float value)
  {
      timeScale = Mathf.Clamp(value, 0.25f, 4f);
      if (Anim != null)
          Anim.speed = timeScale;  // Escala todas las animaciones
  }
  ```
- DragonAnimationDriver también escala todas las esperas/velocidades via `Scaled()` (línea 45: `Scaled(seconds) => seconds / timeScale`)

**Flujo playback speed S59:**
1. UI usuario setea speed (e.g., 2x)
2. CombatVisualizerService.SetSpeed() → playbackSpeed = 2, PushTimeScale() llamado
3. PushTimeScale() itera todos los animadores → Unit.Anim.SetTimeScale(Speed)
4. DragonAnimationDriver.SetTimeScale(2) → Anim.speed = 2, timeScale = 2
5. Todas las WaitForSeconds usan Scaled(): espera original se divide por timeScale
6. Resultado: ataque de 0.5s @ 2x speed = 0.25s real

## Notas

- **Contrato permanente:** No cambiará la firma en futuras sesiones; nuevas capacidades se agregan como métodos nuevos sin quebrar implementaciones viejas.
- **Consumidor:** `CombatVisualizer` (sistema de combate) obtiene la ref vía `GetComponentInChildren<MonchiAnimationDriver>()` en el root del MoriMochi.
- **Implementación de referencia:** `DragonAnimationDriver` (S58+) es implementación actual con SetTimeScale override.
- **Namespace:** `MoriMonchiSimulator` (core)
- **S59:** SetTimeScale es aditivo — código viejo sin override sigue funcionando (no-op por defecto)

## Vinculado a

- [[Index/03 - Combat]] — CombatVisualizer consume este contrato
- [[DragonAnimationDriver]] — **S59** implementación actual, SetTimeScale override
- [[CombatVisualizerService]] — **S59** llama PushTimeScale() para propagar speed

## Conexiones

**Consumido por:**
- `CombatVisualizerService` → obtiene driver y lo dispara en secuencias de ataque/defensa/resultado; **S59** propaga playback speed via SetTimeScale

**Implementado por:**
- `DragonAnimationDriver` (S58+, implementación actual)
- Futuros drivers para modelos definitivos
