# MiniShapes — Guía completa

Formas vectoriales en **modo inmediato** para URP, inspiradas en el asset *Shapes* de Freya Holmér. Sin GameObjects: cada llamada dibuja un quad en el mundo y el shader recorta la forma con una **SDF** (distancia con signo) antialiasada por `fwidth`.

- Plano: **XZ** (guías de suelo). La altura sale de la `y` del centro o del punto A.
- Unidades: **metros** en el mundo. Ángulos en **radianes**, 0 = +X, crece hacia +Z (`Mathf.Atan2(dir.z, dir.x)`).
- Requisitos: Unity 6, URP 17.

---

## 1 · Instalar en otro proyecto

**Opción A — copiar la carpeta (lo más simple):** copiar `com.sowtank.minishapes/` entera dentro de `Packages/` del otro proyecto. Unity la toma como paquete embebido.

**Opción B — desde disco:** Package Manager → `+` → *Install package from disk…* → elegir `package.json`.

**Opción C — git:** subir la carpeta a un repo propio y en Package Manager → *Install package from git URL…* pegar la URL.

Demo: Package Manager → MiniShapes → *Samples* → **Import** en *Demo*; poner `MiniShapesDemo` en un GameObject vacío y dar Play.

---

## 2 · Uso rápido

```csharp
using Sowtank.MiniShapes;
using UnityEngine;

public class MiGuia : MonoBehaviour
{
    void LateUpdate()
    {
        Vector3 c = transform.position + Vector3.up * 0.03f;
        ShapeDraw.DashedRing(c, 2f, 0.06f, 24, 0.55f, Time.time * 0.5f, Color.cyan);
        ShapeDraw.Arrow(c, c + transform.forward * 3f, 0.08f, 0.5f, 0.4f, new Color(1,1,1,0.1f), Color.white);
    }
}
```

- Llamar **cada frame** (en `LateUpdate`, después de mover todo). Lo que no se dibuja ese frame desaparece.
- Los materiales se cargan solos desde `Resources/MiniShapes/`. `ShapeDraw.Configure(...)` los reemplaza por los tuyos.
- `additive: true` usa el material aditivo (resaltes, brillos).
- `ShapeDraw.AlphaScale` multiplica el alfa de todo lo que se dibuje después (para apagar un grupo de guías). Volverlo a 1 al terminar.
- `ShapeDraw.DrawBehind = true` dibuja con el material *Back* (cola 3090) para que quede detrás del resto de las guías. Volverlo a `false`.

---

## 3 · Catálogo de formas

| Id | Método | Qué es | Degradado |
|---|---|---|---|
| 0 | `Ring(center, radius, thickness, color)` | anillo | — |
| 1 | `Disc(center, radius, color[, innerAlpha, outerAlpha])` | disco | radial de alfa |
| 2 | `Segment(a, b, thickness, colorA[, colorB])` | línea con puntas redondas | A → B |
| 2 | `Capsule(a, b, radius, color, innerAlpha, outerAlpha)` | cápsula rellena | eje → borde |
| 3 | `Arrow(a, b, thickness, headLength, headWidth, colorA[, colorB])` | flecha | cola → punta |
| 4 | `DashedRing(center, radius, thickness, dashCount, dashRatio, rotation, color)` | anillo punteado | — |
| 5 | `Arc(center, radius, thickness, startAngle, sweep, colorA, colorB)` | arco con puntas redondas | angular |
| 6 | `DashedSegment(a, b, thickness, dashLength, dashGap, dashOffset, colorA, colorB)` | línea punteada | A → B |
| 7 | `Sector(center, radius, startAngle, sweep, color, innerAlpha, outerAlpha)` | pie / cono de visión | radial de alfa |
| 8 | `DashedArc(center, radius, thickness, startAngle, sweep, dashCount, dashRatio, rotation, colorA, colorB)` | arco punteado | angular |
| 9 | `CapsuleOutline(a, b, radius, thickness, colorA, colorB)` | contorno de cápsula | A → B |

**Rutas:** `PathDrawer.Draw(points, startForward, style, state, color, dt)` dibuja una curva Catmull-Rom punteada que fluye hacia el destino, con alfa cola → cabeza, flecha al final y marcador pulsante. `points[0]` es la posición actual y el último, el destino (por ejemplo `NavMeshAgent.path.corners` con la posición del agente al inicio). Guardar un `PathState` por dueño; `PathStyle` es serializable para ajustarlo en el Inspector.

---

## 4 · Materiales

Los tres usan el shader `MiniShapes/Shape`, alfa **premultiplicado**:

| Material | Blend (Src/Dst) | Cola | Uso |
|---|---|---|---|
| `MiniShape` | One / OneMinusSrcAlpha | 3100 | guías normales |
| `MiniShapeAdditive` | One / One | 3100 | resaltes |
| `MiniShapeBack` | One / OneMinusSrcAlpha | 3090 | lo que va detrás |

