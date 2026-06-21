---
tags: [archive]
---

# Fur 02 — Speckled (Shader Graph · URP)

> Objetivo visual: miles de manchitas diminutas de color sobre un color base legible.
> Aspecto biológico, transiciones suaves (no manchas duras).
> Construye sobre la base del [[FUR_01_Smooth]] (reusa el bloque de fresnel).

---

## ⚠️ Regla de oro (igual que siempre)

La propiedad del color base **debe llamarse `_BaseColor`** (Reference exacto).
Ahí entra el color genético. Las manchitas son un color aparte (`_SpeckleColor`).

---

## 0. Crear el asset

1. `Assets/RunRunSimulator/Resources/Shaders/` → click derecho →
   **Create → Shader Graph → URP → Lit Shader Graph**.
2. Nómbralo `Fur_02_Speckled`.
3. Doble click para abrir.

> Atajo: podés **duplicar** `Fur_01_Smooth` y trabajar desde ahí; ya trae el `_BaseColor`
> y el bloque de fresnel listos. Si lo hacés, saltea el Bloque B de abajo.

---

## 1. Propiedades (Blackboard, botón `+`)

Pon el **Reference** EXACTO de cada una en el Graph Inspector.

| # | Tipo      | Nombre (Display)  | Reference          | Default        | Notas |
|---|-----------|-------------------|--------------------|----------------|-------|
| 1 | **Color** | Base Color        | `_BaseColor`       | blanco         | ⚠️ color genético. HDR off. |
| 2 | **Color** | Speckle Color     | `_SpeckleColor`    | gris oscuro    | Color de las manchitas. |
| 3 | **Float** | Speckle Scale     | `_SpeckleScale`    | `220`          | Slider 50 → 600. Más alto = manchas más chicas y numerosas. |
| 4 | **Float** | Speckle Amount    | `_SpeckleAmount`   | `0.35`         | Slider 0 → 1. Cuántas manchas (cobertura). |
| 5 | **Float** | Speckle Softness  | `_SpeckleSoftness` | `0.15`         | Slider 0.01 → 0.5. Suavidad del borde de cada mancha. |
| 6 | **Color** | Rim Color         | `_RimColor`        | blanco         | Borde iluminado. |
| 7 | **Float** | Rim Power         | `_RimPower`        | `4`            | Slider 1 → 8. |
| 8 | **Float** | Rim Strength      | `_RimStrength`     | `0.3`          | Slider 0 → 1. |
| 9 | **Float** | Smoothness        | `_Smoothness`      | `0.3`          | Slider 0 → 1. |

---

## 2. Bloque A — Las manchitas (Noise → máscara suave → Lerp)

Idea: ruido muy fino convertido en una máscara de manchas con bordes suaves, y
mezclamos el color base con el color de mancha usando esa máscara.

1. Arrastra **UV** al lienzo (`UV`, canal `UV0`).
2. Crea **Multiply** (`Multiply`).
   - `UV.Out` → `Multiply.A`
   - arrastra **Speckle Scale** (`_SpeckleScale`) → `Multiply.B`
3. Crea **Simple Noise** (`Simple Noise`).
   - `Multiply.Out` → `Simple Noise.UV`
   - *(deja el `Scale` interno en 1 — el tamaño lo manda `_SpeckleScale`)*

Ahora convertimos ese ruido en manchas con borde suave usando **Smoothstep**:

4. Crea **One Minus** (`One Minus`).
   - arrastra **Speckle Amount** (`_SpeckleAmount`) → `One Minus.In`
   - *(invertimos para que "más Amount" = más manchas, no menos)*
   - **→ a este resultado lo llamamos `edge1`.**
5. Crea **Add** (`Add`).
   - `edge1` (One Minus.Out) → `Add.A`
   - arrastra **Speckle Softness** (`_SpeckleSoftness`) → `Add.B`
   - **→ a este resultado lo llamamos `edge2`.**
6. Crea **Smoothstep** (`Smoothstep`).
   - `edge1` (One Minus.Out) → `Smoothstep.Edge1`
   - `edge2` (Add.Out)       → `Smoothstep.Edge2`
   - `Simple Noise.Out`      → `Smoothstep.In`
   - **→ esta salida es la MÁSCARA de manchas (0 = base, 1 = mancha, suave en el borde). La llamamos `speckleMask`.**

