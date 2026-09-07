---
tags: [script, world, expedition, recolectable]
---

# MaterialPickup.cs

**Ruta:** `World/Expedition/MaterialPickup.cs`

**Responsabilidad:** Recolectable de expedición: cristal mineral con valor entero, clasificación de origen (lode vs drop), y radio. Requiere componente `Perceivable` del mismo GO (para que el agente lo vea). Expone interfaz: `Value`, `Remaining`, `Taken`, `IsLode`, `IsDrop`, `Radius`, `TryMineUnit()`, `ApproachPoint()`. S98: `disableDelay`, `onTaken`. S99: radio perezoso, `ApproachPoint()`. S104: `SetLode()`, `SetDrop()` para clasificar origen (afecta velocidad de minado en ExpeditionRulesSO).

**Campos Serializados:**
- `value` (int, min 1) — puntos que otorga al minarse completamente. Seteable vía `SetValue()`.
- `disableDelay` (float, min 0) — segundos antes de desactivar tras recolección completa. Si ≤ 0, inmediato.
- `onTaken` (UnityEvent) — dispara al recolectar completamente
- `standoffRadius` (float, min 0) — override de radio: si > 0, usa este; si = 0, calcula lazy desde renderer

**Propiedades Públicas:**
- `int Value { get; }` — valor otorgado
- `int Remaining { get; private set; }` — unidades pendientes
- `bool Taken { get; }` — si ya fue recolectado completamente
- `bool IsLode { get; private set; }` — clasificación lode (S104 NUEVO)
- `bool IsDrop { get; private set; }` — clasificación drop caído (S104 NUEVO)
- `float Radius { get; }` — radio de contacto (cacheado perezoso)

**Métodos Públicos:**
- `void TryMineUnit() → bool` — recolecta unidad. Si Taken, retorna false. Sino: decrementa Remaining, emite onTaken si completo, inicia desactivación
- `void ApproachPoint(Vector3 from, float margin) → Vector3` — punto de llegada en borde del mineral
- `void SetValue(int newValue)` — setter interno (ArenaSandbox.SetupMinerals)
- `void SetLode(bool lode)` — clasifica como lode (S104 NUEVO). Consulta: ExpeditionRulesSO.LodeMiningSecondsPerUnit si true, else MiningSecondsPerUnit
- `void SetDrop()` — marca como drop (S104 NUEVO). Usada por AgentGatherer.Drop() para material soltado

**Ciclo de Vida:**
1. Instancia (ArenaSandbox.SetupMinerals)
2. SetValue() + SetLode() / SetDrop()
3. TryMineUnit() loop hasta Remaining ≤ 0
4. Desactiva con delay opcional

**Clasificación (S104):**
- `IsLode=true` — mineral central de la sala, minado más lentamente (LodeMiningSecondsPerUnit)
- `IsDrop=true` — material caído por golpeo, pickup solo (no minado)
- Default (false,false) — veta chica en esquinas

**Invariantes:**
- Perceivable requerido: `[RequireComponent]` asegura registro
- Remaining decrementa 1 por TryMineUnit(); si Value=5, toma 5 llamadas
- Desactivación = desregistro del registry; agentes nunca vuelven
- Radio perezoso: calculado una vez; sandbox escala post-instancia

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[Perceivable]], [[ArenaSandbox]], [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[TeamBlackboard]], [[ExpeditionRulesSO]]
