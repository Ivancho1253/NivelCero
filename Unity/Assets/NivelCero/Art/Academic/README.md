# Arte 2D de la escena académica (0.7)

## Origen y licencia

Todo el pixel art es original y se genera por código con `Tools/Art/generate_2d_art.py` (Python + Pillow, semillas fijas). No contiene recursos descargados ni licencias de terceros. Sigue la guía `Docs/Arte/DIRECCION_ARTISTICA.md`.

Las hojas están en `Assets/NivelCero/Resources/Academic2D/` y se importan automáticamente con filtro Point, sin mipmaps ni compresión y sin reescalado a potencia de dos (`Editor/AssetImportRules.cs`). Escala: **32 píxeles = 1 unidad = 1 tile**.

| Hoja | Celda | Contenido |
|---|---|---|
| `tileset.png` | 32×32 | 32 tiles: suelo con bordes izquierdo/derecho y variantes, relleno, bordes de foso, pasarela (izq./centro/der.), muro y remate, purga de foso (2 cuadros), paneles de fondo, viga, pilar, caños, cables, consola, rack, señal de ruta, señal de peligro, corrupción (2), suelo y relleno de arena. |
| `zero.png` | 48×56 | Zero: reposo 4, carrera 8, salto 2, caída 2, aterrizaje 1, dash 2, daño 2, muerte 6. Pies en la fila 51 para coincidir con la cápsula. |
| `zero_arm.png` | 48×48 | Brazo y arma apuntados en 9 ángulos (−90° a 90°), Pulse Blaster y Energy Blade. Se espeja, no se rota. |
| `zero_shoulders.txt` | — | Hombro por cuadro, para fijar el brazo al cuerpo en cada pose. |
| `bit.png` | 24×24 | Bit: reposo 4, parpadeo 2, hablar 4, alerta 2, preocupado 2, mirar 4. |
| `guardian.png` | 128×128 | Guardián en 3 variantes de blindaje (una por fase): reposo 4, carga 3, disparo 2, núcleo expuesto 2, brazos arriba 2, golpe 2, destello 1, glitch 2; muerte 8; latente 1. |
| `fx.png` | 48×48 | Descarga hostil, pulso de Zero, impactos cian/blanco/rojo, onda de choque, bloque corrupto, retícula, columna marcada, corte de hoja, polvo, fragmento (8), checkpoint (4), terminal (4), tecla E, triángulo de peligro. |
| `portal.png` | 64×128 | Portal cerrado y abierto (4). |
| `barrier.png` | 32×32 | Láser del firewall (2), placa de la compuerta, emisor. |
| `parallax_far/mid/near.png` | 640×480 | Torres de servidores, torres de refrigeración y pasarela, conductos cercanos (repetibles en horizontal). |
| `portraits.png` | 48×48 | Retratos de diálogo: Bit (4 expresiones), GL1TCH, Guardián. Se usan también en el HUD 3D. |

`tiles.png` (atlas 256×32 de la versión 0.6) se conserva como referencia histórica; ya no se dibuja.

## Tilemap editable

`Nivel Cero > Construir escena 2D editable` crea `Art/Academic/Tiles/*.asset` (un Tile por pieza; suelo, pasarela y muros con colisión Grid) y hornea seis Tilemaps en la escena: suelo con `TilemapCollider2D`, pasarela opcional de un sentido (`PlatformEffector2D`), purga de fosos, pared de fondo, decoración y corrupción. Con la escena horneada, `Academic2D` usa esos Tilemaps; si se vacían, vuelve a generarlos desde `Academic2DLevel` al ejecutar.

## Regenerar

```
python -I Tools/Art/generate_2d_art.py
```

Luego, en Unity, reimportar y volver a hornear la escena 2D. Los tamaños de celda y el orden de filas están replicados en `Scripts/Academic/Academic2DArt.cs`: cambiar ambos a la vez.

`Docs/Arte/hoja_sprites_2d.png` y `Docs/Arte/hoja_guardian_2d.png` son láminas de revisión ampliadas.