Propiedades por material: `_ZTest` (Always por defecto: la guía se ve encima del suelo aunque tenga relieve; poner LessEqual si debe ocultarse detrás de objetos) y `_StencilRef` / `_StencilComp` (Always por defecto; sirve para que otro shader "enmascare" las guías, por ejemplo no dibujarlas encima de personajes que escriben stencil 1 → Ref 1, Comp NotEqual).

---

## 5 · La vara de calidad (vocabulario Shapes)

Reglas que hacen que una guía se vea profesional sin que nadie lo pida:

1. **Puntas redondas** en líneas, arcos y dashes. Nada termina en corte seco.
2. **Grosor en metros** para lo que vive en el suelo (se achica con la distancia como el mundo).
3. **Dash que fluye:** animar `dashOffset` (líneas) o `rotation` (anillos) en la dirección del movimiento. Una ruta quieta parece rota.
4. **Degradados con intención:** líneas cola (transparente) → cabeza (opaca); discos y conos centro → borde; arcos medidores con degradado angular.
5. **Blend según rol:** transparente para lo ambiental, **aditivo** para lo activo o urgente.
6. **Nada aparece de golpe:** entrada con escala 0,85 → 1 (o 1,4 → 1 para marcadores) y alfa en ~0,25 s; salida con alfa.
7. **Opaco antes que transparente**, y lo de fondo con `DrawBehind`.
8. **Alfas bajos** para el ambiente (0,15–0,35), altos solo para lo que el jugador debe leer ya.
9. **Un color = un significado.** Definir la paleta antes de dibujar.

Patrón de animación de entrada/salida:

```csharp
float appear;
void LateUpdate()
{
    appear = Mathf.MoveTowards(appear, visible ? 1f : 0f, Time.deltaTime / 0.25f);
    if (appear <= 0f) return;
    float e = Mathf.SmoothStep(0f, 1f, appear);
    float radius = baseRadius * Mathf.Lerp(0.85f, 1f, e);
    Color c = color; c.a *= e;
    ShapeDraw.Ring(center, radius, 0.05f, c);
}
```

---

## 6 · Cómo funciona por dentro

1. `ShapeDraw.X(...)` llena un `MaterialPropertyBlock` con `_Shape` (el id) y los parámetros de la forma.
2. Calcula un quad en XZ que **envuelve** la forma (centro + escala con margen por grosor) y llama `Graphics.RenderMesh`.
3. El vertex shader pasa la posición de mundo; el fragment toma `p = positionWS.xz`.
4. Según `_Shape`, una función de `MiniShapeSDF.hlsl` escribe:
   - `d` — distancia con signo al borde (negativa adentro, en metros).
   - `t` — 0..1 para el degradado `_Color → _ColorB` (a lo largo, o angular).
   - `radialT` — 0..1 para el degradado de alfa `_InnerAlpha → _OuterAlpha`.
5. Cobertura: `aa = fwidth(d)`, `coverage = 1 - smoothstep(-aa, aa, d)`, `clip` si es ~0. Color final premultiplicado.

La única regla de oro: **todo es una distancia**. Si sabes escribir la distancia a tu forma, tienes la forma con antialiasing, grosor, degradados y blend gratis.

---

## 7 · Crear una forma nueva (paso a paso)

Ejemplo: **Rectángulo redondeado** (`RoundedRect`), id **10**, con centro, tamaño, radio de esquina y rotación.

### Paso 1 — Elegir el id y los parámetros

- Id siguiente libre: `10`. Agregarlo a `ShapeId`:
  ```csharp
  public const int RoundedRect = 10;
  ```
- Reutilizar propiedades existentes antes de crear nuevas: `_Center` (centro), `_Radius` (radio de esquina), `_Thickness` (0 = relleno, >0 = contorno), `_Rotation` (radianes). Solo falta el tamaño → usar `_PointA.xz` como *medio tamaño* (half extents). Si de verdad hace falta una propiedad nueva, ver Paso 4.

### Paso 2 — Escribir la SDF en `Shaders/MiniShapeSDF.hlsl`

```hlsl
void ShapeRoundedRect(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 q = p - _Center.xz;
    float c = cos(-_Rotation), s = sin(-_Rotation);
    q = float2(c * q.x - s * q.y, s * q.x + c * q.y);
    float2 halfSize = _PointA.xz;
    float2 k = abs(q) - halfSize + _Radius;
    float box = length(max(k, 0.0)) + min(max(k.x, k.y), 0.0) - _Radius;
    d = _Thickness > 0 ? abs(box) - _Thickness * 0.5 : box;
    t = saturate(q.x / max(halfSize.x * 2.0, 1e-5) + 0.5);
    radialT = saturate(length(q / max(halfSize, 1e-5)));
}
```

