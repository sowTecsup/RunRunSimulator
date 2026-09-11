---
tags: [script, world, expedition, ui-overlay, visualization, static-utility]
---

# CuePathDrawer.cs

**Ruta:** `World/Expedition/CuePathDrawer.cs`

**Responsabilidad:** Utilidad estática para dibujar rutas de navegación suavizadas (Catmull-Rom) con destino pulsante. Antes vivía integrado en ArenaCueOverlay; ahora separado para enfocarse en rutas de agentes. Mantiene PathCueState por agente (alpha, corners, destino). **S115:** Firma de `Draw()` incluye parámetro `bool ownerVisible`; si dueño no está visible en viewport, ruta no se dibuja (hasValidPath exige ownerVisible).

## Struct PathCueState

```csharp
public class PathCueState
{
    public NavMeshAgent Nav;
    public Vector3 ShownEnd;                // Destino suavizado mostrado
    public bool HasShown;                   // Si ya inicializó ShownEnd
    public float Alpha;                     // Fade de ruta (0-1)
    public Vector3[] Corners;               // Esquinas de path actual
    public float DestAlpha;                 // Fade de marcador destino
    public Vector3 LastDestination;         // Último destino conocido
    public bool HasDestination;             // Bandera de inicialización
}
```

## Método Estático Principal

**Draw(CueStyleSO style, PathCueState state, Transform body, Color baseColor, float dt, bool ownerVisible) → void**

Dibuja ruta + marcador destino. Maneja:
1. **Validación de path (S115 ACTUALIZADO):**
   - `hasValidPath = nav.enabled && nav.isOnNavMesh && nav.hasPath && path.corners.Length ≥ 2 && ownerVisible`
   - **S115 NUEVO:** `ownerVisible` bloqueado si dueño fuera de viewport
   - Destino = última esquina

2. **Suavizado de destino (Lerp exponencial):**
   - Si hasValidPath: `ShownEnd = Lerp(ShownEnd, destination, 1 - Exp(-PathSmoothing * dt))`
   - Detecta cambio de destino: si distancia > 1m → DestAlpha = 0, recomienza fade

3. **Animación de alpha:**
   - Si hasValidPath: `Alpha → 1` (PathFadeSeconds)
   - Si no: `Alpha → 0` (se limpia HasShown/HasDestination cuando llega a 0)

4. **Dibujo de ruta (Catmull-Rom):**
   - Construye control points: virtualStart (forward * StartTangent), virtualEnd (proyectado)
   - Por cada segmento de path:
     - CatmullRom(p0, p1, p2, p3, t) — interpola suavemente
     - Muestrea CurveSamples puntos por segmento
     - En último segmento: dibuja Arrow() (punta)
     - En otros: DashedSegment() (dasheado que fluye a PathFlowSpeed)

5. **Marcador de destino:**
   - Dibuja disco en ShownEnd (pulsante, escala animated)
   - Desaparece si DestAlpha ≤ 0.01

## Interpolación Catmull-Rom

```csharp
Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
{
    float t2 = t * t;
    float t3 = t2 * t;
    return 0.5f * (
        2f * p1 +
        (-p0 + p2) * t +
        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
        (-p0 + 3f * p1 - 3f * p2 + p3) * t3
    );
}
```

Produce curva suave C2-continua (tercera derivada saltos en puntos de control, pero típicamente imperceptible).

## Parámetros de Estilo (CueStyleSO)

- `PathSmoothing` — exponente de Lerp para ShownEnd
- `PathFadeSeconds` — duración fade in/out de ruta
- `CurveSamples` — muestras por segmento Catmull-Rom (típicamente 8-12)
- `StartTangent` — extensión de control point inicial (forward * esta cantidad)
- `PathThickness` — grosor línea
- `PathDashLength`, `PathDashGap` — longitud de dashes
- `PathFlowSpeed` — velocidad de flujo de dashes (Time.time * esto)
- `PathTailAlpha` — alpha mínimo en cola de ruta
- `HeadLength`, `HeadWidth` — dimensiones de punta Arrow
- `DestMarkerRadius`, `DestPulseAmount`, `DestPulseSpeed` — pulsación de marcador
- `ReticleAppearScale` — escala inicial cuando aparece
- `HeightOffset` — elevación Y

## Cambios S115

**Firma de Draw() — NUEVO PARÁMETRO (línea 23):**
```csharp
public static void Draw(CueStyleSO style, PathCueState state, Transform body, Color baseColor, float dt, bool ownerVisible)
```
- Parámetro nuevo `bool ownerVisible` — indica si dueño está visible en viewport (S115 nuevo)
- Llamador (ArenaCueOverlay línea 130) pasa `OnScreen(controller.transform.position)`

**Validación de path — ACTUALIZADA (línea 27):**
```csharp
bool hasValidPath = nav != null && nav.enabled && nav.isOnNavMesh && nav.hasPath && nav.path.corners.Length >= 2 && ownerVisible;
```
- Agrega `&& ownerVisible` al final de cadena de validación
- Si `ownerVisible` es false: ruta NO se dibuja (incluso si path es válido)
- Contexto: criatura fuera de pantalla → ruta desvanece

**Impacto S115:**
- Rutas respetan viewport: solo visibles cuando dueño en encuadre
- Transición suave vía fade existente:
  - ownerVisible = false → hasValidPath = false → Alpha → 0 (PathFadeSeconds)
  - ownerVisible = true → hasValidPath = true (si path válido) → Alpha → 1 (PathFadeSeconds)
- Reduce clutter visual cuando hay múltiples criaturas
- La ruta se desvanece suavemente al salir de pantalla (no pop-out abrupto)

## Invariantes S102 + S115

- **Suavizado exponencial:** destino no salta, sigue suavemente
- **State por agente:** cada PathCueState es independiente (multiagente)
- **Ruta desaparece:** si nav invalid o sin path, alpha → 0 (fade out)
- **S115:** Ruta también desaparece si dueño fuera de viewport
- **Catmull-Rom:** pasa por p1 y p2, no por p0/p3 (control points tangentes)
- **Dashes fluyen:** offset Time-based crea efecto de movimiento
- **Último segmento Arrow:** punta indica dirección final
- **Pulso de destino:** sin(Time * speed) para ondulación visual
- **S115:** ownerVisible es booleano simple: on/off, no fade gradual (fade ocurre vía Alpha en Draw)

## Conexiones

- [[CueStyleSO]] (tuning)
- [[CueDrawer]] (Disc, Ring, Arrow, DashedSegment)
- [[ArenaCueOverlay]] (propietario, llama Draw en LateUpdate; **S115** pasa ownerVisible)
- [[MoriMochiAgent]] (proporciona NavMeshAgent + body.forward)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

