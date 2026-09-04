---
tags: [script, world, perception, registry]
---

# Perceivable.cs

**Ruta:** `World/AI/Perceivable.cs`

**Responsabilidad:** Marca cualquier entidad del mundo (jugador, MoriMochi, cliente, prop, mineral) como perceptible por otros agentes. Auto-registra/desregistra con PerceivableRegistry en OnEnable/OnDisable (patrón NeedStationRegistry). Almacena el tipo (PerceivableKind), etiquetas opcionales, bando expedición (**S99 NUEVO**: ExpeditionTeam), y una referencia al MoriMochiAgent propietario (null para entidades no-Monchi). El struct Percept transporta una sola observación (fuente, tipo, distancia, afinidad, **S99**: team) — valor puro, nunca retenido.

## Struct Percept (S99 ACTUALIZADO)

```csharp
public struct Percept
{
    public Perceivable Source;
    public PerceivableKind Kind;
    public float SqrDistance;
    public float Affinity;
    public ExpeditionTeam Team;  // S99 NUEVO: bando del percepto
}
```

## Clase Perceivable

**Campos:**
- `kind` — PerceivableKind (Player/Monchi/Customer/Prop/**Material** S97)
- `tags` — List<string> opcional para categorización temática
- `team` — **S99 NUEVO** ExpeditionTeam (None/Player/Rival); usado para filtrado de rivales en expedición
- `Monchi` — referencia al MoriMochiAgent propietario (null si es jugador/cliente/prop/mineral)

**Métodos:**
- `Position → Vector3` — posición en tiempo real
- `SetTeam(ExpeditionTeam value)` — **S99 NUEVO** setter para cambiar bando en runtime (usado por `ArenaSandbox` al spawnear agentes)

**Cambios S99:**
- Nuevo campo serializado `team` (default ExpeditionTeam.None)
- Nuevo método público `SetTeam()` para mutar bando (llamado desde `ArenaSandbox.SpawnMonchi()`)
- Struct `Percept` ahora incluye campo `Team` (poblado al crear Percept en `AgentSenses`)

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

## Invariantes S99

- **Auto-registro:** patrón Registry sin callback explícito; `OnEnable/OnDisable` vinculados automáticamente.
- **Team propagación:** `SetTeam()` permite cambiar bando después de creation (usado en spawn de Arena).
- **Percept immutable:** struct de solo lectura; los datos valen solo en el frame en que se crean (no cachear Percepts).
- **Monchi resolution:** `Awake()` busca en el mismo GO y en parent (útil si Perceivable está en child).
- **Material s Kind:** `PerceivableKind.Material` para minerales (sin Monchi ni Team relevante, salvo para rival detection).
- **Player/Client without Team:** generalmente `ExpeditionTeam.None` (neutrales, no compiten en expedición).

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[PerceivableRegistry]] (registro global automático en OnEnable/OnDisable)
- [[AgentSenses]] (itera registry, crea Percepts, popula `Team` field)
- [[AgentSocial]] (lector de Percepts, filtra por Kind/Affinity)
- [[AgentExpedition]] (lector de Percepts, filtra por Kind=Material, **S99:** usa Team)
- [[MoriMochiAgent]] (propietario, se cita en `Monchi`)
- [[MaterialPickup]] (usa Perceivable con Kind=Material, **S97**)
- [[ArenaSandbox]] (llama `SetTeam()` al spawnear **S99**)
- **S99:** [[ExpeditionTeam]], [[WorldEnums]]
