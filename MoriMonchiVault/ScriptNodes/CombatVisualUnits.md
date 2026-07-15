---
tags: [script, combat, visualizer, composition]
---

# CombatVisualUnits.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualUnits.cs`

**Responsabilidad:** Colaborador de `CombatVisualizerService` (composición, regla 11) — spawn/lookup/lifecycle de las unidades del replay 3v3. Dueño de mapeo de DNAs → anchors por fila (Front0/Front1, Mid0/Mid1/Mid2, Back0/Back1 convención hex 2-3-2), instantiación de modelos visuales, binding de barras UI con stats del snapshot, lifecycle de despawn. Plain data (DTO `CombatVisualUnit`) + stateless operations. **S42:** Spawn gana param ElementTableSO, crea vcam Cinemachine hija por unidad. **S43:** VCam referencia la CinemachineCamera hija (para CombatCameraDirector). **S47:** Bind de barra cambia a sin argumentos — la barra se inicializa en blanco, los datos vienen por SetHp/SetShield después.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Team(side)` | `IReadOnlyList<CombatVisualUnit>` | Retorna la lista inmutable de unidades de un lado (A o B) |
| `Get(side, index)` | `CombatVisualUnit` | Busca la unidad en un lado/índice exacto; null si no existe |
| `Spawn(side, dnas, snapshots, board, prefab)` | `void` | **S42/S47:** Instancia visualizadores para un equipo, resuelve anchors por fila, arma instancias, binds de barras (S47: sin args), crea vcams; **S43:** VCam capturada en unit.VCam |
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
    public MoriMonchiVisualizer Instance;                              // prefab instanciado
    public MoriMonchiCombatVisualizer Hooks;                           // ref al comportamiento del visualizador
    public MoriMonchiCombatVisualizerUITK Bar;                         // barra UI world-space
    public MoriMonchiProceduralAnimator Anim;                          // animator para keyframes
    public Transform Anchor;                                           // nodo del board donde vive
    public Unity.Cinemachine.CinemachineCamera VCam;  // S42/S43 NEW   // vcam child para cortes de cámara
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

## Flujo de Spawn (S41 → S42 → S43 → S47)

**Entrada:**
- `side` — `CombatVisualSide.A` o `.B`
- `dnas` — lista de CreatureDNA del equipo (1..3)
- `snapshots` — lista de `CombatFighterSnapshot` (índice 1:1 con dnas)
- `board` — Transform del BoardA o BoardB (contiene los anchors hijos)
- `prefab` — MoriMonchiVisualizer a instanciar

**Proceso:**
1. Borra el equipo anterior (teamA o teamB según side)
2. Itera por cada DNA con su snapshot correspondiente
3. Resuelve el anchor por `snapshot.Row` (0=Front, 1=Mid, 2=Back)
4. Instancia el prefab como hijo del anchor
5. Assemble visual (fur, parts, colores) vía `MoriMonchiVisualizer.SetFurDatabase + Assemble`
6. Captura referencias (Hooks=`as MoriMonchiCombatVisualizer`, Bar=`GetComponentInChildren`, Anim=`GetComponent`)
7. **S47 NUEVO:** Bind barra UI sin argumentos: `unit.Bar?.Bind();` — la barra se inicializa vacía, esperando SetHp/SetShield
8. **S42 NUEVO:** Crea GameObject hijo "VCam_{side}{i}" con CinemachineCamera (priority 0) + CinemachineRotationComposer
9. **S43 IGUAL:** Asigna referencia vcam a `unit.VCam` (ya se hacía en S42, ahora solo documenta)
10. Agrega `CombatVisualUnit` a la lista del equipo

**Resultado:** El equipo está vivo en pantalla, cada unidad en su anchor exacto, barras inicializadas vacías (se rellenan vía API CombatVisualizerService), vcams listos para corte de cámara (CombatCameraDirector levanta prioridades).

## S47 Cambios

**Bind de barra sin argumentos:**
- **Antes (S42-S46):** `unit.Bar?.Bind(snapshot.Name, snapshot.Attack, snapshot.Speed, snapshot.Role, elementName, elementColor);` — barra inicializada con stats
- **Ahora (S47):** `unit.Bar?.Bind();` — sin argumentos, barra inicializada en blanco
- Los datos (HP, escudo, marcos) llegan posteriormente vía API:
  - `SetHp(current, max)` — sincroniza porcentaje de barra
  - `SetShield(amount)` — dibuja segmento azul
  - `SetActiveTurn(bool)` — marco dorado
  - `SetTargeted(bool)` — marco rojo

**Impacto:** Simplifică la inicialización (no necesita stats de snapshot), la barra se actualiza dinámicamente durante replay sin mantener copias de datos.

## S41 Cambios

**Nuevo en S41:**
- Extracción del spawn/lookup de `CombatVisualizerService` hacia esta clase (regla 11: composición)
- Convención de anchors por nombre (Front0/1, Mid0/1/2, Back0/1) en lugar de array genérico
- Binding de barras directamente durante spawn (antes se hacía en el visualizer)
- Helper `TransformOf` y `PosOf` para que el visualizer acceda a transforms sin conocer la lista

**Composición de regla 11:** `CombatVisualizerService.units : CombatVisualUnits` es el único lugar que mutates las listas; no hay statics ni lookups globales. El service orquesta la reproducción, esta clase solo sabe spawnar/desmantelar.

## S42 Cambios

**Nuevo en S42:**
- Resolución de identidad del elemento desde ElementTableSO (ya sin usar en S47, pero conservado para compatibilidad)
- **Nuevos objetos Cinemachine:** Por cada unidad spawneada, crea GameObject hijo con:
  - Nombre: `VCam_{side}{i}` (p.ej. "VCam_A0", "VCam_B2")
  - Posición: localPosition (0f, 1.5f, -2.6f) relativa a la unidad
  - Componente `CinemachineCamera` con Priority = 0 (inactivo por defecto)
  - Componente `CinemachineRotationComposer` (para rotación/look)
- Referencia vcam capturada en `unit.VCam`

**Impacto:** VCams listos para cortes de cámara dinámicos durante replay.

## S43 Cambios

**Sin cambios de código; documentación:**
- `unit.VCam` ya se asignaba en S42 (línea `unit.VCam = vcam;` tras AddComponent)
- **Nuevos subscribers:** CombatCameraDirector suscribe `OnActiveUnit` y llama `CombatVisualizerService.VCamOf(side, index)` para elevar prioridades
- **Nuevo método en CombatVisualizerService:** `VCamOf(side, index)` retorna `units.Get(side, index)?.VCam`

**Impacto:** Los VCams creados en S42 ahora son activamente dirigidos por CombatCameraDirector durante el replay (cortes dinámicos por turno).

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualizerService]] — orquestador que usa esta clase para spawn/lifecycle, llama Bind() sin args (S47)
- [[CombatCameraDirector]] — **S43 NEW** suscriptor que accede VCams via VCamOf()
- [[MoriMonchiCombatVisualizerUITK]] — **S47** barra minimal, Bind() sin args, API SetHp/SetShield/SetActiveTurn/SetTargeted

## Conexiones

- **Entrada:** `CombatVisualizerService.Play()` llama `Spawn(side, dnas, snapshots, board, prefab)` en ambos lados
- **Salida:** Popula lista de `CombatVisualUnit` que el service itera cada frame para animar barras/posiciones
- **Refs:** `MoriMonchiVisualizer` (prefab), `GameManager.FurTypeDatabase`, `GameManager.PartVisualBank`
- **Board refs:** BoardA/BoardB (Transform con 7 hijos anchor)
- **Hooks:** `MoriMonchiCombatVisualizer`, `MoriMonchiCombatVisualizerUITK`, `MoriMonchiProceduralAnimator`
- **S42/S43:** `CinemachineCamera`, `CinemachineRotationComposer` (vcams hijos)
- **S43 Query:** `CombatCameraDirector.HandleActiveUnit()` → `CombatVisualizerService.VCamOf()` → `units.Get()?.VCam`
- **S47:** `CombatVisualizerService.ForwardRoutine()` llama `SetHp()` / `SetShield()` / `SetActiveTurn()` / `SetTargeted()` sobre `unit.Bar`
