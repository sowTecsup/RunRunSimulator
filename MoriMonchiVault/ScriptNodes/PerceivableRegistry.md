---
tags: [script, world, perception, registry]
---

# PerceivableRegistry.cs

**Ruta:** `World/AI/PerceivableRegistry.cs`

**Responsabilidad:** Índice estático en tiempo de ejecución de todas las Perceivable activas en la escena. Auto-limpieza via OnEnable/OnDisable (mismo patrón que NeedStationRegistry, no requiere wireo manual). Estático (sin MonoBehaviour) porque los agentes viven en el mismo World domain que las perceivables — es una consulta intra-dominio legítima, no un singleton cross-system. AgentSenses lo consulta cada escaneo de percepción.

**Métodos estáticos:**
- `Register(Perceivable) → void` — agrega si no está duplicado
- `Unregister(Perceivable) → void` — remueve
- `Count → int` — cantidad de perceivables registradas
- `QueryInRadius(Vector3 from, float radius, Perceivable exclude, List<Perceivable> results) → void` — non-alloc: limpia results y rellena con todas las perceivables dentro del radio (excepto exclude), sin ordenar

**Notas:**
- QueryInRadius devuelve la lista sin ordenar; AgentSenses la ordena por distancia
- La lista puede contener nulls temporales (destrucciones raciales); QueryInRadius los salta
- Usado exclusivamente por AgentSenses durante su Tick() throttled

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[Perceivable]], [[AgentSenses]]
