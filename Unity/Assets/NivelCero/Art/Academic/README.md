# Atlas académico original

`tiles.png` es un atlas de 256 × 32 píxeles, dividido en ocho celdas de 32 × 32. Se genera mediante `Tools/GenerateTiles.ps1`, usando System.Drawing. No contiene recursos descargados ni licencias de terceros para registrar. Es arte provisional original, no el acabado visual de la experiencia 3D.

Orden de las celdas: suelo metálico, peligro, Zero, Bit, Fragmento de Código, Guardián, portal, servidor de fondo.

Importar como Sprite, textura legible y filtro Point. `Academic2D` recorta ocho sprites desde la textura en ejecución, usa el suelo en un `Tilemap` con `TilemapCollider2D` y conserva los otros sprites como entidades de la escena.

Se ejecutó el generador y se inspeccionó visualmente el PNG. La importación en Unity, las colisiones, los saltos, el combate y el recorrido de principio a fin todavía requieren ejecución real en el editor y en un build.
