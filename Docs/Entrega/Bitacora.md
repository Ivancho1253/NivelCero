# Bitácora verificable de desarrollo

Nivel Cero: Escape del Glitch · corte documental: 16 de noviembre de 2026 (incluye la versión 0.7, §8)

Se registran acciones y resultados respaldados por archivos locales. No se asignan horas de trabajo, autores, integrantes, sprints celebrados, revisiones humanas ni responsabilidades no acreditadas. Los documentos anteriores de `Docs/` contienen estados iniciales de la migración; cuando dicen que Unity estaba ausente o que ciertas pruebas estaban pendientes, describen esa etapa y no el resultado posterior registrado aquí.

## 1. Material recibido y conservación

La versión 0.5.0 recibida contiene ocho archivos: `game.js`, `index.html`, `style.css`, `icon.svg`, `menu.png`, `JUGAR_NIVEL_CERO.bat`, `LEEME_PRIMERO.txt` y `CHANGELOG.txt`. No era un proyecto Unity. El encargo ampliado adjunto define objetivos; no contiene las consignas originales AE1/AE2/AE4 para cotejo directo.

Antes de modificar se generó `../NivelCero_3D_v0_5_original_20261116_002458.zip`, 744.077 bytes, SHA256 `5A5F1A7C40162129C00F85637A6397B1AFB556F279EFAF88EAAEF4EE638D3613`. El commit inicial `4e8f046` conserva los archivos originales. Este registro no acredita push a un remoto.

## 2. Registro de decisiones y resultados

| Etapa | Acción real | Evidencia o consecuencia |
|---|---|---|
| Auditoría | Lectura del código, controles, render y progresión WebGL. | `Docs/AUDITORIA.md`; hack instantáneo y combate por dash identificados. |
| Conservación | ZIP y control de versiones antes de reconstruir. | Respaldo y huella anteriores. |
| Arquitectura | Separación de lógica pura Core y sistemas Unity. | `CircuitPuzzle`, `ProgressData`, Gameplay, World, Session y HUD. |
| Núcleo | Circuitos generados, conexiones recíprocas, rotación y datos de progreso validados. | `Tests/results/core-checks.txt`: 7.619 aserciones y 160 tableros resueltos. |
| Herramientas | Tras la autorización posterior, instalación y uso del Editor y Blender. | Proyecto actual fijado a Unity 6000.3.28f1; registro de arte con Blender 4.5.10 LTS. No confundir con la etapa inicial sin herramientas. |
| Gameplay | CharacterController, cámaras FPS/TPS, salto, dash, armas, daño y colisiones. | `Tests/results/playmode.xml`; 11 pruebas de integración aprobadas. |
| IA | Estados, visibilidad, NavMeshAgent terrestre y proyectiles con barrido físico. | Código `World/SecurityAgent.cs` y superficie creada en `SectorBuilder`. |
| Jefe | Escudo real, exposición por hack y tres fases; defensas de arena. | Recorrido 3D con cuatro hacks y derrota del jefe. |
| Arte | Modelos, rig y clips originales generados con Blender; importación y materiales Unity. | `Docs/ARTE_ORIGINAL.md`, manifiestos y prueba de escena completa. |
| Audio/UI | Audio sintetizado adaptativo; fuente Rajdhani con texto OFL incluido. | `AdaptiveAudio.cs`, `GameHud.cs`, `Resources/Fonts/OFL.txt`. |
| Escena académica | Atlas original, Tilemap y física 2D, Bit, fragmentos, circuito, firewall, jefe y portal. | Pruebas 2D de integración y recorrido. |
| Corrección del nivel 2D | Se retiraron dos escalones que hacían obligatoria la ruta opcional; se ajustó el salto para que el acceso superior sea voluntario. | Cambio documentado en el generador del nivel y README del atlas; recorrido aprobado con sólo dos fragmentos. |
| Audio 2D | Se incorporó el AudioListener faltante a su cámara. | Corrección del arranque de la escena antes de la distribución. |
| Recorridos | Automatización mediante entradas normales, sin escribir estado de victoria ni teletransportar al jugador. | XML de recorrido 3D, reproductor Windows y escena 2D. |

