---
tags: [script, world, abstract, interface]
---

# MonchiAnimationDriver.cs

**Ruta:** `World/MonchiAnimationDriver.cs`

**Responsabilidad:** Contrato permanente (abstract MonoBehaviour) que define la interfaz de animación de combate para MoriMonchis. Cada modelo visual implementa su propio driver. Consumido por `CombatVisualizer`. Métodos: `IsBusy` (propiedad, indica si hay corrutina activa), `MoveTo(destination, onArrived)` (aproximación), `PlayAttack(targetPosition, onImpact, onFinished)` (ataque con callbacks en impacto y fin), `PlayHit(intensity)` (golpe recibido), `PlayBuff(onFinished)` (mejora aliado), `PlayDefeat()` (derrota), `PlayVictory()` (victoria), `PlayIdle()` (reposo/reset).

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

## Contrato para Implementadores

- Cada implementación (e.g., `SpiderAnimationDriver`, futuros drivers para modelos definitivos) debe:
  - Manejar interrupciones vía `PlayIdle()` o nuevo `PlayX()` (interrupción limpia)
  - Mantener `IsBusy` sincronizado con corrutinas activas
  - Invocar callbacks en el momento exacto (onImpact durante golpe, onArrived al llegar)
  - Limpiarse en `OnDisable()` (desuscribir, parar corrutinas)

## Notas

- **Contrato permanente:** No cambiará la firma en futuras sesiones; nuevas capacidades se agregan como métodos nuevos sin quebrar implementaciones viejas.
- **Consumidor:** `CombatVisualizer` (sistema de combate) obtiene la ref vía `GetComponentInChildren<MonchiAnimationDriver>()` en el root del MoriMochi.
- **Implementación de referencia:** `SpiderAnimationDriver` es prototipo procedural descartado; nuevos drivers para modelos finales heredarán de esta clase.
- **Namespace:** `MoriMonchiSimulator` (core)

## Vinculado a

- [[Index/03 - Combat]] — CombatVisualizer consume este contrato
- [[SpiderAnimationDriver]] — implementación prototipo (deuda técnica)

## Conexiones

**Consumido por:**
- `CombatVisualizer` → obtiene driver y lo dispara en secuencias de ataque/defensa/resultado

**Implementado por:**
- `SpiderAnimationDriver` (prototipo, descartado)
- Futuros drivers para modelos definitivos