Mezclamos los dos colores con la máscara:

7. Crea **Lerp** (`Lerp`).
   - arrastra **Base Color** (`_BaseColor`)    → `Lerp.A`
   - arrastra **Speckle Color** (`_SpeckleColor`) → `Lerp.B`
   - `speckleMask` (Smoothstep.Out)            → `Lerp.T`
   - **→ esta salida es el COLOR CON MANCHAS. La llamamos `colorSpeck`.**

---

## 3. Bloque B — Fresnel suave (idéntico al Smooth)

> Si duplicaste `Fur_01_Smooth`, este bloque ya existe — saltealo.

1. Crea **Fresnel Effect** (`Fresnel Effect`).
   - arrastra **Rim Power** (`_RimPower`) → `Fresnel Effect.Power`
2. Crea **Multiply** (`Multiply`).
   - `Fresnel Effect.Out` → `Multiply.A`
   - arrastra **Rim Strength** (`_RimStrength`) → `Multiply.B`
   - **→ `rimMask`.**
3. Crea **Multiply** (`Multiply`).
   - `rimMask` → `Multiply.A`
   - arrastra **Rim Color** (`_RimColor`) → `Multiply.B`
   - **→ `rimLit`.**

---

## 4. Bloque C — Combinar y volcar al Master

1. Crea **Add** (`Add`).
   - `colorSpeck` (Bloque A, paso 2.7) → `Add.A`
   - `rimLit` (Bloque B, paso 3.3)     → `Add.B`
2. Conecta al **Master Stack → Fragment**:
   - `Add.Out` → **Base Color**
   - arrastra **Smoothness** (`_Smoothness`) → **Smoothness**
   - **Metallic** = `0`
   - **Emission** = sin conectar
   - **Alpha** = `1`

---

## 5. Guardar y aplicar

1. **Save Asset** (arriba a la izquierda).
2. Crea/reusa un material con shader `Shader Graphs/Fur_02_Speckled`.
3. Asignalo al body renderer del MoriMochi de prueba.
4. Play: el color genético entra por `_BaseColor`; las manchas usan `_SpeckleColor`.

---

## ✅ Checklist de validación

- [ ] El color base sigue siendo **legible** (las manchas no lo tapan).
- [ ] Se ven **muchas manchitas diminutas**, no pocas grandes (subí `_SpeckleScale` si están grandes).
- [ ] Subir **Speckle Amount** agrega más cobertura de manchas; bajarlo las reduce.
- [ ] Los bordes de cada mancha son **suaves**, no recortados (controlado por `_SpeckleSoftness`).
- [ ] Cambiar `_BaseColor` recolorea el fondo; cambiar `_SpeckleColor` recolorea las manchas.
- [ ] El fresnel del borde sigue funcionando como en el Smooth.

> Si se ve bien, marcá ✅ en [[00 - Fur Shaders Index]] y vamos por **Fur 03 — Patchwork**.

---

## Mapa de nodos (resumen visual)

```
UV ─► Multiply ─► Simple Noise ─────────────────────────► Smoothstep ─► (speckleMask) ─┐
        ▲(_SpeckleScale)                                   ▲    ▲                       │
                                                           │    │                       │
              _SpeckleAmount ─► One Minus ─►(edge1)────────┘    │                       ▼
                                     └────► Add ─►(edge2)───────┘        _BaseColor ─► Lerp.A
                                            ▲(_SpeckleSoftness)        _SpeckleColor ─► Lerp.B
                                                                       (speckleMask) ─► Lerp.T
                                                                                         │
                                                                                  (colorSpeck)
                                                                                         ▼
Fresnel Effect ─► Multiply ─►(rimMask)─► Multiply ─►(rimLit) ─────────────────────► Add ─► Base Color
   ▲(_RimPower)    ▲(_RimStrength)          ▲(_RimColor)                            ▲
                                                                          Smoothness ─► Smoothness
                                                                          Metallic = 0
```