Reglas de la SDF:
- `d` en **metros**, negativa adentro. Para convertir un relleno en contorno: `abs(d) - grosor/2`.
- Puntas redondas = usar `length(...)` hacia el extremo (ver `SdCapsule`).
- Unión de dos formas = `min(d1, d2)`; intersección = `max(d1, d2)`; resta = `max(d1, -d2)` (así está hecho `DashedArc`: arco ∩ dashes).
- Punteado angular: `frac(ángulo / período)`; punteado lineal: `frac((u - offset) / período)` (copiar de `ShapeDashedRing` / `ShapeDashedSegment`).
- Evitar divisiones por cero con `max(x, 1e-5)`.
- Referencia de SDFs 2D listas para copiar: *Inigo Quilez — 2D distance functions* (iquilezles.org).

### Paso 3 — Enchufarla en `Shaders/MiniShape.shader`

En la cadena del `Frag`, agregar una rama **al final** (después de la de `_Shape < 9.5`):

```hlsl
else if (_Shape < 10.5) ShapeRoundedRect(p, d, t, radialT);
```

(El orden importa: la cadena compara `_Shape < N + 0.5` de menor a mayor, así que cada forma nueva va siempre al final.)

### Paso 4 — (Solo si hace falta) propiedad nueva

1. En `Properties`: `_Size ("Size", Vector) = (1,1,0,0)`.
2. En el `CBUFFER_START(UnityPerMaterial)`: `float4 _Size;` (**misma** declaración en todos los pases; si falta, se rompe el SRP Batcher).
3. En `ShapeDraw`: `private static readonly int SizeID = Shader.PropertyToID("_Size");`.

### Paso 5 — El método C# en `Runtime/ShapeDraw.cs`

```csharp
public static void RoundedRect(Vector3 center, Vector2 size, float cornerRadius, float rotation, float thickness, Color colorA, Color colorB, float innerAlpha = 1f, float outerAlpha = 1f, bool additive = false)
{
    Material mat = Pick(additive);
    if (mat == null) return;
    EnsureResources();

    colorA.a *= AlphaScale;
    colorB.a *= AlphaScale;

    mpb.Clear();
    mpb.SetColor(ColorID, colorA);
    mpb.SetColor(ColorBID, colorB);
    mpb.SetFloat(InnerAlphaID, innerAlpha);
    mpb.SetFloat(OuterAlphaID, outerAlpha);
    mpb.SetFloat(ShapeID, ShapeId.RoundedRect);
    mpb.SetVector(CenterID, center);
    mpb.SetVector(PointAID, new Vector3(size.x * 0.5f, 0f, size.y * 0.5f));
    mpb.SetFloat(RadiusID, Mathf.Min(cornerRadius, Mathf.Min(size.x, size.y) * 0.5f));
    mpb.SetFloat(RotationID, rotation);
    mpb.SetFloat(ThicknessID, thickness);

    float extent = size.magnitude * 0.5f + thickness;
    Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
}
```

Checklist del método:
- **Siempre** `mpb.Clear()` y setear `_Color`, `_ColorB`, `_InnerAlpha`, `_OuterAlpha` (si no, hereda basura de la forma anterior).
- **Siempre** multiplicar alfas por `AlphaScale`.
- **Quad envolvente:** tiene que cubrir la forma **más** el grosor y el antialiasing. Si la forma rota, usar el radio que la contiene (`size.magnitude / 2`). Quad chico = forma recortada; quad enorme = overdraw.
- Firmas consistentes con el resto: posiciones primero, medidas, ángulos, colores, alfas, `additive` al final.

### Paso 6 — Probar

1. Agregar una línea a `MiniShapesDemo` para la forma nueva.
2. Play: revisar bordes suaves a distintas distancias de cámara, grosor 0 y >0, rotación, degradados, y que la forma no se corte en el borde del quad.
3. Consola sin errores de shader.
4. Actualizar la tabla del catálogo (§3) y `CHANGELOG.md`.

### Ideas de formas siguientes

| Forma | Truco SDF |
|---|---|
| Polígono regular (hexágono de celda) | SDF de n-gono de Quilez |
| Cruz / "X" de objetivo | `min` de dos cápsulas rotadas |
| Retícula de 4 arcos | llamar `Arc` 4 veces (no hace falta shader) |
| Arco con flecha | `min(arco, triángulo)` en el extremo |
| Barra de progreso circular | `Arc` con `sweep = valor * 2π` y degradado angular |
| Estrella | SDF de estrella de Quilez |

Regla práctica: **si se puede componer con llamadas existentes, componer en C#**; solo bajar al shader cuando la forma necesita ser una sola superficie (sin superposición de alfa) o cuesta demasiadas llamadas.

---

## 8 · Límites conocidos

- Solo plano XZ (horizontal). Para guías verticales o en pantalla haría falta rotar el quad y la base de `p`.
- Una draw call por forma (sin instancing). Cientos por frame está bien; miles, conviene agrupar.
- Solo URP (usa `Core.hlsl` de URP y la pasada `UniversalForward`).
- Sin texto.
