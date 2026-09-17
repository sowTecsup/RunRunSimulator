# MiniShapes

Formas vectoriales SDF en modo inmediato para URP (Unity 6): anillos, discos, líneas, flechas, arcos, sectores, cápsulas y punteados en el plano XZ, más rutas curvas animadas. Sin GameObjects, con antialiasing, degradados y blend aditivo.

```csharp
using Sowtank.MiniShapes;

void LateUpdate()
{
    ShapeDraw.DashedRing(transform.position, 2f, 0.06f, 24, 0.55f, Time.time, Color.cyan);
}
```

- **Instalar:** copiar esta carpeta a `Packages/` del proyecto (o Package Manager → *Install package from disk*).
- **Demo:** Package Manager → MiniShapes → Samples → Demo.
- **Guía completa** (API, vara de calidad, cómo crear formas nuevas): [`Documentation~/MiniShapes.md`](Documentation~/MiniShapes.md).

## Estructura

```
Runtime/ShapeDraw.cs          dibujante de formas (API pública)
Runtime/ShapeId.cs            ids de forma (contrato con el shader)
Runtime/PathDrawer.cs         rutas Catmull-Rom punteadas que fluyen
Runtime/PathStyle.cs          ajustes de ruta (serializable)
Runtime/PathState.cs          estado por dueño de ruta
Runtime/Resources/MiniShapes  materiales por defecto (normal, aditivo, detrás)
Shaders/MiniShape.shader      shader URP "MiniShapes/Shape"
Shaders/MiniShapeSDF.hlsl     una función SDF por forma
Samples~/Demo                 componente que dibuja todo
```
