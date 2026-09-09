---
tags: [script, world, perception, registry]
---

# Perceivable.cs

**Ruta:** `World/AI/Perceivable.cs`

**Responsabilidad:** Marca cualquier entidad del mundo (jugador, MoriMochi, cliente, prop, mineral) como perceptible por otros agentes. Auto-registra/desregistra con PerceivableRegistry en OnEnable/OnDisable (patrón NeedStationRegistry). Almacena el tipo (PerceivableKind), etiquetas opcionales, bando expedición (**S99**: ExpeditionTeam), y una referencia al MoriMochiAgent propietario (null para entidades no-Monchi). **S109:** Expone NoticeRadius como propiedad dinámica que lee Stats.VisibleFrom (visibilidad por habilidades pasivas). El struct Percept transporta una sola observación (fuente, tipo, distancia, afinidad, **S99**: team) — valor puro, nunca retenido.

## Struct Percept (S99 ACTUALIZADO)

```csharp
public struct Percept
{
    public Perceivable Source;
    public PerceivableKind Kind;
    public float SqrDistance;
    public float Affinity;
    public ExpeditionTeam Team;  // S99: bando del percepto
}
```

## Clase Perceivable

**Campos Serializados:**
- `kind` — PerceivableKind (Player/Monchi/Customer/Prop/**Material** S97)
- `tags` — List<string> opcional para categorización temática
- `team` — **S99** ExpeditionTeam (None/Player/Rival); usado para filtrado de rivales en expedición

**Propiedades Públicas:**
- `PerceivableKind Kind { get; }` — tipo del percepto
- `IReadOnlyList<string> Tags { get; }` — tags de solo lectura
- `ExpeditionTeam Team { get; }` — equipo del percepto
- `void SetTeam(ExpeditionTeam value)` — **S99** setter para cambiar bando en runtime (usado por ArenaSandbox al spawnear agentes)
- `Vector3 Position { get; }` — posición en tiempo real
- **`float NoticeRadius { get; }`** (S109 NUEVO) — radio de visibilidad del agente:
  - Si Monchi != null (tiene agente): retorna `Monchi.Stats.VisibleFrom` (dinámico, desde habilidades pasivas)
  - Si no tiene agente: retorna 0f (invisible, ej: materiales, muebles)
  - Ejemplo: si habilidad pasiva suma +4m a VisibleFrom, NoticeRadius = 4m
- `MoriMochiAgent Monchi { get; private set; }` — referencia al MoriMochiAgent si existe

**Métodos Privados:**

- `Awake()` — resuelve Monchi lazy: busca en self o parent (MoriMochiAgent u otro componente)
- `OnEnable()` — registra en PerceivableRegistry.Register(this)
- `OnDisable()` — desregistra en PerceivableRegistry.Unregister(this)

## Ciclo de Vida

```csharp
Awake():
  Monchi = GetComponent<MoriMochiAgent>() || GetComponentInParent<MoriMochiAgent>()
  → Null para entidades no-Monchi (jugador, cliente, mineral)

OnEnable():
  PerceivableRegistry.Register(this)
  → Se agrega a la lista global de perceptibles

OnDisable():
  PerceivableRegistry.Unregister(this)
  → Se remueve de la lista global (ej: gameObject.SetActive(false))
```

## Invariantes S99 + S109

- **Auto-registro:** patrón Registry sin callback explícito; `OnEnable/OnDisable` vinculados automáticamente.
- **Team propagación:** `SetTeam()` permite cambiar bando después de creation (usado en spawn de Arena).
- **Percept immutable:** struct de solo lectura; los datos valen solo en el frame en que se crean (no cachear Percepts).
- **Monchi resolution:** `Awake()` busca en el mismo GO y en parent (útil si Perceivable está en child).
- **Material sin Kind:** `PerceivableKind.Material` para minerales (sin Monchi ni Team relevante, salvo para rival detection).
- **Player/Client without Team:** generalmente `ExpeditionTeam.None` (neutrales, no compiten en expedición).
- **NoticeRadius dinámico (S109):** cada frame, leído desde Monchi.Stats.VisibleFrom:
  - Si Stats cambia (ej: habilidad pasiva activada), NoticeRadius se actualiza automáticamente
  - Usado por PerceivableRegistry.QueryInRadius() para expand queries
  - Usado por AgentSenses.Tick() para CanSense(Mathf.Max(perceptionRadius, NoticeRadius))
- **NoticeRadius = 0 si sin Monchi:** no es percepto de criatura, invisible dinámicamente

## S99 Cambios

- Nuevo campo serializado `team` (default ExpeditionTeam.None)
- Nuevo método público `SetTeam()` para mutar bando en runtime
- Struct `Percept` incluye campo `Team` (poblado en AgentSenses)

## S109 Cambios

- Propiedad `NoticeRadius` nueva (computed, no cacheado):
  - `Monchi != null ? Monchi.Stats.VisibleFrom : 0f`
- Stats resueltos por AgentAbilities.Bind(); NoticeRadius refleja eso instantáneamente
- PerceivableRegistry.QueryInRadius() y AgentSenses.Tick() usan NoticeRadius
- Permite que habilidades pasivas afecten visibilidad de rivales dinámicamente

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[PerceivableRegistry]] (registro global automático en OnEnable/OnDisable)
- [[AgentSenses]] (itera registry, crea Percepts, popula `Team` field, usa NoticeRadius)
- [[AgentSocial]] (lector de Percepts, filtra por Kind/Affinity)
- [[AgentExpedition]] (lector de Percepts, filtra por Kind=Material, usa Team)
- [[MoriMochiAgent]] (propietario, se cita en `Monchi`, expone Stats)
- [[MaterialPickup]] (usa Perceivable con Kind=Material)
- [[ArenaSandbox]] (llama `SetTeam()` al spawnear)
- [[ExpeditionTeam]], [[ExpeditionStats]]
