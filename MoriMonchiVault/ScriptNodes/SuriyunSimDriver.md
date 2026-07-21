---
tags: [script, prototype, sandbox]
---

# SuriyunSimDriver.cs

**Ruta:** `Prototype/SuriyunSimDriver.cs`

**Responsabilidad:** Paleta de diseño visual para el nuevo modelo Suriyun Dragons (pivot S56). `LoadBanks()` carga vía `Resources.Load()` tres bancos permanentes: fur patterns (MonchiFur_00-32, 33 materiales), faces (MonchiFace_01-25, 25 materiales), gems (5 materiales: Gold, Ruby, Emerald, Sapphire, Amethyst). `CollectDragons()` recolecta todos los GameObjects raíz con nombre "Dragon_*" de la escena. `ApplyLook(dragon, shiny)` aplica a cada dragón: (1) patrón de fur aleatorio; (2) color base aleatorio en rango natural (H full, S 0.28–0.56, V 0.78–0.96); (3) armonía de color derivada vía `BuildHarmony()` que genera dos colores secundarios (alas/acento) según roll: 40% análoga (±28° hue + sat/val modulados), 30% monro (sat×0.5 wings / sat×1.15 accent), 20% triádica (120°+240°), 10% complementaria (180°+150°); (4) tinte vía `MaterialPropertyBlock` con `ColorGenetics.BuildFurPalette()` + 4 PropertyIDs UTS (`_BaseColor`, `_1st_ShadeColor`, `_2nd_ShadeColor`, `_RimLightColor`); (5) si `shiny=true`, reemplaza material con gema completa sin tinte; (6) caras (Face renderer) usan material face bank sin tinte. `Update()` cicla animaciones: cada dragón elige estado aleatorio de array Estados (IdleA, Walk/Run/Jump/Eat/Rest, Roar/Fire/Sick/Yes/No/Damage) vía `Animator.CrossFade()` cada 2–5 segundos, con 50% probabilidad de swapear cara simultáneamente. **Knobs serializados:** `seed` (4242 reproducible), `randomizeEachPlay` (sistema.Random sin seed si true), `shinyCount` (2 dragones brillan), `cycleSeconds` (2–5s rango), `saturationRange` (0.28–0.56), `valueRange` (0.78–0.96). **Escena:** `Assets/Scenes/SuriyunSimTest.unity`, GameObject `__SimSetup`. **Contexto:** Sandbox independiente; valida colorimetría y animaciones del asset Suriyun Dragons antes de integración; NO participa del gameplay.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[ColorGenetics]]
