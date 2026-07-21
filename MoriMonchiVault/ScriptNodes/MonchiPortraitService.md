---
tags: [script, ui, service, singleton, graphics]
---

# MonchiPortraitService.cs

**Ruta:** `UI/MonchiPortraitService.cs`

**Responsabilidad:** Singleton "fotomatón" estudio oculto. Renderiza capturas 2D de MoriMonchis (full-body retrato + **S58 headshot lateral**). Cachea Texture2D/Sprite por UniqueID/ToStringID. Pipeline: Assemble visual, SetMood, pose Idle, encuadre automático por Bounds, RenderTexture + ReadPixels.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetPortrait(CreatureDNA dna)` | `Texture2D` | Retrato full-body (caché) |
| `GetPortraitSprite(CreatureDNA dna)` | `Sprite` | Sprite full-body (caché) |
| `GetHeadshot(CreatureDNA dna)` | `Texture2D` | **S58** Cabeza lateral 512×192 (caché) |
| `GetHeadshotSprite(CreatureDNA dna)` | `Sprite` | **S58** Sprite cabeza (caché) |

## Campos Serializados (S58)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `visualBank` | `MonchiVisualBankSO` | - | Visual bank Suriyun |
| `furTypeDatabase` | `FurTypeDatabaseSO` | - | Fur types |
| `boothVisualizer` | `MonchiVisualizer` | - | Visualizador |
| `boothCamera` | `Camera` | - | Cámara capture |
| `boothRoot` | `GameObject` | - | Raíz booth (desactivada) |
| `textureSize` | `int` | 384 | Tamaño retrato |
| `framePadding` | `float` | 1.15 | Padding retrato |
| `cameraPitch` | `float` | 12 | Pitch retrato |
| `cameraYaw` | `float` | 180 | Yaw retrato (frontal) |
| `portraitMood` | `MonchiMood` | Neutral | Expresión |
| `headshotWidth` | `int` | 512 | **S58** Ancho headshot |
| `headshotHeight` | `int` | 192 | **S58** Alto headshot |
| `headshotPadding` | `float` | 1.05 | **S58** Padding headshot |
| `headshotTopFraction` | `float` | 0.35 | **S58** Franja altura (35% bounds) |
| `headshotCenterHeight` | `float` | 0.58 | **S58** Centro vertical (58% arriba) |
| `headshotPitch` | `float` | 2 | **S58** Pitch headshot |
| `headshotYaw` | `float` | 140 | **S58** Yaw 140° (3/4 lateral) |
| `headshotRoll` | `float` | 0 | **S58** Roll (sin inclinación) |

## Campos Privados (S58)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cache` | `Dictionary<string, Texture2D>` | Caché retrato full-body |
| `spriteCache` | `Dictionary<string, Sprite>` | Caché sprite retrato |
| `headshotCache` | `Dictionary<string, Texture2D>` | **S58** Caché headshot |
| `headshotSpriteCache` | `Dictionary<string, Sprite>` | **S58** Caché sprite headshot |
| `rt` | `RenderTexture` | RenderTexture retrato (384×384) |
| `headshotRt` | `RenderTexture` | **S58** RenderTexture headshot (512×192) |

## Cambios S58

**Nuevos métodos GetHeadshot/GetHeadshotSprite:**
- Capturan franja de cabeza (altura TopFraction 35% de bounds)
- Centro vertical a CenterHeight (58% arriba del min.y)
- Yaw 140° = vista 3/4 lateral con ojo visible
- Pitch 2° ≈ frontal
- Resolución 512×192 (ratio 8:3)
- Caché separada (headshotCache, headshotSpriteCache)

**Flujo CaptureHeadshot:**
1. Crea RenderTexture 512×192 (si no existe)
2. Assemble + SetMood (como retrato)
3. Calcula headBounds: franja de TopFraction (35%) centrada a CenterHeight (58%)
4. Posiciona cámara hacia cabeza (Pitch/Yaw/Roll)
5. Ajusta aspect ratio cámara a 512/192
6. Render a headshotRt (shadows off)
7. ReadPixels → Texture2D
8. Restaura camera.targetTexture = rt, ResetAspect
9. Caché headshot y retorna

**Parámetros S58 (ajustables en inspector):**
- headshotWidth/Height → resolución
- headshotPadding → zoom (1.05 = 5% margin)
- headshotTopFraction → altura franja (0.35 = top 35%)
- headshotCenterHeight → centro (0.58 = 58% desde min.y)
- headshotPitch/Yaw/Roll → ángulos cámara

## Consumidores S58

- [[CombatOrderBarUITK]] — headshots en cartas de orden
- [[CombatVisualizerPanelUITK]] — headshots en log "Eventos"
- [[MonchiPortraitUI]] — ApplyHeadshot wrapper

## Vinculado a

- [[Index/10 - Visualization]]
- [[MonchiPortraitUI]] — consumer principal
- [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]] — data visual

## Conexiones

**Entrada:**
- API: GetPortrait, GetHeadshot (por DNA)
- Construcción serializada en GameScene

**Salida:**
- Texture2D/Sprite caché
- UI panels, cartas de combate