La secuencia representa dependencias de desarrollo y resultados disponibles; no pretende ser un registro de horas de personas. No se atribuyen a un docente aprobaciones que no se hayan recibido.

## 3. Resultados de pruebas ejecutadas

| Archivo en `Tests/results/` | Resultado XML | Alcance acreditado |
|---|---|---|
| `editmode.xml` | 10 aprobadas, 0 fallidas | Núcleo, datos y tres pruebas 2D que entran en Play Mode. |
| `playmode.xml` | 11 aprobadas, 0 fallidas | WASD, diagonal, cámara, salto/dash, armas/paredes, hack, escudo, muerte/respawn, guardado/corrupción. |
| `fullscene.xml` | 1 aprobada, 0 fallidas | Inicialización/renderizado de escena 3D y arte original. |
| `fullroute.xml` | 1 aprobada, 0 fallidas | Recorrido completo 3D mediante acciones normales. |
| `windows-fullroute.xml` | 1 aprobada, 0 fallidas | Recorrido completo 3D en reproductor Windows de pruebas fuera del Editor. |
| `academic-fullroute.xml` | 1 aprobada, 0 fallidas | Recorrido completo 2D mediante acciones normales y sin respawn. |
| `windows-academic-fullroute.xml` | 1 aprobada, 0 fallidas | Recorrido completo 2D fuera del Editor. |
| `windows-final-routes.xml` | 2 aprobadas, 0 fallidas en repetición | Ambos recorridos en una suite conjunta Windows; hubo una incidencia inicial documentada abajo. |

Son resultados de distintas suites y ejecuciones; no deben sumarse como número de pruebas únicas ni como personas que jugaron. `core-checks.txt` es un registro separado de comprobaciones C# sin motor.

### Datos finales que constan en las salidas

- **3D, Editor:** posición final `(0.00, 0.08, 134.31)`, integridad 86, dos fragmentos, checkpoint 2, jefe 0, cuatro hacks, 38 disparos registrados y 2.057 frames del bot.
- **3D, reproductor Windows de pruebas:** misma condición final, integridad 86, dos fragmentos, jefe 0, cuatro hacks, 37 disparos registrados y 2.182 frames del bot.
- **2D, Editor:** posición final `(100.23, 1.81)`, integridad 100, dos fragmentos requeridos, firewall abierto, jefe 0, cero muertes y 1.106 frames del bot.

Los contadores describen esas ejecuciones con paso temporal controlado. No equivalen a FPS medidos ni a duración de gameplay humano. La salida Windows repite el mismo mensaje en distintos niveles del XML; no son varias partidas independientes.

### Fechas del registro

Los XML de Editor incluyen campos UTC generados por su runner. `windows-fullroute.xml` contiene campos de fecha `0001-01-01`; esa marca no es una fecha de desarrollo válida y no se utiliza para reconstruir horarios. Se conservan los archivos como evidencia del resultado y no se inventan fechas para corregirlos.

## 4. Práctica iterativa utilizada

La organización observable fue por incrementos: inspeccionar y conservar, implementar una responsabilidad, ejecutar comprobaciones concretas, corregir problemas y ampliar a integración y recorrido completo. El cambio de los escalones 2D es un ejemplo: una ruta que debía ser opcional interfería con el camino principal; se corrigió la geometría y se demostró salida con dos fragmentos.

Este procedimiento es compatible con una forma de trabajo ágil e incremental. No se acredita un Scrum formal, reuniones, burndown, duración de sprints ni reparto de integrantes sin registros que lo respalden. El backlog técnico pendiente se organiza por validación y dependencia del resultado anterior.

## 5. Incidencia de automatización y repetición Windows

