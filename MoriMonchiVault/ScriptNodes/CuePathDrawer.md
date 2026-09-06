---
tags: [script, world, expedition, ui-overlay, visualization, static-utility]
---

# CuePathDrawer.cs

**Ruta:** `World/Expedition/CuePathDrawer.cs`

**Responsabilidad:** Utilidad estática para dibujar rutas de navegación suavizadas (Catmull-Rom) con destino pulsante. Antes vivía integrado en ArenaCueOverlay; ahora separado para enfocarse en rutas de agentes. Mantiene PathCueState por agente (alpha, corners, destino).

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

**Draw(CueStyleSO style, PathCueState state, Transform body, Color baseColor, float dt) → void**

Dibuja ruta + marcador destino. Maneja:
1. **Validación de path:**
   - `hasValidPath = nav.enabled && nav.isOnNavMesh && nav.hasPath && path.corners.Length ≥ 2`
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

## Invariantes S102

- **Suavizado exponencial:** destino no salta, sigue suavemente
- **State por agente:** cada PathCueState es independiente (multiagente)
- **Ruta desaparece:** si nav invalid o sin path, alpha → 0 (fade out)
- **Catmull-Rom:** pasa por p1 y p2, no por p0/p3 (control points tangentes)
- **Dashes fluyen:** offset Time-based crea efecto de movimiento
- **Último segmento Arrow:** punta indica dirección final
- **Pulso de destino:** sin(Time * speed) para ondulación visual

## Conexiones

- [[CueStyleSO]] (tuning)
- [[CueDrawer]] (Disc, Ring, Arrow, DashedSegment)
- [[ArenaCueOverlay]] (propietario, llama Draw en LateUpdate)
- [[MoriMochiAgent]] (proporciona NavMeshAgent + body.forward)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
