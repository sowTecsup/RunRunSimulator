---
tags: [draft, arena, combate, diseno]
---

# 27 - Intenciones en vivo (Draft)

> Borrador aislado para analizar. No está enlazado desde `00 - Index` ni reemplaza nada todavía. Nace de la charla S125.

## 1. Regla

**Cada MoriMochi tiene tres botones: Pelear, Huir, Recolectar. Solo uno activo a la vez, se cambia en vivo, sin espera. La personalidad (`Role`) decide QUÉ hace con la intención; los rasgos (diales) deciden CÓMO lo hace.**

- Por criatura, no por equipo.
- Reemplaza las bases por personalidad de S122 (`ArenaBases`) y las tres órdenes de antes de la sala (`ArenaOrders`: Loot/Contact/Posture).
- Test del texto plano: un botón produce tres conductas distintas según a quién se lo das. Conocer a tu equipo es la habilidad.

## 2. Decisiones de Juan (S125)

| # | Pregunta | Respuesta |
|---|---|---|
| 1 | ¿Botón por equipo o por criatura? | Por criatura |
| 2 | ¿Solo antes de la sala o en vivo? | En vivo |
| 3 | ¿Reemplaza bases o va encima? | Reemplaza |
| 4 | ¿Huir = salir de la sala o dejar de pelear? | Dejar de pelear y escapar |
| 5 | ¿Huir saca de la sala a salvo? | No: busca ponerse seguro, pero puede volver a pelear. Sirve cuando ves al enemigo cargando un ataque |
| 6 | ¿Cambiar de intención tiene espera? | No |
| 7 | ¿Los rasgos (valentía, etc.) influyen? | No en la decisión, sí en cómo responde |

## 3. Tabla Role × Intención

| | Agresivo | Protector | Empático |
|---|---|---|---|
| **Pelear** | Caza al rival más débil o al ladrón (cazador actual) | Pelea sin alejarse del material ni de sus compañeros (guardián actual) | Se suma al compañero que ya está peleando (2 contra 1); si nadie pelea, guarda |
| **Huir** | Sale del peligro lo justo: se queda al borde, listo para volver | Sale del peligro poniéndose delante de un compañero | Sale del peligro juntándose con el compañero más herido |
| **Recolectar** | Recolecta en la veta lejana, del lado rival | Recolecta en la veta segura, cerca | Recolecta cerca de otro compañero |

### Huir, común a los tres

- **Peligro** = plantillas de ataque cargándose (`ClashTelegraphing`) + rivales amenazantes cerca.
- Sale de la plantilla primero, después toma distancia. No inicia choques. Si lo alcanzan, recibe el golpe normal.
- Queda "a salvo" en su punto según Role hasta que el jugador cambie de intención.
- Si llevaba material, lo conserva (no lo asegura; asegurar sigue siendo de Recolectar).

### Rasgos → cómo responde

| Rasgo | Efecto |
|---|---|
| Valentía (`Boldness`) | Distancia de huida (bajo = más lejos) y tiempo de reacción ante el telegrafiado (bajo = reacciona antes) |
| Sociabilidad | Cuánto se pega al grupo al huir o recolectar |

Los diales nunca bloquean ni cambian la intención.

## 4. Plan técnico por lotes

**A · Datos (tabla de intenciones)**
- `enum ArenaIntent { Fight, Flee, Gather }`.
- Estática nueva `ArenaIntents` (reemplaza `ArenaBases`): `(Role, ArenaIntent)` → ocupación + sitio, nombre corto y descripción de la conducta, lectura del rival.
- Borrar `ArenaBase`, `LootChoice`, `ContactChoice`, `PostureChoice`, `OrderPillar`, `ArenaOrders`, `ArenaBases`; `ArenaOrderRules` y `ArenaOrderCatalog` se reducen o desaparecen.
- `Occupation` gana `Evade` (o equivalente).

**B · Comportamiento**
- `MoriMochiAgent.SetIntent(ArenaIntent)`: cancela la tarea activa y re-enruta en el mismo frame.
- `AgentExpedition` enruta por ocupación derivada de `(Role, Intent)`.
- Tarea nueva `AgentEvade` (colaborador, una responsabilidad): lee peligro, sale de plantillas, elige punto seguro por Role, respeta diales.
- `AgentGatherer` y `AgentClash` dejan de leer pilares (`Orders.Contact`/`Posture`/`Loot`) y leen intención + Role.
- Empático/Pelear usa `TryJoinSocialFight` existente; cae a guardián si nadie pelea.

**C · Tarjeta y panel**
- `ArenaHudCard`: tres botones de intención por tarjeta, clickeables en vivo (también durante `hud-card--fighting`), activo resaltado con color de equipo. Íconos antes que texto.
- `ArenaPlanPanel`: se borra la fila BASE; queda elegir intención inicial por criatura.
- `ArenaCastPlanner`, `ArenaSandbox`, `ArenaRosterSO`, `ArenaCastEntry`: `ArenaOrders` → `ArenaIntent`.
- Lectura del rival: "Agresivo" + intención visible solo al revelarse (regla S107 de descubrimiento).

**D · Rivales (se corta si falta tiempo)**
- Reglas simples por bot: Huir si ve telegrafiado propio en rango o vida baja; volver a su intención base al pasar el peligro.
- Intención base por semilla de sala (reemplaza `RivalPlans`).

## 5. Mutaciones fuera de código (requieren OK)

- UXML/USS de la tarjeta del HUD y del panel de plan.
- `ExpeditionRulesSO`: distancias y tiempos de huida, umbral de vida baja del bot.
- `ArenaRosterSO` (roster de desarrollo): migrar entradas de órdenes a intención.
- Tabla `Strings` si los textos van localizados.

## 6. Abiertas

1. `ArenaMatrixDev` / `ArenaMatrixPlans` prueban las 6 bases: ¿borrar o adaptar a 9 celdas Role × Intención?
2. Huir sin salida: ¿qué hace si el peligro lo rodea (acorralado)? Propuesta: se encoge (menos empuje recibido), no pelea.
3. ¿El jugador ve la plantilla del rival cargándose con tiempo suficiente para reaccionar a mano? Medir ventana de telegrafiado vs. tiempo de clic.
4. ¿La intención activa se muestra sobre la criatura en el mundo o solo en la tarjeta?
5. Recolectar Agresivo "del lado rival": ¿roba material soltado o solo mina vetas lejanas?

## 7. Riesgos

- Microgestión: con 3 criaturas y cambio sin espera, la partida puede volverse de clics rápidos. Vigilar en la sesión de feel a 1×.
- Huir demasiado fuerte deja todos los choques en empate; la ventana de telegrafiado es la perilla.
- Borrar `ArenaOrders` toca ~20 archivos; conviene lote A+B compilando juntos.
