---
tags: [index, combat, design]
---

# 13 - Combat Design Direction (norte de diseño)

**Status:** DIRECCIÓN DE DISEÑO decidida con Juan 2026-07-10 (cierre S36). Responde a la duda "¿el combate es solo un replay glorificado?". La implementación actual (S32–S35) es la base técnica; esto define hacia dónde evoluciona el **contenido** y la **presentación**. Diseño canónico → Notion (pendiente de volcar); esta nota es la captura viva.

## Referencias
- **Pokémon Quest**: equipo de 3, combate auto-simulado.
- **Super Auto Pets**: cada acción muestra impacto CLARO; entendés qué pasa sin saber los detalles; el **momento eureka** cuando tu build hace click.

## Decisión marco: combate = PAYOFF DEL THEORYCRAFT (autobattler)
La agencia vive en armar/criar/equipar el equipo; el combate lo resuelve. El **async determinista por semilla (S32) es pilar y SE QUEDA**. El fix de "se siente pasivo" NO es meter input en vivo (rompería el async), es: (a) drama legible + (b) un beat de agencia pre-pelea.

## Los 5 pilares
1. **3v3 team autobattler.** Equipo de 3 vs 3. Multiplica el espacio de sinergias y da la agencia de "composición de equipo" que el género necesita. Motiva la cría (criar para roles) y el equipo (kitear cada rol).
2. **Cada MoriMonchi = UN rol legible.** La legibilidad de SAP viene de que cada unidad tiene UNA identidad clara. Colapsar cada MM en un rol reconocible (tanque / pegador / soporte / disruptor / …) hace el tablero legible: mirás un equipo y entendés el plan.
3. **El equipo EXPRESA el rol (la palanca del eureka).** El MM tiene una inclinación innata (genética/stats); el equipo AMPLIFICA una característica para comprometerlo a un rol. Eureka = "vi que este MM tenía X alto, le puse Y para inclinarlo, y ahora es un [rol] demoledor". Ata la profundidad genética al payoff.
4. **Sinergias SIMPLIFICADAS → basadas en ROLES y telegrafiadas.** Retira el modelo opaco de recetas de stacks de elementos (S35: costaba VER qué pasaba). Sinergia = roles que se complementan, VISIBLE en el preview del equipo + en interacciones en cancha. Menos, más profundas, legibles.
5. **Visualizer: un beat dramático por vez.** Cada acción = causa→efecto clara, una a la vez, con pausa (el storytelling ya planeado: ghost bar teñida, banner de eventos, popups duraderos). Los roles ayudan: cada beat se narra solo ("el tanque aguanta, el pegador crítea, el soporte cura").

## Qué SOBREVIVE (motor — no se tira)
- Sim determinista por semilla (S32) — se extiende naturalmente a 3v3.
- Arquitectura de efectos polimórficos (leaves `SynergyEffectBase`/`CombatProcEffect`) — se **retarget** de "element stacks" a "kits de rol".
- Pipeline de equipo (`EquipmentSO`/`StatSheet`) — pasa a ser la palanca de rol.
- Motor del visualizer (replay, nodos, barras) — gana el layer de drama.
- Genética con permadeath — las stakes que hacen que mirar IMPORTE.

## Qué CAMBIA (contenido / scope)
- **1v1 → 3v3**: el lift grande. Toca el sim (`CombatService`/`SimulateCore`), `CombatRecord`/snapshots, el matchmaking JS, y el visualizer (mostrar 6 combatientes). Es el trabajo de ingeniería principal.
- **Elementos / estados emergentes (S35) → kits de rol**: el sistema de 6 elementos + recetas probablemente se **repurposea o archiva** (Juan pidió simplificar). El MOTOR queda; el CONTENIDO se re-autora. Marcado explícito para que futuras sesiones sepan que ese contenido está en revisión.
- **Targeting de 3v3**: nuevo — quién pega a quién (frente/atrás, prioridad por rol) es donde vive la legibilidad estilo SAP.

## Preguntas abiertas (próxima pasada de diseño, ANTES de tocar el sim)
1. El set de roles definitivo (¿4–5? tanque / pegador / soporte / disruptor / comodín?).
2. Cómo deriva el rol: ¿de la genética (stats natos), del equipo, o de ambos?
3. Reglas de targeting / posición en 3v3.
4. Cuánto del sistema de elementos S35 se conserva vs se archiva.
5. El beat de agencia pre-pelea concreto (postura / swap de ítem / prioridad de target).

## Opinión del orquestador (registrada)
La dirección es coherente y correcta para la profundidad ya construida. El insight de Juan — "rol propio por MM + equipo que resalta una característica" — es justo lo que da legibilidad (SAP) y momento eureka. Recomendación: mantener el async determinista, hacer del ROL la unidad de legibilidad, y **retargetear (no tirar)** el motor de efectos hacia kits de rol. El riesgo/costo principal es 1v1→3v3; conviene cerrar la pasada de diseño (set de roles + targeting) antes de tocar el sim. Coherencia con el resto del juego: la tienda fija + corrales (expansiones por PC) alimenta el roster de 3 que después pelea.

Relacionado: [[Index/03 - Combat]], [[Index/09 - Active Context]], [[Index/12 - Unity MCP]].
