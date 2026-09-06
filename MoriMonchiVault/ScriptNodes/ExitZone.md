---
tags: [script, world, expedition, objective]
---

# ExitZone.cs

**Ruta:** `World/Expedition/ExitZone.cs`

**Responsabilidad:** Zona de depósito circular donde criaturas aseguran unidades (minerals/crystals) durante un round de arena. Cada salida pertenece a un equipo (Player, Rival) y acumula los `Secured` depositados. `Contains(worldPosition)` verifica si un punto está dentro del radio (Y-ignorant). Solo `AgentExpedition.Secure()` llama a `Deposit()`. Las salidas se siembran en `ArenaSandbox.SpawnExits` en posiciones fijas: `center + dir * (arenaHalfSize - 4)` (inset 4 → cuatro esquinas).

## Campos serializados

- **radius:** radio de detección circular (default 2.5m, Min 0.5m)
- **onDeposit:** UnityEvent disparado cada vez que se deposita algo

## Propiedades públicas

- **Radius → float** — radio de la zona (Read-only)
- **Team → ExpeditionTeam** — equipo propietario (Player, Rival), delegado a `perceivable` (Read-only)
- **Secured → int** — unidades acumuladas (Read-only, int { get; private set; })

## Métodos públicos

- `SetTeam(ExpeditionTeam team)` — asigna equipo a través de perceivable
- `Contains(Vector3 worldPosition) → bool` — retorna true si el punto está dentro del radio XZ (ignora Y)
- `Deposit(int units)` — suma unidades si units > 0, dispara `onDeposit` event

## Flujo

1. **Spawn:** `ArenaSandbox.SpawnExits` instancia 4 ExitZone (una por esquina, 2 Player + 2 Rival), cada una con `Perceivable` attached y equipo asignado vía `SetTeam()`
2. **Detección:** criaturas en `Occupy.Exit` o navegando llaman `zone.Contains(position)` para verificar proximidad
3. **Depósito:** cuando una criatura completa su ocupación (Securing fase), `AgentExpedition.Secure()` llama `zone.Deposit(units)` → `Secured += units` + evento
4. **Cierre:** `ArenaRound.End()` lee `exits[i].Secured` congelados en `frozenPlayerSecured` / `frozenRivalSecured` y determina ganador

## Invariantes S101

- `Secured` nunca disminuye (solo suma, Deposit chequea `units > 0`)
- Radius es inmutable post-Awake
- Team es inmutable post-SetTeam (asignado al inicio)
- `Contains()` proyecta al plano horizontal (Y = 0), ignora altura de criatura

## Conexiones

**Entrada:**
- Lectura: `transform.position`, `radius`
- Setter: `SetTeam(ExpeditionTeam)`

**Salida:**
- Evento `onDeposit` disparado desde `Deposit()`
- Propiedad `Secured` leída por `ArenaRound.SumSecured()`

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[AgentExpedition]]
- [[Perceivable]]
- [[ArenaRound]]
