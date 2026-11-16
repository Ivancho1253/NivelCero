# Nivel Cero Escape del Glitch

Entrega funcional Unity 0.6.0 para Windows, con Sector de Arranque 3D y escena académica 2D. La versión web 0.5 original se conserva intacta.

## Jugar

Abrir `JUGAR_VERSION_UNITY.bat` o `Entrega/01_Videojuego3D/Windows/NivelCero.exe`. No hace falta instalar Unity para jugar. Mantener la carpeta Windows completa, incluyendo `NivelCero_Data` y DLL.

En el menú, `Nueva partida` inicia 3D y `Escena académica 2D` abre la alternativa con Tilemap. `Continuar` restaura progreso 3D. El lanzador original `JUGAR_NIVEL_CERO.bat` sigue abriendo WebGL 0.5.

Controles 3D: WASD, mouse, Espacio salto, Shift dash, Ctrl agacharse, V FPS/TPS, C hombro, 1 blaster, 2 espada, clic izquierdo atacar, clic derecho apuntar/bloquear, R recargar, E Bit/terminal, Esc pausa. En 2D: A/D, Espacio, Shift, mouse, clic izquierdo,1/2,E,Esc.

## Archivos de entrega

- `Entrega/01_Videojuego3D/Windows/`: ejecutable nativo y sus dependencias.
- `Entrega/01_Videojuego3D/Nivel_Cero_Unity.zip`: proyecto editable compartido por ambas escenas.
- `Entrega/02_Escena2D/`: instrucciones de escena académica y referencias a su código/assets.
- `Entrega/03_GDD/`: GDD editable Word y PDF.
- `Entrega/04_Gameplay/`: grabaciones reales 3D/2D del framebuffer Unity y audio del juego. El 3D dura 1 min 51 s a 1080p/30 fps.
- `Entrega/05_Portafolio/`: bitácora, capturas y resultados verificables.
- `Entrega/06_Presentacion/index.html`: presentación offline de 14 diapositivas, navegación y notas. También hay PDF, guion de 20 minutos y preguntas técnicas.

## Abrir y modificar

Agregar la carpeta `Unity` a Unity Hub, con Editor 6000.3.28f1. Assets, Packages y ProjectSettings están versionados. La escena 3D contiene geometría, modelos, iluminación y navegación editables; el constructor sigue disponible en `Nivel Cero > Construir escena 3D editable`. La escena 2D construye su Tilemap y entidades desde `Academic2D` al ejecutarse.

Los modelos originales y 14 clips de Zero están en `Art/Models`; la fuente Blender y el generador están en `Tools/Art`. La fuente Rajdhani conserva OFL. No se compraron assets ni se incorporaron modelos comerciales.

## Pruebas y límites

Se ejecutaron 7619 aserciones Core, 10 pruebas EditMode, 11 de integración PlayMode, render de escena y recorridos completos 3D/2D. Ambos recorridos alcanzaron victoria también en reproductores Windows de pruebas, sin teletransporte, invulnerabilidad o cambios de inventario/vida. Esas pruebas son automatizadas y se distinguen del arranque comprobado del ejecutable de distribución. Consultar `Docs/VALIDACION.md` y XML de entrega.

El video proviene de cuadros internos de Unity y del AudioListener del juego. No registra escritorio ni micrófono. El bot usa las mismas entradas y acciones del juego; su velocidad no mide primeras partidas humanas ni rendimiento.

Se midió la cadencia real del recorrido 3D a 1920×1080 sin grabación: promedio 59,87 FPS, percentil 95 de 16,99 ms, con i5-14600KF y RTX 4060 y límite de 60 FPS. La muestra dura 33,16 segundos y no garantiza mínimos constantes ni otras PC. Ver `Docs/Entrega/Rendimiento.md`.

Faltan playtests humanos de comodidad, ritmo, dificultad, animación y audio, medición de duración 10–20 min y profiling extenso en otras configuraciones. Tampoco se recibieron las consignas AE originales para cotejo. La aceptación del 3D como entrega principal depende del docente; la escena 2D es la base complementaria demostrable.

El proyecto preserva Zero, Bit, GL1TCH, Guardián y los cuatro sectores narrativos. Sólo Sector de Arranque está construido; la salida no carga una campaña de cuatro niveles.
