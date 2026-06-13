# RunRunSimulator — MoriMonchis — CLAUDE.md

>Empieza cada mensaje diciendo Juan, . Este archivo es tu regla de oro: leelo siempre primero.

## Source of truth

| Recurso | Para que |
|---------|----------|
| Notion Wiki | Diseno vivo, decisiones, preguntas abiertas. Cuando dudes de **diseno**, abre Notion. |
| `MoriMonchiVault/` (Obsidian) | Detalle de **implementacion**, quirks tecnicos, archivos clave. Cuando dudes de **codigo**, lee del vault. |
| `MoriMonchiVault/ScriptNodes/` | Un nodo `.md` por cada script `.cs`. Leer antes de abrir el codigo fuente. |

---

## Protocolo de trabajo para IA

1. **Abrir sesion**: leer `MoriMonchiVault/Index/09 - Active Context.md` (estado actual)
2. **Identificar sistema**: usar `MoriMonchiVault/00 - Index.md` (routing por tarea)
3. **Leer diseno**: abrir `MoriMonchiVault/Index/XX - Tema.md` (diseno, flujo, invariantes)
4. **Leer script nodes**: abrir `MoriMonchiVault/ScriptNodes/NombreScript.md` (responsabilidad, conexiones)
5. **Planear con Opus**: disenar la solucion antes de picar codigo. Evaluar alternativas, invariantes, impacto en otros sistemas.
6. **Solo entonces leer `.cs`**: ya sabes que hace cada script y como se conecta. Confirmar que el plan encaja.
7. **Generar sub-agentes Sonnet**: delegar tareas concretas a sub-agentes (uno por archivo o responsabilidad). Cada sub-agente recibe el plan, la ruta del archivo, y las reglas de codigo.
8. **Cerrar sesion**: actualizar `09 - Active Context.md` con lo tocado y siguiente paso.
9. **Disparar agente de vault** *(autorizado por Juan, ejecutar siempre al cierre)*: invocar Deepseek via OpenCode para actualizar ScriptNodes. Ver seccion **Agente Externo de Vault**.
10. **Cada mensaje** empieza con "Juan:" seguido del contenido

---

## Proyecto

Simulador de tienda retro 3D (80s). Cria/pelea MoriMonchis (Gremlins + Furby + Tamagotchi) con genetica visible, muerte permanente y combate async server-side.

- **Singular**: MoriMochi · **Plural**: MoriMonchis | Codigo: `Creature`/`CreatureDNA` · UI/assets: MoriMochi/MoriMonchis
- **Stack**: Unity C# · Odin Inspector · Newtonsoft.Json · UGS (Auth, Cloud Save, Cloud Code, Scheduler)

---

## Arquitectura de vault

```
MoriMonchiVault/
├── 00 - Index.md              ← Entry point IA (routing por tarea)
├── Index/                     ← 11 notas principales por dominio (01-11)
└── ScriptNodes/               ← 95 nodos, uno por script .cs
```

---

## Reglas de codigo (NO NEGOCIABLES)

1. **Desacoplamiento estricto via eventos**: Comunicacion cross-system solo por `GameEvents`. El evento transporta la data. Suscriptor NO busca `GameManager.Instance.Registry`.
2. **Persistencia solo por evento**: Ningun gameplay script llama `SaveSystem.SaveDatabase` ni `PushToCloud`. Solo emiten `GameEvents.RegistryChanged`. `GameManager` es el unico dueno de persistencia.
3. **Sin comentarios en codigo**: No anadir `//` ni `/* */` sin pedido expreso de Juan. La documentacion vive en el vault.
4. **Sin features adelantadas**: No implementar mecanicas hasta su etapa del roadmap.
5. **DNA como string ligero**: `ToStringID()`/`FromID()` son el contrato de red. Timestamp es metadata, no parte del genetic string.
6. **IDs de partes**: nunca pueden contener `-` (separador del DNA string).
7. **Odin siempre**: `SerializedScriptableObject` con `[OdinSerialize]` para diccionarios.
8. **Sin complejidad innecesaria**: No anadir campos, abstracciones ni features no pedidos. Tres lineas similares > abstraccion prematura.
9. **Desuscribir siempre**: `OnEnable` suscribe, `OnDisable` desuscribe. Un `event static` mantiene vivo al suscriptor (leak + excepcion al disparar sobre objeto destruido).
10. **Evitar referencias redundantes**: Siempre buscar centralizar eventos y comunicarlos o suscribirlos atravez de eventos o singleton

