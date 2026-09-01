---
tags: [script, combate, dragon-rps, interaction]
---

# DragonRpsSession.cs

**Ruta:** `DragonRps/DragonRpsSession.cs`

**Responsabilidad:** Combate interactivo ronda a ronda, pensado para UI o CLI. Mantiene `Player` (tu dragón) y `Foe` (rival). Método `Board()` retorna texto de tablero: golpes, tu mano con potencia visible, y estado intacto de ambos (cuántos de cada tipo quedan — descarte público del rival). `Play(handIndex)` ejecuta tu turno, elige la IA rival con `Counting`, resuelve ronda, detecta fin. Retorna feedback de texto para cada acción. **No dispara GameEvents ni persiste.**

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsSide]], [[DragonRpsBrain]], [[DragonRpsMatch]]
