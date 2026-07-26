---
tags: [script, world, npc, data]
---

# NpcDialogueBank.cs

**Ruta:** `World/Npc/NpcDialogueBank.cs`

**Responsabilidad:** Clase estática pura que proporciona frases de diálogo para NPCs (S68: keys de localización en lugar de strings hardcodeados). Almacena arrays de keys (5-6 por situación; la resolución a idioma ocurre en `One()` via `Loc.Tr`). Sin estado mutable.

## Cambios S68 (Localization-ready)

**Antes:** Arrays de frases en español (e.g., `wandering[] = {"¿Qué tendrán hoy?", "A ver qué me encuentro…", ...}`)

**Ahora:** Arrays de keys de localización (e.g., `wandering[] = {"npc.wandering.01", "npc.wandering.02", ...}`)

**Método `One()` (línea 99-100):**
```csharp
private static string One(string[] bank, string targetName) =>
    Loc.Tr(bank[Random.Range(0, bank.Length)], targetName);
```
- Antes: elegía frase aleatoria de array (string directo)
- Ahora: elegía key aleatoria, luego `Loc.Tr(key, targetName)` → resuelve a idioma activo + formatea con {0}

**Datos públicos (arrays privados, acceso vía método `Pick`):**
- `wandering[]` (6 keys): vagando por la tienda sin objetivo. Ej: "npc.wandering.01", "npc.wandering.02", ...
- `inspecting[]` (6 keys): examinando una criatura en estante. Ej: "npc.inspecting.01", ...
- `approaching[]` (5 keys): decidió comprar, va hacia caja. Usa `{0}` como placeholder del nombre MM.
- `queueing[]` (5 keys): espera en fila.
- `waiting[]` (5 keys): frente de fila, esperando respuesta del jugador.
- `negotiating[]` (5 keys): durante negociación con jugador. Usa `{0}` para nombre MM.
- `purchased[]` (5 keys): compró exitoso. Usa `{0}` para nombre MM.
- `outbid[]` (5 keys): otro cliente compró su objetivo. Usa `{0}` para nombre MM.
- `queueFull[]` (5 keys): no pudo entrar a la fila.
- `noDeal[]` (5 keys): se va sin comprar (rechazó contraoferta, timeout, etc.).

**Métodos públicos:**
- `Pick(NpcAgent.NpcState state, NpcAgent.LeaveReason reason, string targetName) → string`: devuelve una frase aleatorio-seleccionada + formateada con `targetName`, según el estado actual del cliente.
  - Para estados `Wandering`, `InspectingDisplay`, `Queueing`, `WaitingAtRegister`: ignora `reason`, consulta el array correspondiente al estado.
  - Para estado `ApproachingRegister`, `Negotiating`: ignora `reason`, consulta el array del estado.
  - Para estado `Leaving`: ramifica según `reason`:
    - `LeaveReason.Purchased` → elige de `purchased[]`.
    - `LeaveReason.Outbid` → elige de `outbid[]`.
    - `LeaveReason.QueueFull` → elige de `queueFull[]`.
    - `LeaveReason.None` (o default) → elige de `noDeal[]`.
  - Reemplaza `{0}` en la frase localizada con `targetName` usando parámetro args en `Loc.Tr(key, targetName)`.

**Privados:**
- `One(string[] bank, string targetName) → string`: auxiliar que elige un elemento aleatorio del array de keys y lo resuelve vía `Loc.Tr(key, targetName)`.

**Datos por situación (resumen de contenido):**

| Situación | Estado | Clave | Contenido |
|-----------|--------|-------|----------|
| Explorando | Wandering | vagancia | Curiosidad, exploración, expectativa. |
| Inspeccionando | InspectingDisplay | análisis | Deliberación, indecisión, observación. |
| Yendo a caja | ApproachingRegister | decisión | Entusiasmo por la criatura elegida (con nombre). |
| Fila de espera | Queueing | paciencia | Tolerancia, aburrimiento, impaciencia. |
| Frente de caja | WaitingAtRegister | impotencia | Llamadas de atención, golpeteo, fastidio. |
| Negociando | Negotiating | regateador | Preguntas sobre precio (con nombre MM). |
| Compró feliz | Leaving + Purchased | victoria | Alegría, satisfacción, promesa de cuidado (con nombre). |
| Perdió subasta | Leaving + Outbid | derrota | Decepción, arrepentimiento, reclamo (con nombre). |
| Fila llena | Leaving + QueueFull | impotencia | Queja por aglomeración, sin nombre. |
| Se va vacío | Leaving + None/Other | resignación | Conformismo, próxima oportunidad, sin nombre. |

**Notas de implementación:**
- Cada array tiene 5-6 keys para evitar repetición (Random.Range sorteea índice).
- Los placeholders `{0}` en frases ahora se reemplazan via `Loc.Tr(key, targetName)` (Unity Localization resuelve args en la tabla).
- No hay estado privado ni inicialización: es purely functional.
- Consumidor único: [[NpcThoughtTag]] llama `Pick()` en `UpdateThought()` cuando detecta cambio de situación.
- S68 cambio: arrays ahora guardan keys (strings), no frases en español. Resolución a idioma ocurre en `One()`.

**Vinculado a:** [[Index/04 - Customer System]], [[Index/14 - Localization]]

**Conexiones:** [[NpcThoughtTag]] (único consumidor), [[NpcAgent]] (lee Estado y LeaveReason), [[NpcNameBank]] (patrón paralelo), [[Loc]] (resolución localización)
