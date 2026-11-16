# Antes y después (0.6 → 0.7)

`comparacion_antes_despues.png`: a la izquierda la versión 0.6 (cuadros del video de entrega anterior); a la derecha la 0.7.

**Procedencia de las imágenes.** Las capturas "antes" son cuadros extraídos con FFmpeg de `Entrega/04_Gameplay/*.mp4` de la 0.6. Las 3D "después" (`Despues/01…11`) son renders de la cámara del juego desde puntos de vista fijos (`VisualReview3DTests`): Zero se ubica con `Respawn` y no incluyen el HUD IMGUI. Las 2D "después" salen del reproductor Windows de pruebas e incluyen el HUD. No son partidas humanas.

## 3D

| Aspecto | 0.6 | 0.7 |
|---|---|---|
| Piso | Líneas cian emisivas en todos los módulos | Juntas mate; el cian queda para terminales, checkpoints y guías |
| Techo y ritmo | Losa plana gris, pasillo uniforme | Vigas cada 6 m, conductos y umbrales con dintel por zona |
| Zonas | Sin hitos | Umbrales cian (Seguridad, Exploración, Núcleo) y rojo (Arena) |
| Repetición | Mismos bloques en todo el recorrido | Contenedores fuera de la ruta, anillo rojo y pilones en la arena |
| Enemigos | Reutilizaban los modelos de Zero (terrestre) y de Bit (dron) | Siluetas angulosas propias con barra sensora roja |
| Zero | Se perdía contra el fondo | Luz de contorno trasera; cadencia de piernas ligada a la velocidad |
| Arma en 1ª persona | Dependía del dispositivo de entrada | Siempre visible, con retroceso, recarga y balanceo |
| Bit | Esfera que seguía de costado | Detrás del hombro en 3ª persona, adelante y al costado en 1ª; evita paredes, se inclina, mira terminales y reacciona al hablar |
| Guardián | Encendido sin presentación | Luces de arena que suben, título, giro hacia Zero y núcleo rojo, pulsante o ámbar según el estado |
| Interfaz | Texto plano | Paneles, barras segmentadas, retratos, jefe con fases y estado |

## 2D

| Aspecto | 0.6 | 0.7 |
|---|---|---|
| Personajes | Sprites de 32 px escalados de forma desproporcionada | Zero 27 cuadros, brazo apuntado, Bit 18, Guardián 63, sin escalar |
| Escenario | Una sola pieza de suelo y bloques azules | Tileset de 32 piezas, 6 Tilemaps, fondo, decoración y corrupción por zona |
| Parallax | Rectángulos de color | Tres capas pintadas y repetibles |
| Jefe | Imagen que oscila y cambia de color | Presentación, 3 fases, 3 ataques anunciados, ventanas de núcleo, muerte animada |
| Sonido | Pitidos senoidales | 51 archivos originales: música de exploración y combate, ambiente, stingers y efectos |

Para valorar lectura en movimiento, ritmo y comodidad hace falta observación humana; estas imágenes no la reemplazan.
