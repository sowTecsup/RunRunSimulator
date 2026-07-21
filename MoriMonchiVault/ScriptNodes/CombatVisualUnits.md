---
tags: [script, combat, visualizer, composition]
---

# CombatVisualUnits.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualUnits.cs`

**Responsabilidad:** Colaborador de `CombatVisualizerService` (composición, regla 11) — spawn/lookup/lifecycle de las unidades del replay 3v3. Dueño de mapeo de DNAs → anchors por fila (Front0/Front1, Mid0/Mid1/Mid2, Back0/Back1 convención hex 2-3-2), instantiación de modelos visuales, binding de barras UI con stats del snapshot, lifecycle de despawn. Plain data (DTO `CombatVisualUnit`) + stateless operations.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Team(side)` | `IReadOnlyList<CombatVisualUnit>` | Retorna la lista inmutable de unidades de un lado (A o B) |
| `Get(side, index)` | `CombatVisualUnit` | Busca la unidad en un lado/índice exacto; null si no existe |
| `Spawn(side, dnas, snapshots, board, prefab)` | `void` | Instancia MonchiVisualizer para un equipo, resuelve anchors por fila, arma instancias, binds de barras radiales, crea vcams; VCam capturada en unit.VCam |
| `DespawnAll()` | `void` | Destruye todos los GameObjects de ambos equipos y limpia listas |
| `SetActive(unit, active)` | `void` | Muestra/oculta el GameObject de una unidad (sin destruir) |
| `TransformOf(side, index)` | `Transform` | Retorna el transform de la unidad o su anchor (fallback) |
| `PosOf(side, index)` | `Vector3` | Retorna la posición mundial de la unidad o su anchor |

## Estructura: CombatVisualUnit (DTO)

```csharp
public class CombatVisualUnit
{
    public CombatVisualSide Side;                                      // A o B
    public int Index;                                                  // 0..2 dentro del equipo
    public CreatureDNA Dna;                                            // datos genéticos
    public CombatFighterSnapshot Snapshot;                             // stats post-equipo del momento
    public float MaxHp;                                                // snapshot.MaxHp
    public float ShownHp;                                              // para animar cambios de HP
    public MonchiVisualizer Instance;              // S58: nuevo tipo    // prefab instanciado (Suriyun rig)
    public CombatRadialHealthBar Bar;              // S58: nuevo tipo    // anillo radial world-space
    public MonchiAnimationDriver Anim;             // S58: nuevo tipo    // driver de animación Suriyun
    public Transform Anchor;                                           // nodo del board donde vive
    public Unity.Cinemachine.CinemachineCamera VCam;                   // vcam child para cortes de cámara
}
```

## Clase CombatVisualUnits

```csharp
private readonly List<CombatVisualUnit> teamA = new List<CombatVisualUnit>();
private readonly List<CombatVisualUnit> teamB = new List<CombatVisualUnit>();