La primera ejecución de la suite conjunta Windows registró **1 aprobada y 1 fallida**. El recorrido académico dejó al Guardián con 27 de integridad; no se declaró victoria artificial ni se editó su vida. Se investigó la interferencia de pérdida de foco entre ventanas. El fixture se ajustó para desactivar exclusivamente la pausa por foco durante la automatización; no modificó daño, posición, fragmentos, dificultad ni condición de salida.

La repetición actual de `Tests/results/windows-final-routes.xml` registra **2 aprobadas y 0 fallidas**, con ambos casos de recorrido aprobados. Los logs `Tests/unity-windows-final-routes.log` y `Tests/unity-windows-final-routes-2.log` permiten distinguir la ejecución inicial y la posterior. El resultado final no borra el fallo previo ni demuestra estabilidad absoluta en todos los entornos.

## 6. Evidencia visual y entregables producidos

`Tests/results/sector3d-runtime.png` es una captura producida durante validación de la escena. Las láminas de Blender documentan inspección de modelos y deben identificarse como tales. Ninguna debe rotularse como una sesión humana si procede de automatización o render de inspección.

El **build release sin assemblies de tests** se generó en `Entrega/01_Videojuego3D/Windows/NivelCero.exe`. Su carpeta incluye los datos y bibliotecas de soporte. `release-startup.json` registra un arranque independiente real con Direct3D y NVIDIA GeForce RTX 4060, con cero excepciones no controladas observadas. El SHA256 del EXE de entrega coincide con el registro: `FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`. Se distingue este arranque de los recorridos completos en reproductores de pruebas; no se atribuye una partida humana al release.

Se generaron MP4 reales del framebuffer Unity. FFprobe confirmó el 3D de **111,502417 s**, **1920×1080**, H.264 a **30 fps codificados**, audio AAC estéreo a **48 kHz**, **39.516.980 bytes**. El 2D dura **30,887583 s**, mide **1078×904**, usa los mismos codecs/frecuencias y ocupa **2.252.736 bytes**. Ambos están en `Entrega/04_Gameplay/`; sus ejecuciones de grabación tienen XML aprobados. Las diferencias de resolución son reales y no se ocultan.

Se exportó GDD Word editable y PDF desde Google Docs a `Entrega/03_GDD/`. La presentación local en `Entrega/06_Presentacion/` incluye HTML navegable, notas, guion de 20 minutos y PDF visual de **14 páginas 16:9** verificado/renderizado con Poppler. Se corrigieron texto de plantilla y una imagen genérica de la fuente inicial en línea, sustituyéndolos por capturas reales en las diapositivas locales 1 y 2. La fuente inicial no se presenta como el resultado local corregido.

El entorno instalado y utilizado en esta etapa es Unity 6000.3.28f1, Hub 3.22.6, CLI 1.0, licencia Personal activa, Blender 4.5.10 LTS y FFmpeg 9.0.1. Las instalaciones fueron posteriores y autorizadas; no contradicen el inventario inicial, que era un estado histórico.

Los Markdown son fuentes editables complementarias. Las exportaciones Word/PDF se actualizaron durante el cierre y se comprobaron con Poppler y LibreOffice. Los documentos y la presentación no acreditan exposición oral realizada ni aprobación docente.

## 7. Pendientes explícitos

1. Cotejar AE1, AE2, AE3 y AE4 originales y actualizar la matriz con el texto real de la consigna.
2. Obtener la decisión docente sobre el 3D como experiencia principal y ajustar la entrega según esa decisión.
3. Revisar la distribución íntegra en otro equipo; el build y su arranque ya están registrados, pero no se acredita compatibilidad universal.
4. Revisar con el docente la suficiencia académica de los videos y documentos producidos.
5. Hacer playtests humanos de comprensión del hack, control, combate, dificultad, ritmo y accesibilidad.
6. Ampliar la muestra breve a 1080p ya ejecutada: 59,87 FPS promedio y P95 16,99 ms con i5-14600KF y RTX 4060, sin grabación. El informe `Rendimiento.md` usa intervalos reales de Stopwatch; faltan profiling CPU/GPU y otras configuraciones.
7. Medir duración de primeras partidas; el objetivo 10–20 minutos sigue sin validación humana.
8. Revisar pulido de animación, montaje de armas, materiales y mezcla de audio con observación real.

