---
tags: [script, ui, expedition, presentation]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** Presentador de UI dinámica vía UIToolkit (construcción completa por código en OnEnable). **S102 NUEVO:** oculta marcador/roster/resultado hasta que ronda corre (IsRunning) o termina (IsOver). Etiqueta de sala siempre visible. Actualiza cada frame solo si hay cambio (caché de últimos textos). Renderiza ocupaciones con Verb() (vigila/rompe/distrae/recolecta).

## Campos Serializados

- **round** (ArenaRound, Required) — lectura de estado vivo
- **playerColor**, **rivalColor**, **timeColor** — colores de texto

## Elementos UIToolkit (instanciados en OnEnable)

- **scoreboard** — flex row Player | Timer | Rival (arriba centro)
  - playerLabel, timeLabel, rivalLabel
  - **S102 NUEVO:** oculto hasta IsRunning || IsOver
  
- **rosterRoot** — flex row (Player names | Rival names) debajo de scoreboard
  - playerRoster, rivalRoster
  - **S102 NUEVO:** oculto hasta IsRunning || IsOver
  
- **seedLabel** — "sala NNNN" (esquina arriba-izq, opacidad 0.7)
  - **S102 NUEVO:** siempre visible (no se oculta post-round)
  
- **resultRoot** — resultado (Ganas/Pierdes/Empate, centro)
  - resultLabel
  - **S102 NUEVO:** oculto hasta IsOver

## Métodos Privados

- `RefreshRoster() → void` — itera Spawned, agrupa por Team, construye strings nombre + Verb(ocupación)
  - **S102 NUEVO:** lastRosterCount se reinicia en OnEnable (no persiste entre sesiones)
  
- `RefreshSeed() → void` — "sala {ActiveSeed}"
  - nunca se oculta

- `Verb(Occupation) → string` — Guard→"vigila", Break→"rompe", Decoy→"distrae", default→"recolecta"

- `Update() → void` — tick principal:
  1. Oculta/muestra scoreboard + roster:
     - visible = (IsRunning || IsOver)
  2. Si visible y cambios:
     - RefreshRoster() si Spawned.Count != lastRosterCount
     - actualiza playerLabel/rivalLabel si puntos cambiaron
     - actualiza timeLabel si Remaining cambió (formato MM:SS)
  3. Si IsOver y no resultShown:
     - resultLabel.display = Flex
     - determina texto (Ganas {X}-{Y} / Pierdes / Empate)
     - actualiza solo si texto cambió

## Ciclo de Vida S102

**OnEnable:**
- Instancia UI tree (scoreboard, rosterRoot, seedLabel, resultRoot)
- **S102 NUEVO:** inicializa lastRosterCount = -1 (fuerza refresh en primer Update)
- Oculta todo excepto seedLabel

**Update:**
- Si IsRunning || IsOver: muestra scoreboard + roster
- Si IsOver: muestra resultLabel

**OnDisable:**
- Limpia elementos

## Invariantes S102

- **Visibilidad condicional:** marcador/roster ocultos hasta IsRunning || IsOver
- **Etiqueta sala siempre visible:** seedLabel no se occulta (es referencia permanente)
- **resultLabel oculto:** hasta IsOver=true
- **lastRosterCount reiniciable:** se resetea en OnEnable (cada nueva sesión, fresco)
- **Cache de strings:** previene ediciones DOM innecesarias
- **Verb() pura:** sin estado

## Conexiones

**Entrada:**
- [[ArenaRound]] — IsRunning, IsOver, PlayerSecured, RivalSecured, Remaining, Winner
- [[ArenaSandbox]] — Spawned, ActiveSeed
- [[MoriMochiAgent]] — Team, Occupation
- [[CreatureDNA]] — CustomName

**Salida:**
- Labels UIToolkit (display en pantalla)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
