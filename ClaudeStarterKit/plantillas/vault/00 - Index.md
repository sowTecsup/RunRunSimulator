---
tags: [index, core]
---

# 00 — Index (AI Entry Point)

> Lee esto PRIMERO. Te dice exactamente que archivos leer antes de tocar codigo.

---

## Estructura del vault

```
<PROYECTO>Vault/
├── 00 - Index.md              <- ESTE ARCHIVO (entry point para IA)
├── Index/                     <- notas por dominio y diseno (01-XX)
└── ScriptNodes/               <- un nodo por script .cs
```

---

## Quick Routing (tarea -> leer esto primero)

| Tarea | Leer en `Index/` | Despues en `ScriptNodes/` |
|-------|------------------|---------------------------|
| **<Sistema A>** | [[Index/01 - ...]] | [[ScriptA]], [[ScriptB]] |
| **<Sistema B>** | [[Index/02 - ...]] | [[ScriptC]] |
| **Bugs conocidos** | [[Index/08 - Known Bugs & Checkpoints]] | — |
| **Sesion actual** | [[Index/09 - Active Context]] | — |
| **Deuda tecnica** | [[Index/11 - Technical Debt]] | — |
| **Editor en vivo (CLI + MCP)** | [[Index/12 - Unity Editor]] | — |

> Si un sistema no esta en esta tabla, para efectos practicos no existe. Cuando se agrega
> un sistema, se agrega su fila.

---

## Estructura del codigo

```
Assets/<PROYECTO>/Scripts/
├── Core/          # manager raiz, bus de eventos, save system, enums, interfaces
├── Data/          # estado puro: SOs, structs, databases
├── Systems/       # orquestacion por dominio, desacoplada via bus
├── World/         # representacion 3D
├── UI/            # paneles y HUD
├── Player/        # controller e inputs
└── Editor/        # tooling dev y tools propias
```

---

## Patrones arquitectonicos (no negociables)

1. **Bus de eventos** — comunicacion cross-system SIEMPRE por eventos estaticos. El evento
   lleva el payload; el suscriptor nunca va a buscar el singleton.
2. **Singleton** — solo para servicios de runtime.
3. **Pipeline de persistencia** — `Mutacion -> evento -> manager -> disco -> nube`.
   Ningun script de gameplay guarda directo.
4. **Aislamiento de input** — action maps mutuamente excluyentes, uno activo a la vez.

---

## Indice por palabra clave

| Palabra clave | Mirar en |
|---------------|----------|
| <termino que usa Juan> | [[Nodo]] o [[Index/XX]] |

---

## Como usar este vault (para la IA)

1. Identifica la tarea en la tabla de routing.
2. Lee la nota `Index/` del dominio: intencion, flujo, invariantes.
3. Lee los `ScriptNodes` especificos y segui los `[[wikilinks]]`.
4. Revisa `Index/08 - Known Bugs`.
5. Revisa `Index/09 - Active Context`.
6. **Solo entonces** lee los `.cs`.