El progreso técnico demostrado es sustancial: ambas escenas tienen una condición de victoria alcanzada. El estado de calidad final y los entregables académicos se evalúan con estas limitaciones visibles.

## 8. Versión 0.7: dirección artística, personajes, audio e interfaz (16 de noviembre de 2026)

**Entorno.** El Editor de Unity no estaba instalado en esta PC. Con autorización se instalaron Unity Hub 3.22.6 (winget, paquete MSIX) y el Editor 6000.3.28f1. La licencia Personal se activó desde Unity Hub. Como el Hub MSIX guarda la licencia en su carpeta aislada (`%LOCALAPPDATA%\Packages\UnityTechnologies.UnityHub_…\LocalCache\Local\Unity\licenses`), se copió a `%LOCALAPPDATA%\Unity\licenses` para que el Editor en modo batch la encuentre.

| Etapa | Acción real | Evidencia o consecuencia |
|---|---|---|
| Dirección artística | Guía breve de paleta, formas, jerarquía, tipografía, glitch y señales. | `Docs/Arte/DIRECCION_ARTISTICA.md` |
| Arte 2D | Generador original de pixel art: Zero, brazo apuntado, Bit, Guardián (3 variantes), FX, tileset de 32 piezas, parallax, portal, firewall y retratos. Revisión visual de las láminas; se corrigieron apoyo de pies, torso, cable y celda del brazo. | `Tools/Art/generate_2d_art.py`, `Docs/Arte/hoja_*.png` |
| Audio | Generador original de 51 WAV: música de exploración y combate con fundido, ambiente, efectos por función y voces. Se midieron picos y uniones de bucle. | `Tools/Audio/generate_audio.py`, `Docs/Arte/AUDIO.md` |
| Sistemas compartidos | `SoundBank`, `MusicDirector`, `GameSettings` (volumen de música y efectos separados, reducir efectos), `UiTheme`. | Scripts `Audio/` y `UI/` |
| Escena 2D | Reescrita en partes: juego, nivel, jefe e interfaz; `Bit2D` con `Rigidbody2D` cinemático y triggers; animador de sprites; Tilemaps horneados y editables. | GDD §14 |
| Escena 3D | Iluminación por zonas, piso sin neón, techo con vigas, umbrales, contenedores, arena con anillo y pilones, siluetas de enemigos propias, luz de contorno de Zero, checkpoint con estado, presentación y giro del Guardián, arma en 1ª persona corregida, Bit según la cámara, cadencia de piernas según la velocidad. | GDD §16, `Docs/Arte/ANTES_DESPUES.md` |
| Incidencias | El horneado 2D guardó Tilemaps vacíos (Unity descargó los Tile al abrir la escena): se recargan tras abrirla y se valida que el suelo no quede vacío. Se agregó también una regeneración en ejecución. El blaster de 1ª persona quedaba en el origen de la cámara sin dispositivos de entrada: se posiciona antes de leerlos. Un primer reescalado del blaster (×2,2) se descartó tras verlo en render. | Renders `Tests/results/review3d` |
| Grabación 2D | Un primer intento de grabación en reproductor Windows se interrumpió (el reproductor se cerró a los 397 cuadros). No se usó como evidencia. | — |

### Pruebas de la 0.7 (Unity 6000.3.28f1, Editor en modo batch)

