---
tags: [script, data, expedition, state]
---

# ArenaRun.cs

**Ruta:** `Data/Expedition/ArenaRun.cs`

**Responsabilidad:** Modelo de datos que mantiene el estado de una bajada por pisos (run) de la expedición. Gestor de vida de los combatientes, material asegurado y progresión de pisos. Determinista: pisos se generan por semilla derivada de BaseSeed (XOR). Tipos de piso: Enemies (combate) y Buff (recuperación de vida + material gratis). Diferencia crítica S124: **vida es el riesgo** — no es energía restaurada, sino puntos que decrecen al perder combates. Máximo 100, -15 por golpe recibido, +30 en pisos de Buff.

**S124:** Creado. Constantes: `HealthPerKnock=15`, `BuffHealth=30`, `MaxHealth=100`, `BuffEvery=3` (cada 3 pisos). Determinismo: `FloorSeedOf()` genera semilla única por piso vía XOR determinista.

**S129:** `ToResult()` construye `FallenIds` y `TeamIds`. Removidos `SetStartHealth` y diccionario `startHealth`; todas las criaturas entran con `MaxHealth`.

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `BaseSeed` | `int` | Semilla raíz de la bajada (ambiente TickCount) |
| `Floor` | `int` | Piso actual (arranca en 0, incrementa con EnterFloor) |
| `Material` | `int` | Material asegurado acumulado |
| `Lost` | `bool` | True si el rival ganó un piso de Enemies |
| `FloorsCompleted` | `int` | Pisos totales jugados |
| `TeamIds` | `IReadOnlyList<string>` | IDs de criaturas del equipo del jugador |
| `CurrentKind` | `ArenaFloorKind` | Tipo del piso actual (Enemies o Buff) |
| `NextFloor` | `int` | Floor + 1 |
| `NextKind` | `ArenaFloorKind` | Tipo del próximo piso |

## Métodos Clave

| Método | Descripción |
|--------|-------------|
| `FloorSeedOf(baseSeed, n)` | Estático; genera semilla única para piso n vía XOR determinista |
| `FloorSeed(n)` | Instancia; atajos FloorSeedOf(BaseSeed, n) |
| `KindOf(n)` | Estático; piso n es Buff si (n > 0) && (n % 3 == 0) |
| `HealthOf(id)` | Retorna vida actual de criatura; default MaxHealth si no existe |
| `IsDown(id)` | True si HealthOf(id) <= 0 |
| `EnterFloor()` | Incrementa Floor; si CurrentKind == Buff, +30 vida a todos |
| `RecordFloor(winner, playerSecured, stats)` | Procesa resultado: acumula material, aplica daño (-15 por golpe), detecta derrota si Rival gana un Enemies |
| `ToResult()` | **(S129)** Construye ExpeditionResult con `FallenIds` (health ≤ 0) y `TeamIds` (elenco) |

## Ciclo de Vida Típico

1. Constructor: `new ArenaRun(baseSeed, teamIds)` — inicializa con IDs del elenco; todas entran con MaxHealth
2. `EnterFloor()` — dispara al entrar a un nuevo piso; Buff auto-cura
3. Gameplay: Arena sandbox corre combates
4. `RecordFloor(winner, secured, stats)` — después de cada ronda; aplica daño, acumula material
5. Repeat 2-4 hasta Lost o retiro manual
6. `ToResult()` — al retirarse; arma FallenIds (health ≤ 0) y TeamIds

## Invariantes S129

- Vida inicial: todas entran con MaxHealth (sin SetStartHealth)
- Vida: [0, 100], no energía restaurable — **riesgo irrevocable**
- Piso 0 inexistente: entradar con `EnterFloor()` incrementa a 1
- Buff cada 3: piso 3, 6, 9, ...
- Semilla determinista: misma BaseSeed + Floor = mismo despliegue de enemigos
- Material: acumula indefinidamente; se anula si Lost
- FallenIds y TeamIds armados en ToResult() (viajan en ExpeditionResult al retornar)

## Vinculado a

[[Index/22 - Bajada Nocturna y Linaje]] (S122), [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

## Conexiones

- [[ArenaRunDirector]] — orquestador de ciclo por piso
- [[ArenaFloorPanel]] — presentador UI de estado
- [[ExpeditionHandoff]] — entrada/salida de bajada
- [[ArenaRound]] — resultados de ronda para RecordFloor
- [[ArenaSandbox]] — generador de arena por semilla
