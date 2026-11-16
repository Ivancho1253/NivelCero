# Atlas académico original

`tiles.png` es un atlas de 256 × 32 píxeles, dividido en ocho celdas de 32 × 32. Se genera mediante `Tools/GenerateTiles.ps1`, usando System.Drawing. No contiene recursos descargados ni licencias de terceros para registrar. Es arte provisional original, no el acabado visual de la experiencia 3D.

Orden de las celdas: suelo metálico, peligro, Zero, Bit, Fragmento de Código, Guardián, portal, servidor de fondo.

Importar como Sprite, textura legible y filtro Point. `Academic2D` recorta ocho sprites desde la textura en ejecución, usa el suelo en un `Tilemap` con `TilemapCollider2D` y conserva los otros sprites como entidades de la escena.

Se ejecutó el generador y se inspeccionó visualmente el PNG. Las ocho celdas contienen píxeles visibles. El audio de esta escena también es original, generado en C# como pulsos sinusoidales; no se incorporaron recursos externos.

`Academic2D` contiene un recorrido con dos saltos sobre huecos, una ruta elevada opcional, dos fragmentos obligatorios, checkpoints, Bit como NPC autónomo, un circuito rotatorio de 3 × 3 que controla un firewall físico, un Guardián con tres fases y portal de salida. El fallo del hack tiene penalización y cooldown; el guardado de esta escena es durante la partida, no persistente entre ejecuciones.

La ruta opcional no debe bloquear el corredor principal. Se retiraron dos escalones que atravesaban todo el camino y se ajustó el impulso del salto a 14 unidades/s para que la plataforma elevada se pueda alcanzar voluntariamente desde un lateral. El paso a nivel del suelo queda abierto y permite completar el nivel sin el tercer fragmento.

`Tests/PlayMode/AcademicFullRouteTests.cs` recorre la escena real con teclado y mouse sintéticos: inicia mediante el mismo comando del menú, salta ambos huecos, recupera los dos fragmentos principales, abre el circuito con E, gira sus módulos, valida la conexión, atraviesa el firewall, activa el checkpoint, dispara contra el Guardián y entra al portal. No escribe posición, salud ni progreso. Su existencia no acredita un recorrido aprobado hasta que Unity lo ejecute y genere resultados.

Hay pruebas de integración en `Tests/Editor/AcademicPlayModeTests.cs`: entran en Play Mode y verifican Tilemap/física, bloqueo del firewall, éxito real del circuito y consecuencias del fallo. La mera presencia de las pruebas no acredita su aprobación. El recorrido manual de principio a fin y la revisión del build deben registrarse después de ejecutarse realmente.
