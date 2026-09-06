---
tags: [script, world, ai]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta la vida de una criatura en el mundo. Compone ocho colaboradores: AgentContext (estado compartido), AgentBrain (máquina de estados), AgentPhysics (ragdoll), AgentConfinement (pens), AgentSenses (percepción throttled), AgentSocial (decisiones sociales), AgentExpedition (recolección), **S100:** AgentClash (combate). **S102 NUEVO:** expone fachada de visión (HasVisionCone, VisionRadius, VisionDegrees, NearSenseRadius) que delega a VisionProfile y ExpeditionRulesSO. Implementa IThrowable (agarrar/lanzar) e IInteractable (petting). Update() despachador de ticks por estado; FixedUpdate() para physics. **S55:** composición pura (sin partial).

## Máquina de Estados

| Estado | Responsable | Descripción |
|--------|-------------|-------------|
| `Idle` | AgentBrain | Esperando aleatorio |
| `Roaming` | AgentBrain | NavMesh autónomo |
| `Reacting` | AgentBrain | Persigue/huye del jugador |
| `Carried` | AgentPhysics | Agarrado por jugador |
| `Thrown` | AgentPhysics | Ragdoll en aire |
| `Recovering` | AgentPhysics | Get-up post-lanzamiento |
| `SeekingNeed` | AgentBrain | Navega a estación crítica |
| `UsingStation` | AgentBrain | Consume de estación |
| `Courting` | AgentConfinement | Danza de apareamiento |
| `Socializing` | AgentSocial | Acercándose, persiguiendo, durmiendo o peleando |
| `HandFeed` | AgentBrain | Aceptando comida de la mano |
| `Expedition` | AgentExpedition | Persiguiendo mineral recolectable |
| `Clashing` | **S100** AgentClash | Combatiendo |

## Propiedades (Fachada) S102

**Esenciales:**
- `DNA → CreatureDNA` — read-only
- `Intent → CreatureIntent` — intención actual (prioridad: Clashing → Socializing → Expedition → brain)
- `IsHeld`, `IsAirborne`, `IsPenned`, `IsForSale`, `IsRecovering` — estados

**Percepciones y objetivos:**
- `Percepts → IReadOnlyList<Percept>` — pobladas por AgentSenses (con filtro cono S102)
- `CollectedMaterial → int` — acumulador
- `Carried → int` — material siendo cargado
- `MiningProgress → float` — progreso (0–1) si Intent == Taking
- `ExpeditionTarget → Transform` — mineral/salida/puesto/presa
- `SocialPartner → MoriMochiAgent` — agente de socialización o null
- `Team → ExpeditionTeam` — None/Player/Rival

**Visión S102 NUEVO:**
- `HasVisionCone → bool` — si agente tiene cono de visión (ExpeditionRulesSO.Current != null)
- `VisionRadius → float` — rango de visión (resuelto por VisionProfile.Resolve con skew por boldness)
- `VisionDegrees → float` — ángulo del cono (resuelto por VisionProfile.Resolve)
- `NearSenseRadius → float` — audición ciega (NearSenseRadius de ExpeditionRulesSO)

**Ocupación (S101):**
- `Occupation → Occupation` — estrategia asignada (None/Gather/Guard/Break/Decoy/Explore)
- `SetOccupation(Occupation occupation)` → void
- `SetHomeExit(ExitZone exit)` → void
- `SetGuardPost(Transform post)` → void

**Clash S100:**
- `ClashTarget → MoriMochiAgent` — rival actual o null
- `ClashGesture → string` — gesto a disparar
- `IsClashTargetable → bool` — puede ser golpeado
- `ForceClash(ClashMoveSO move, MoriMochiAgent rival) → bool` — dev tool

## Métodos de Visión S102

**Fachada pública:**
```csharp
public bool HasVisionCone => ExpeditionRulesSO.Current != null;

public float VisionRadius
{
    get
    {
        if (!HasVisionCone || DNA == null) return SocialTuningSO.Current?.PerceptionRadius ?? 0f;
        VisionProfile.Resolve(DNA, ExpeditionRulesSO.Current, out float radius, out _, out _);
        return radius;
    }
}

public float VisionDegrees
{
    get
    {
        if (!HasVisionCone || DNA == null) return 360f;
        VisionProfile.Resolve(DNA, ExpeditionRulesSO.Current, out _, out float degrees, out _);
        return degrees;
    }
}

public float NearSenseRadius
{
    get
    {
        if (!HasVisionCone) return 0f;
        VisionProfile.Resolve(DNA, ExpeditionRulesSO.Current, out _, out _, out float nearRadius);
        return nearRadius;
    }
}
```

**Uso:**
- ArenaCueOverlay.DrawPerception() → if (agent.HasVisionCone) DrawVisionCone() else DashedRing()
- ArenaCueOverlay.LateUpdate() → perceptionRadius = agent.HasVisionCone ? agent.VisionRadius : global
- AgentSenses.Tick() → si ExpeditionRulesSO.Current: filtra Percepts por CanSense()

## Ciclo de Actualización S102

```csharp
Update():
  brain.TickAlways(dt)     // decay necesidades
  senses.Tick()            // scan perceivables (filtro cono S102)
  
  switch (ctx.State):
    Idle/Roaming → if (!clash.TryEngage()) {if (!expedition.TryEngage()) social.TryEngage()}
    Thrown       → clash.TickAirborne(); physics.TickThrown()
    Expedition   → expedition.TickExpedition()
    Clashing     → clash.TickClashing()
    Socializing  → social.TickSocializing()
```

## Invariantes S102

- **HasVisionCone es sensor:** lee ExpeditionRulesSO.Current != null
- **VisionProfile.Resolve delegado:** calcula radius/degrees/nearRadius con skew por boldness
- **Osadía skew:** osados ven más lejos pero más estrecho; tímidos ven menos lejos pero más amplio
- **Audición aparte:** NearSenseRadius ignora conos (toque ciego)
- **Fachada inmutable:** las propiedades no son settables desde fuera

## Conexiones

- [[VisionProfile]] — Resolve() para calcular parámetros
- [[ExpeditionRulesSO]] — Current (activado en ArenaSandbox.OnEnable)
- [[SocialTuningSO]] — fallback PerceptionRadius si !HasVisionCone
- [[AgentSenses]] — usa VisionProfile.CanSense para filtrar Percepts
- [[ArenaCueOverlay]] — usa HasVisionCone/VisionRadius/VisionDegrees/NearSenseRadius para dibujo
- **Componentes internos:** AgentContext, AgentBrain, AgentPhysics, AgentConfinement, AgentSenses, AgentSocial, AgentExpedition, AgentClash

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
