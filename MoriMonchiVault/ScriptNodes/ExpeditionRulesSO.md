---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** **Singleton por escena** (`Current` static) que centraliza tuning de expedición. Contiene lista polimórfica Odin de reglas `ExpeditionRuleBase`, knobs de navegación compartidos, beats de interacción, y **S102 NUEVO:** sección Visión (radio/ángulo/audición con skew por osadía). **S102 NUEVO:** `Activate(rules)` y `Deactivate(rules)` estáticos (llamados por ArenaSandbox.OnEnable/OnDisable). En tienda, `Current == null` → expedición desactiva. En Arena, `Current` apunta a asset `ExpeditionRules.asset`.

## Propiedades Estáticas

- `Current → ExpeditionRulesSO` — **S102 CAMBIO:** ya no se fija en OnEnable. Se activa/desactiva vía `Activate(rules)` / `Deactivate(rules)`.

## Métodos Estáticos S102 NUEVO

- `Activate(ExpeditionRulesSO rules) → void` — Current = rules
- `Deactivate(ExpeditionRulesSO rules) → void` — si Current == rules, Current = null

**Uso:**
```csharp
private void OnEnable()
{
    ExpeditionRulesSO.Activate(expeditionRules);
}

private void OnDisable()
{
    ExpeditionRulesSO.Deactivate(expeditionRules);
}
```

## Campos Públicos

**Lista de reglas:**
- `rules` (List<ExpeditionRuleBase>, IReadOnlyList pública) — lista polimórfica de reglas de evaluación.

**Tuning de navegación:**
- `ArriveDistance` (float, min 0.1, default 0.9)
- `RepathInterval` (float, min 0.05, default 0.5)
- `GiveUpSeconds` (float, min 1, default 12)
- `ApproachMargin` (float, min 0.05, default 0.15)

**Tuning de beats:**
- `NoticeSeconds` (float, min 0, default 0.5)
- `TakeSeconds` (float, min 0, default 1.2)
- `LoseSeconds` (float, min 0, default 1)

**Tuning de ocupación Gather:**
- `MiningSecondsPerUnit` (float, min 0.5, default 4)
- `CarryCapacity` (int, min 1, default 3)
- `DepositSeconds` (float, min 0, default 0.8)
- `DropPrefab` (MaterialPickup)
- `DropScale` (float, min 0.1, default 0.6)

**Tuning de ocupación Guard:**
- `GuardRadius` (float, min 1, default 4)

**Tuning de ocupación Break:**
- `HuntRepathInterval` (float, min 0.1, default 0.4)

**Tuning de ocupación Decoy:**
- `DecoyRange` (float, min 1, default 4.5)
- `TauntSeconds` (float, min 0, default 0.8)
- `DecoyFleeDistance` (float, min 1, default 8)
- `DecoyFleeSeconds` (float, min 0.5, default 5)
- `DecoyCooldown` (float, min 0, default 4)

**Tuning de Visión S102 NUEVO:**
- `VisionRadius` (float, min 1, default 8) — rango base de visión (escala con osadía vía BoldnessVisionSkew)
- `VisionDegrees` (float, min 30, max 360, default 160) — ángulo del cono (escala inversa a osadía)
- `NearSenseRadius` (float, min 0, default 2) — audición ciega (ignora cono)
- `BoldnessVisionSkew` (float, min 0, max 1, default 0.5) — multiplicador de skew por osadía
  - skew = BoldnessVisionSkew * (boldness - 0.5) * 2 (rango [-0.5, 0.5])
  - radius = VisionRadius * (1 + skew) — osados ven más lejos
  - degrees = clamp(VisionDegrees * (1 - skew), 30, 360) — osados ven más estrecho

## Métodos Públicos

- `PopulateDefaults()` — **Botón Odin**: inicializa `rules` e inserta `SeekMaterialRule()`.

## Invariantes S102

- **Singleton por escena:** `Current` refleja el asset activo (o null en tienda)
- **Activate/Deactivate:** llamados por componentes al entrar/salir de escena (ArenaSandbox.OnEnable/OnDisable)
- **Compartido:** navegación y beats consultados por `AgentExpedition.TickExpedition()`
- **Visión condicional:** si Current == null, no hay cono (fallback a ring omnidireccional)
- **Skew determinístico:** mismo DNA + rules → mismo radio/ángulo

## Conexiones

- [[VisionProfile]] — usa VisionRadius, VisionDegrees, NearSenseRadius, BoldnessVisionSkew en Resolve()
- [[AgentSenses]] — lee Current para filtrar Percepts con CanSense()
- [[MoriMochiAgent]] — fachada HasVisionCone, VisionRadius, VisionDegrees, NearSenseRadius
- [[ArenaCueOverlay]] — usa HasVisionCone/VisionRadius/VisionDegrees para dibujar cono
- [[AgentExpedition]] — lector de tuning (beats, carry, guard, break, decoy)
- [[ArenaSandbox]] — propietario, Activate/Deactivate en OnEnable/OnDisable

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
