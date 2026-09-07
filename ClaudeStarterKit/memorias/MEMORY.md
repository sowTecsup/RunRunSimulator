# Memory Index

> Plantilla de indice de memorias portables. Copiar el contenido de esta carpeta a
> `C:\Users\<USUARIO>\.claude\projects\<slug-del-proyecto-nuevo>\memory\` y pegar estas
> lineas en el `MEMORY.md` de ahi (o usarlo entero si el proyecto es nuevo).
>
> Las anecdotas citadas vienen del proyecto MoriMonchis, pero la **regla** de cada una es
> general. El "por que" con el caso real es lo que hace que la regla se respete: no las
> resumas quitandoles la historia.

## Como trabajar (feedback)

- [Lenguaje: espanol neutral](feedback_language_neutral_spanish.md) — Hablar en espanol neutral, no rioplatense
- [Sin sobre-ingenieria](feedback_no_overengineering.md) — El approach mas simple es el correcto; preguntar quien ya tiene la informacion
- [Legibilidad primero](feedback_legibilidad_primero.md) — Nunca cumplir el minimo en presentacion/UI
- [Diseno legible en sus simientos](feedback_diseno_legible_en_simientos.md) — Nada de soluciones visuales para problemas de sistema; test del texto plano
- [QA proactivo con referentes](feedback_qa_proactivo_referentes.md) — Detectar detalles de feel ANTES de que Juan los reporte
- [Verificacion visual con screenshots](feedback_verificacion_visual_screenshots.md) — Toda entrega visible cierra con capturas MIRADAS
- [VFX solo con MMFeedbacks](feedback_vfx_solo_mmfeedbacks.md) — Juice NUNCA por codigo; estructura de prefab con hijo `Feedbacks/`
- [Guias visuales: la vara es Shapes](feedback_guias_visuales_vara_shapes.md) — Punteados, puntas redondas, curvas y transiciones de entrada
- [Obsidian solo con aprobacion](feedback_obsidian_on_approval.md) — Escribir en el vault solo con diseno cerrado y OK explicito
- [vault-documenter solo con comando](feedback_vault_documenter_solo_comando.md) — Nunca por iniciativa propia
- [Prefabs via MCP](feedback_prefabs_via_mcp.md) — No leer `.prefab`/`.unity` como texto

## Tooling y entorno (reference)

- [Unity CLI por PowerShell](reference_unity_cli_powershell.md) — La tool Bash bloquea `unity ...`; usar PowerShell
- [Fix push GCM](reference_gcm_push_fix.md) — Dos cuentas de credenciales cuelgan el push; forzar la correcta
- [Flujo de dos PCs](project_two_pcs_workflow.md) — Trabajo + casa; fetch al abrir, modal de "modified externally" bloquea el MCP
- [Cloud Code SDK](feedback_cloud_code.md) — Solo si el proyecto usa UGS: fetch REST directo, no el SDK

## Del proyecto nuevo (a escribir sobre la marcha)

<!-- Aca van las memorias propias del proyecto nuevo: GDD, lore, decisiones de diseno,
     sistemas vivos y muertos. Una memoria por hecho, con su **Why:** y **How to apply:**. -->
