---
tags: [script, ui, uitk]
---

# StoreContainerPriceTagUITK.cs

**Ruta:** `UI/StoreContainerPriceTagUITK.cs`

**Responsabilidad:** Componente UITK que renderiza precios estimados de MoriMonchis en un display. Requerimiento: `[RequireComponent(typeof(StoreContainer))]`. Ref `UIDocument`. Queries UITK: root (doc.root), list (`price-list`). Awake: obtiene StoreContainer. OnEnable: suscribe a `container.OnDisplayContentsChanged`. OnDisable: desuscribe. Start: binds root/list, llama Rebuild con Occupants iniciales. HandleChanged: callback que llama Rebuild. Rebuild: limpia list, si occupants vacío hide root, sino itera occupants, calcula precio de cada DNA via `CustomerService.EstimateAverage()`, renderiza label con nombre custom (o "MM") + " · " + precio + " D". Agrupa etiquetas CSS `npc-row` y `npc-name`, `price-row` para precio.

**Vinculado a:** [[Index/08 - UI System]]

**Conexiones:** [[StoreContainer]], [[CustomerService]], [[CreatureDNA]], [[MoriMochiAgent]]
