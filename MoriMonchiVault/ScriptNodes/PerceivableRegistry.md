---
tags: [script, world, perception, registry]
---

# PerceivableRegistry.cs

**Ruta:** `World/AI/PerceivableRegistry.cs`

**Responsabilidad:** Índice estático en tiempo de ejecución de todas las Perceivable activas en la escena. Auto-limpieza via OnEnable/OnDisable (mismo patrón que NeedStationRegistry, no requiere wireo manual). Estático (sin MonoBehaviour) porque los agentes viven en el mismo World domain que las perceivables — es una consulta intra-dominio legítima, no un singleton cross-system. AgentSenses lo consulta cada escaneo de percepción. **S109:** QueryInRadius respeta NoticeRadius dinámico de perceivables para expand radio efectivo.

**Métodos estáticos:**
- `Register(Perceivable) → void` — agrega si no está duplicado
- `Unregister(Perceivable) → void` — remueve
- `Count → int` — cantidad de perceivables registradas
- **`QueryInRadius(Vector3 from, float radius, Perceivable exclude, List<Perceivable> results) → void`** (S109 ACTUALIZADO) — non-alloc: limpia results y rellena con todas las perceivables dentro del radio efectivo (excepto exclude), sin ordenar:
  - Para cada perceivable p:
    - Si p == null o p == exclude: skip
    - Calcula sqrDistance = (p.Position - from)²
    - Calcula límite: `limit = p.NoticeRadius > radius ? p.NoticeRadius² : radius²` (S109)
    - Si sqrDistance ≤ limit: agrega a results
  - Ejemplo S109: si radius=8m y perceivable tiene NoticeRadius=12m (habilidad pasiva), límite efectivo = 144 (12²)

**Notas:**
- QueryInRadius devuelve la lista sin ordenar; AgentSenses la ordena por distancia
- La lista puede contener nulls temporales (destrucciones raciales); QueryInRadius los salta
- Usado exclusivamente por AgentSenses durante su Tick() throttled
- **S109:** La expansión de radio es dinámica — si NoticeRadius cambia (ej: equipo re-armado), query lo respeta
- **Fórmula S109:** máxima de radios cuadrados, no suma. Percept con NoticeRadius > query radius expande el límite de búsqueda

**Integración:**

- Llamado desde AgentSenses.Tick() con `PerceptionRadius` como parámetro
- Resultado filtrado después por VisionProfile.CanSense() (cono de visión)
- NoticeRadius leído en cada query (lazy, no cacheado)

**S109 Cambios:**

- Método QueryInRadius() actualizado: ahora respeta `p.NoticeRadius` del percepto
- Límite de búsqueda dinámico: `max(radius², NoticeRadius²)` en lugar de fijo `radius²`
- Permite que habilidades pasivas que sumen VisibleFrom expandan el rango de detección
- Exemplo: criatura con "Aura Visible" (VisibleFrom +5m) se detecta 5m más lejos de lo normal

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[Perceivable]], [[AgentSenses]], [[ExpeditionStats]]
