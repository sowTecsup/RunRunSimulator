---
tags: [script, creatures, presentation, visual]
---

# MonchiCarryDisplay.cs

**Ruta:** `World/Creatures/MonchiCarryDisplay.cs`

**Responsabilidad:** Presentador visual que instancia y posiciona cristales (carritos) bajo el MoriMochi mientras carga. Lee `agent.Carried` cada frame, pool de instancias por `SetActive`, posiciones animadas (stacked + jitter determinista). Sin lógica de gameplay; solo presenta el estado de carga visual. El pop de cristales es manejado por el prefab (MMF_Player AutoPlayOnEnable).

**Campos Serializados:**

- `agent` (MoriMochiAgent, Required) — referencia al agente para leer `Carried`
- `visualizer` (MonchiVisualizer, Required) — acceso a `visualizer.ModelRoot` para buscar hueso
- `crystalPrefab` (GameObject, Required, AssetsOnly) — prefab de cristal individual a instanciar
- `boneName` (string, default "Spine1") — nombre del hueso bajo ModelRoot donde anclar cristales
- `baseHeight` (float, default 0.45f) — altura base (Y world) del primer cristal relativo al hueso
- `stackStep` (float, default 0.2f) — altura incremental entre cristales (Δy por unidad)
- `jitter` (float, default 0.08f) — amplitud de jitter determinista (X, Z world)
- `maxShown` (int, Min 1, default 6) — máximo de cristales a renderizar (si Carried > maxShown, solo muestra los primeros)

**Campos Internos:**

- `anchor` (Transform) — hueso encontrado bajo ModelRoot; cached, re-resolved en LateUpdate si null
- `shown` (List<Transform>) — instancias activas/inactivas de cristales, reutilizadas

**Métodos:**

**LateUpdate() — cada frame:**
1. Calcula `carried = min(agent.Carried, maxShown)` (capped)
2. Re-resuelve `anchor` si es null (busca hueso bajo `visualizer.ModelRoot` por nombre)
3. Si anchor sigue null, desactiva todas las instancias y retorna
4. Mantiene pool: agranda `shown` si `carried > shown.Count`, reutiliza por SetActive
5. Para cada instancia i en shown:
   - Si i >= carried: desactiva (SetActive false)
   - Si i < carried:
     - Si no está activo: activa y resetea `localScale = one` (para anular pops de sesión anterior)
     - Posiciona en mundo: `anchor.position + up * (baseHeight + stackStep*i) + right*jx + forward*jz`
     - Jitter determinista: `jx = jitter * (i%2==0 ? 1:-1) * (i%3==0 ? 0.5:1)`, `jz = jitter * ((i/2)%2==0 ? -0.6:0.6)`
     - Rota: `Euler(0, 37*i, 0)` (giro Y cumulativo)

**OnDisable():**
- Desactiva todas las instancias (`SetActive false`)

**FindBone(Transform root, string name) — privado:**
- Recursivo DFS por árbol de hijos
- Retorna primer Transform cuyo nombre coincida o null

**Invariantes S109:**

- **Pool por SetActive:** sin Destroy/Instantiate cada frame; reutiliza GameObjects (performance)
- **Jitter determinista:** mismo i → mismo jitter (no random frame-to-frame)
- **Re-resolution de hueso:** si visualizer se cambia (ej. equipo re-armado), anchor = null fuerza búsqueda
- **Sin interacción visual con código:** pop es manejado por MMF_Player en el prefab con AutoPlayOnEnable
- **Escalado resetado:** `localScale = one` antes de reactivar para anular tweens previos
- **OnDisable limpia:** evita instancias "perdidas" visibles

**Integración:**

- Agregado como componente en el prefab del agente (mismo GO que MoriMochiAgent)
- Requiere refs serializadas (agent, visualizer, crystalPrefab)
- Funciona en arena sandbox y expedición (lee ctx.Carried internamente via agent.Carried)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[MonchiVisualizer]], [[AgentGatherer]], [[ExpeditionNav]]