---

## Eventos (GameEvents.cs)

| Evento | Quien dispara |
|--------|---------------|
| `OnRegistryChanged` | toda mutacion de gameplay |
| `OnRegistryReloaded` | CloudSyncService tras pull/reset (UI-only, sin push) |
| `OnCreatureMinted` | GameManager.MintRandomCreature |
| `OnCombatCompleted` | combate local |
| `OnCombatLogged` | AsyncCombatService.ApplyResult |
| `OnBreedingCompleted` | breeding local + async |
| `OnFurnitureChanged` | FurnitureService (toda mutacion) |
| `OnFurnitureReloaded` | reload furniture (clear+resync, sin push) |

Eventos UI viven en `UIManager` como `static event Action`. Detalle en [[MoriMonchiVault/Index/05 - UI System]] y [[MoriMonchiVault/Index/07 - Persistence & Identity]].

---

## Workflow de documentacion

| Capa | Dueno | Cuando |
|------|-------|--------|
| **Notion** (diseno) | Juan | Decision de diseno nueva, pregunta resuelta. Yo NO toco Notion. |
| **MoriMonchiVault/Index/** (implementacion) | IA (a pedido) | Cambio contrato publico, quirk tecnico nuevo, sub-etapa cerrada. |
| **MoriMonchiVault/ScriptNodes/** | Agente externo Deepseek | Automatico al cierre de sesion si hubo scripts tocados. |
| **CLAUDE.md** (nucleo) | IA (a pedido) | Regla nueva, cambio de stack, roadmap status flip, nuevo archivo top-level del vault. |
| **09 - Active Context** | IA (cada sesion) | Apertura y cierre de sesion. |

**09 - Active Context** debe listar al final: que archivos `.cs` se modificaron y que archivos se crearon. Esa lista es el input del agente externo.

Bug menor sin cambiar contratos → no actualizar vault (git log basta).

**Backup del CLAUDE.md original**: `ClaudeOld.md` y `ClaudeOld_1.md` en la raiz.

---

## Agente Externo de Vault (Deepseek via OpenCode)

**Proposito**: actualizar `ScriptNodes/` al cierre de sesion sin gastar tokens de Opus/Sonnet en documentacion mecanica.

**Comando** (ejecutado por Claude Code via Bash al paso 9):
```
opencode run -m opencode/deepseek-v4-flash-free "<prompt>"
```

**Formato del prompt de handoff** (autocontenido, Deepseek no tiene contexto del proyecto):
```
Eres un agente de documentacion para un proyecto Unity C#.
Tu unica tarea: actualizar los ScriptNodes del vault de Obsidian.

RUTA DEL VAULT: C:/Users/USUARIO/Documents/GitHub/RunRunSimulator/MoriMonchiVault/ScriptNodes/

SCRIPTS TOCADOS EN ESTA SESION:
- [ruta relativa al script] → [NUEVO | MODIFICADO] → ScriptNodes/[NombreScript].md

INSTRUCCIONES:
1. Lee cada script .cs listado arriba.
2. Para MODIFICADO: actualiza el .md existente (responsabilidad, campos publicos, conexiones con otros sistemas).
3. Para NUEVO: crea el .md siguiendo el formato de cualquier nodo existente en ScriptNodes/.
4. No toques ningun otro archivo.
5. No agregues comentarios ni explicaciones fuera del .md.
```

**Reglas de ejecucion**:
- Ejecutar **solo** si hubo scripts `.cs` tocados en la sesion (bug cosmético sin cambio de contrato → omitir).
- Si el agente falla, Claude lo reporta a Juan y lo registra en `09 - Active Context` como pendiente.
- Juan tiene **autorizado** este paso sin confirmacion adicional por sesion.
