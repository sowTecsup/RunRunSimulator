---
tags: [archive]
---

# Fur 01 — Smooth (Shader Graph · URP)

> Objetivo visual: color uniforme, variación mínima, fresnel suave, aspecto de juguete nuevo.
> Proyecto: URP 17.3 (Unity 6). Este es el shader **base** sobre el que se construyen los otros 6.

---

## ⚠️ Regla de oro (no la rompas)

La propiedad del color **debe llamarse `_BaseColor`** (con ese Reference exacto).
Tu código (`MoriMonchiVisualizer.ApplyColor` y `FurRenderer.SetProps`) inyecta el color
genético en `_BaseColor`. Si le pones otro nombre, la criatura sale gris/negra.

---

## 0. Crear el asset

1. En `Assets/RunRunSimulator/Resources/Shaders/`, click derecho →
   **Create → Shader Graph → URP → Lit Shader Graph**.
2. Nómbralo `Fur_01_Smooth`.
3. Doble click para abrir el editor de Shader Graph.

---

## 1. Propiedades (panel Blackboard, esquina sup. izquierda — botón `+`)

Crea estas propiedades. Para cada una, tras crearla, abre el **Node Settings** (panel
Graph Inspector) y pon el campo **Reference** EXACTO como se indica.

| # | Tipo      | Nombre (Display) | Reference        | Default        | Notas |
|---|-----------|------------------|------------------|----------------|-------|
| 1 | **Color** | Base Color       | `_BaseColor`     | blanco         | ⚠️ el color genético entra acá. Modo: **HDR** off. |
| 2 | **Float** | Variation        | `_Variation`     | `0.04`         | Slider 0 → 0.2. Variación de brillo (muy baja). |
| 3 | **Float** | Noise Scale      | `_NoiseScale`    | `60`           | Slider 1 → 200. Tamaño del grano. |
| 4 | **Color** | Rim Color        | `_RimColor`      | blanco         | Color del borde iluminado. |
| 5 | **Float** | Rim Power        | `_RimPower`      | `4`            | Slider 1 → 8. Más alto = borde más fino. |
| 6 | **Float** | Rim Strength     | `_RimStrength`   | `0.3`          | Slider 0 → 1. Intensidad del fresnel. |
| 7 | **Float** | Smoothness       | `_Smoothness`    | `0.3`          | Slider 0 → 1. |

> Arrastra cada propiedad desde el Blackboard al lienzo cuando el paso lo pida.

---

## 2. Bloque A — Variación sutil de color (el "casi uniforme")

Idea: un ruido finísimo que sube/baja apenas el brillo del color base. Apenas perceptible.

1. Arrastra **UV** al lienzo (nodo `UV`, canal `UV0`).
2. Crea **Multiply** (`Multiply`).
   - `UV.Out` → `Multiply.A`
   - arrastra **Noise Scale** (`_NoiseScale`) → `Multiply.B`
3. Crea **Simple Noise** (`Simple Noise`).
   - `Multiply.Out` → `Simple Noise.UV`
   - (deja `Scale` interno en 1; el escalado lo controla tu propiedad)
4. Crea **Remap** (`Remap`).
   - `Simple Noise.Out` → `Remap.In`
   - `In Min Max` = `(0, 1)`
   - `Out Min Max` = `(-1, 1)`
5. Crea **Multiply** (segundo).
   - `Remap.Out` → `Multiply.A`
   - arrastra **Variation** (`_Variation`) → `Multiply.B`
   - *(resultado: un valor diminuto, ej. ±0.04)*
6. Crea **Add** (`Add`).
   - `Multiply.Out` (paso 5) → `Add.A`
   - constante `1` → `Add.B`  *(crea un nodo `Float` con valor 1, o escribe 1 en el puerto)*
   - *(resultado: un multiplicador cercano a 1, ej. 0.96–1.04)*
7. Crea **Multiply** (tercero).
   - arrastra **Base Color** (`_BaseColor`) → `Multiply.A`
   - `Add.Out` (paso 6) → `Multiply.B`
   - **→ esta salida es el COLOR BASE CON VARIACIÓN. La llamaremos `colorVar`.**

---

## 3. Bloque B — Fresnel suave (rim de juguete nuevo)

1. Crea **Fresnel Effect** (`Fresnel Effect`).
   - arrastra **Rim Power** (`_RimPower`) → `Fresnel Effect.Power`
   - *(deja Normal y View Dir por defecto)*
2. Crea **Multiply** (`Multiply`).
   - `Fresnel Effect.Out` → `Multiply.A`
   - arrastra **Rim Strength** (`_RimStrength`) → `Multiply.B`
   - **→ esta es la MÁSCARA de rim (un escalar 0..1). La llamaremos `rimMask`.**
3. Crea **Multiply** (`Multiply`).
   - `rimMask` (salida del paso 2) → `Multiply.A`
   - arrastra **Rim Color** (`_RimColor`) → `Multiply.B`
   - **→ esta es la LUZ DE BORDE coloreada. La llamaremos `rimLit`.**

---

## 4. Bloque C — Combinar y volcar al Master

1. Crea **Add** (`Add`).
   - `colorVar` (salida bloque A, paso 2.7) → `Add.A`
   - `rimLit` (salida bloque B, paso 3.3) → `Add.B`
   - **→ esta es la salida final de color.**
2. Conecta al **Master Stack → Fragment**:
   - `Add.Out` → **Base Color**
   - arrastra **Smoothness** (`_Smoothness`) → **Smoothness**
   - **Metallic** = `0` (escribe 0 en el puerto o déjalo en 0)
   - **Emission** = sin conectar
   - **Alpha** = `1` (déjalo por defecto)

---

## 5. Guardar y aplicar

1. Arriba a la izquierda del editor → **Save Asset**.
2. Crea un material: click derecho sobre `Fur_01_Smooth` → **Create → Material**
   (o usa el `.mat` existente y cámbiale el shader a `Shader Graphs/Fur_01_Smooth`).
3. Asigna ese material al **body renderer** del MoriMochi de prueba.
4. Play: el color genético debe entrar por `_BaseColor` automáticamente vía tu código.

---

## ✅ Checklist de validación (qué deberías ver)

- [x] La superficie tiene **un solo color sólido** (el de `_BaseColor`).
- [x] Si subís **Variation** a tope (~0.2) aparece un grano finísimo; en 0.04 casi no se nota.
- [x] Al girar la cámara, el **borde** se ilumina suave (fresnel). Subir **Rim Strength** lo hace más obvio.
- [x] Cambiar `_BaseColor` en el material recolorea TODO de forma uniforme.
- [x] En Play, cada criatura toma su color genético sin tocar el material a mano.

> Si esto se ve bien y el flujo del MD fue claro, seguimos con **Fur 02 — Speckled**.

---

## Mapa de nodos (resumen visual)

```
UV ─► Multiply ─► Simple Noise ─► Remap(0,1→-1,1) ─► Multiply ─► Add(+1) ─┐
        ▲(_NoiseScale)                                   ▲(_Variation)     │
                                                                           ▼
                                            _BaseColor ──────────────► Multiply ─► (colorVar) ─┐
                                                                                               ▼
Fresnel Effect ─► Multiply ─► (rimMask) ─► Multiply ─► (rimLit) ──────────────────────► Add ─► Base Color
   ▲(_RimPower)     ▲(_RimStrength)            ▲(_RimColor)                              ▲
                                                                                Smoothness ─► Smoothness
                                                                                Metallic = 0
```