private static readonly string[] FrontNames = { "Front0", "Front1" };
private static readonly string[] MidNames   = { "Mid0", "Mid1", "Mid2" };
private static readonly string[] BackNames  = { "Back0", "Back1" };
```

**Convención de anchors:** El board contiene 7 hijos nombrados por fila:
- `Front0`, `Front1` (frontal)
- `Mid0`, `Mid1`, `Mid2` (central)
- `Back0`, `Back1` (trasera)

El método privado `ResolveAnchor(board, row, ref frontUsed, ref midUsed, ref backUsed)` itera sobre los nombres ordenados por uso y devuelve el Transform del anchor libre, incrementando el contador para la siguiente unidad de esa fila.

## Flujo de Spawn (S58 — Migración Suriyun; S59 — Identidad de barra)

**Entrada:**
- `side` — `CombatVisualSide.A` o `.B`
- `dnas` — lista de CreatureDNA del equipo (1..3)
- `snapshots` — lista de `CombatFighterSnapshot` (índice 1:1 con dnas)
- `board` — Transform del BoardA o BoardB (contiene los anchors hijos)
- `prefab` — MonchiVisualizer a instanciar

**Proceso:**
1. Borra el equipo anterior (teamA o teamB según side)
2. Itera por cada DNA con su snapshot correspondiente
3. Resuelve el anchor por `snapshot.Row` (0=Front, 1=Mid, 2=Back)
4. Instancia el prefab como hijo del anchor
5. Assemble visual vía `MonchiVisualizer.SetBank(GameManager.MonchiVisualBank) + SetFurDatabase() + Assemble()`
6. Captura referencias:
   - `Instance` — el MonchiVisualizer instanciado
   - `Anim` — `inst.GetComponent<MonchiAnimationDriver>()` (nuevo tipo S58)
   - `Anchor` — el Transform del anchor
7. **S58 NUEVO:** Crea GameObject hijo "RadialBar_{side}{i}" con `CombatRadialHealthBar` componente
   - Posición: localPosition (0, 0.05, 0)
   - **S59 CAMBIO:** Bind con identidad: `unit.Bar?.Bind(side, i)` (antes: Bind sin parámetros)
   - SetFacingTarget: `unit.Bar.SetFacingTarget(inst.transform)` para orientar cierre del anillo
   - Destruye barra en `DespawnTeam()`: `Object.Destroy(unit.Bar.gameObject);`
8. **S42+:** Crea GameObject hijo "VCam_{side}{i}" con CinemachineCamera (priority 0) + CinemachineRotationComposer
9. Agrega `CombatVisualUnit` a la lista del equipo

**Resultado:** El equipo está vivo en pantalla, cada unidad en su anchor exacto, barras radiales inicializadas con identidad, vcams listos para corte de cámara.

## S58 Cambios (Migración Suriyun + Retiro Pipeline Visual Legacy)

**Tipos retipados:**
- `Instance: MoriMonchiVisualizer` → `Instance: MonchiVisualizer` (nuevo rig Suriyun)
- `Anim: MoriMonchiProceduralAnimator` → `Anim: MonchiAnimationDriver` (contrato unificado)
- `Bar: MoriMonchiCombatVisualizerUITK` → `Bar: CombatRadialHealthBar` (mundo-espacio, no UI legacy)
- **Eliminado:** campo `Hooks` (ya no existe `MoriMonchiCombatVisualizer`)

**Impacto:**
- Spawn ahora usa `GameManager.MonchiVisualBank` (SO Suriyun) en lugar de `PartVisualBankSO` (legacy, eliminado)
- AnimationDriver expone PlayAttack(target, onImpact, onDone), PlayHit(intensity), PlayDefeat, PlayVictory, PlayIdle, PlayBuff(buffName) — contrato cambiado
- Barra radial genera sprites por código (anillos fill Radial360), se orienta por yaw del MM, no tiene UI legacy
- DespawnTeam también destruye barras radiales ahora

**Compatibilidad:** Ya no hay MoriMonchiVisualizer ni MoriMonchiProceduralAnimator en el codebase.

## S59 Cambios (Identidad de barra + hover externo)

**Bind con identidad:**
- Línea 80: `unit.Bar?.Bind(side, i)` — ahora pasa side/index como parámetros
- CombatRadialHealthBar.Bind() requiere identidad para funcionar correctamente
- Sin identidad, la barra no renderiza ni responde a eventos OnUnitHover

**Impacto en hover:**
- La barra ahora es únicamente "dueña" de su identidad (side, index)
- CombatVisualEvents.OnUnitHover(side, index, hover) → solo la barra con esa identidad responde
- Flujo: CombatOrderBarUITK emite → CombatRadialHealthBar.HandleUnitHover() → externalHover setea → UpdateVisibility() fade

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualizerService]] — orquestador que usa esta clase para spawn/lifecycle
- [[CombatRadialHealthBar]] — **S58** barra radial nuevas, API SetHp/SetShield/SetActiveTurn/SetTargeted; **S59** Bind(side, index) identidad
- [[MonchiAnimationDriver]] — **S58** contrato de animación
- [[CombatVisualEvents]] — **S59** OnUnitHover emitido por CombatOrderBarUITK

## Conexiones

- **Entrada:** `CombatVisualizerService.Play()` llama `Spawn(side, dnas, snapshots, board, prefab)` en ambos lados
- **Salida:** Popula lista de `CombatVisualUnit` que el service itera cada frame para animar
- **Refs:** `MonchiVisualizer` (prefab), `GameManager.MonchiVisualBank`, `GameManager.FurTypeDatabase`
- **Board refs:** BoardA/BoardB (Transform con 7 hijos anchor)
- **Anim:** `MonchiAnimationDriver.PlayAttack/PlayHit/PlayDefeat/PlayVictory/PlayIdle/PlayBuff/SetTimeScale`
- **Bar:** `CombatRadialHealthBar.Bind/SetHp/SetShield/SetActiveTurn/SetTargeted/SetFacingTarget`
- **VCam:** `CinemachineCamera`, `CinemachineRotationComposer` (vcams hijos)
- **Events:** `CombatVisualEvents.OnUnitHover` (suscriptor: CombatRadialHealthBar)