| Archivo en `Tests/results/` | Resultado | Alcance |
|---|---|---|
| `editmode-0.7.xml` | 10/10 | Núcleo, datos y tres integraciones 2D (Tilemap/física, circuito y firewall, fallo del hack). |
| `playmode-integration-0.7.xml` | 11/11 | Movimiento, cámara, armas, hack, escudo, muerte/respawn, guardado. |
| `fullroute-0.7.xml` | 1/1 | Recorrido 3D completo con entradas normales y la geometría nueva: vida final 86, 2 fragmentos, 4 hacks, 38 disparos, 2.081 cuadros. |
| `fullscene-0.7.xml` | 1/1 | Inicialización y render de la escena 3D. |
| `academic-fullroute-0.7.xml` | 1/1 | Recorrido 2D sin respawn con el jefe nuevo: el bot sólo esquiva por marcas y onda visibles; vida final 100, 1.774 cuadros. |
| `academic-showcase-editor.xml` | 1/1 | Recorrido 2D largo: Bit, pasarela opcional, caída y respawn, validación prematura, pausa, jefe y victoria (3 fragmentos, 1 caída). |
| `review2d.xml`, `review3d.xml` | 1/1 cada una | Renders de revisión. La 2D comprueba que la cápsula y el sprite de Zero apoyan en el piso. |

Una ejecución de todas las suites PlayMode juntas se canceló: la escena 3D de la prueba de humo quedaba cargada y se mezclaba con los fixtures de integración (dos AudioListener). Por eso las suites se ejecutan por separado, igual que en la 0.6. Estas pruebas no sustituyen partidas humanas.

### Videos, build y entrega 0.7

- **Grabaciones.** Primer intento 2D: el reproductor de pruebas procesaba teclado y mouse reales sin foco, y lo tipeado en la PC llegó como saltos. Las pruebas ahora desactivan los dispositivos físicos mientras corren. Primer intento 3D: falló porque la sesión 3D se pausó al perder el foco. Se agregó `Session.PauseWhenUnfocused`, que sólo la automatización desactiva. Además, la codificación PNG pasó a hilos aparte: las grabaciones finales corren en tiempo real (2D: 2.185 cuadros, 74,65 s de reloj y 74,67 s de juego; 3D: 1.851 cuadros, 63,64 s y 63,81 s).
- **Videos.** `Nivel_Cero_Gameplay_2D.mp4`: 74,7 s. `Nivel_Cero_Gameplay_3D.mp4`: 63,672 s. Ambos a 1920×1080, 30 fps, AAC 48 kHz (FFprobe). Pico de audio del 2D: −1,6 dBFS, sin saturación.
- **Build.** `ProjectSetup.BuildWindows` (0.7.0): Result Success. El release arrancó en una prueba de 15 s con Direct3D 12 y RTX 4060, sin excepciones en el registro. El SHA256 de `NivelCero.exe` coincide con el de la 0.6 porque es el lanzador genérico de Unity; el contenido nuevo está en `NivelCero_Data`.
- **Entrega.** `Package-Delivery.ps1`: verificación aprobada (233 GUID únicos, 2 escenas, WebGL original intacto), ZIP fuente de 522 entradas y `SHA256SUMS.txt` regenerado. Se ajustaron dos scripts de herramientas: `Package-Delivery.ps1` sólo detecta el Editor real (no el `unity.exe` del Hub) y `Check-Project.ps1` tiene un respaldo si `rg` no está instalado.

### Pendientes de la 0.7

1. Partidas humanas: comodidad, lectura en movimiento, dificultad del Guardián 2D y 3D, ritmo, mezcla de audio y opción "Reducir efectos".
2. Regenerar `Entrega/03_GDD/*.docx/pdf` y la presentación desde el GDD Markdown actualizado; esas exportaciones siguen siendo de la 0.6.
3. El Guardián 3D conserva su modelo y su mecánica (escudo por hack); se mejoró su presentación, sus estados visuales y su sonido, pero no tiene animaciones esqueletales nuevas.
4. Los enemigos 3D son ensamblajes de primitivas con diseño propio, no modelos de Blender.
5. Partida completa del release fuera de las pruebas (sólo se acreditó el arranque).
