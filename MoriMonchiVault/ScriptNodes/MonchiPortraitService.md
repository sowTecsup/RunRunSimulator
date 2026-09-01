---
tags: [script, ui, service, singleton, graphics]
---

# MonchiPortraitService.cs

**Ruta:** `UI/MonchiPortraitService.cs`

**Responsabilidad:** Singleton "fotomatón" estudio oculto. Renderiza capturas 2D de MoriMonchis (full-body retrato). Cachea Texture2D/Sprite por UniqueID/ToStringID. Pipeline: Assemble visual, SetMood, pose Idle, encuadre automático por Bounds, RenderTexture + ReadPixels. **S93:** Pipeline de headshot eliminado completamente (`GetHeadshot`, `GetHeadshotSprite`, `CaptureHeadshot`, cachés de headshot y 8 campos serializados de configuración).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetPortrait(CreatureDNA dna)` | `Texture2D` | Retrato full-body (caché) |
| `GetPortraitSprite(CreatureDNA dna)` | `Sprite` | Sprite full-body (caché) |

## Campos Serializados

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

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cache` | `Dictionary<string, Texture2D>` | Caché retrato full-body |
| `spriteCache` | `Dictionary<string, Sprite>` | Caché sprite retrato |
| `rt` | `RenderTexture` | RenderTexture retrato (384×384) |

## Cambios S58-S92

**Métodos legacy (desaparecidos S93):**
- `GetHeadshot()`, `GetHeadshotSprite()` — fueron removidas en S93
- `CaptureHeadshot()` — pipeline de cabeza eliminado
- Cachés: headshotCache, headshotSpriteCache eliminadas
- Campos: headshotWidth, headshotHeight, headshotPadding, headshotTopFraction, headshotCenterHeight, headshotPitch, headshotYaw, headshotRoll eliminados (8 campos totales)

## Vinculado a

- [[Index/10 - Visualization]]
- [[MonchiPortraitUI]] — consumer principal
- [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]] — data visual

## Conexiones

**Entrada:**
- API: GetPortrait (por DNA)
- Construcción serializada en GameScene

**Salida:**
- Texture2D/Sprite caché
- UI panels
