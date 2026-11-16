# Auditoría inicial y actualización de cierre · 16 de noviembre de 2026

**Lectura histórica:** las secciones iniciales registran lo encontrado antes de instalar Unity y reconstruir el juego. El apartado final actualiza los resultados posteriores. Las frases sobre herramientas ausentes, pruebas pendientes o arte faltante pertenecen a aquella primera inspección. Para evidencia vigente, consultar `VALIDACION.md`.

## Material recibido

Ocho archivos originales: `game.js`, `index.html`, `style.css`, `icon.svg`, `menu.png`, lanzador BAT, instrucciones y changelog. No hay Unity Assets, escenas, modelos, clips, proyecto C#, paquetes npm ni documentación académica AE1/AE2/AE4 original. El adjunto `pasted-text.txt` contiene el encargo ampliado, no esos documentos originales. Las referencias académicas de esta migración se basan en él y deben cotejarse con la consigna real.

`game.js` es una IIFE WebGL2 sin bibliotecas externas: shaders, primitivas, físicas de suelo, audio sintetizado, UI DOM y simulación comparten un único archivo. Usa Pointer Lock y conserva un hook QA con teletransporte/derrota del jefe; ese hook no se trasladó al juego Unity.

| Sistema original | Evidencia del código | Decisión |
|---|---|---|
| Movimiento/cámaras | `directionFromKeys`, `applyMouse`, `render`, V/C alternan perspectiva | Reescribir con CharacterController y cámara Unity; C pasa a hombro |
| Colisiones | `terrainAt` considera superficies horizontales; no resuelve paredes | Sustituir por física de Unity |
| Dash | Cooldown temporal permite nuevos impulsos en aire | Añadir carga aérea hasta aterrizar |
| Hack | `tryHack` cambia estados al pulsar E | Sustituir por circuito con conexiones recíprocas y validación |
| Combate | Dash mata drones y daña al jefe; no existen armas | Salud común, blaster, espada, recarga y bloqueo |
| Enemigos | Trayectoria sinusoidal y proyectiles | Estados y visibilidad; navegación avanzada pendiente |
| Guardián | Tres puntos, ALFA+BETA, daño por contacto dash | Daño por armas durante vulnerabilidad habilitada por hack |
| Fragmentos | Los tres son obligatorios | Dos IDs principales obligatorios; tercero opcional |
| Checkpoints | Datos en memoria y respawn | Persistencia JSON con validación de esquema |
| Arte | Cubos, esferas, toroides y octaedros dibujados a mano | Sólo referencia; nuevos modelos siguen provisionales |
| Entrega académica | WebGL3D; el propio LEEME indica que no cumple Unity2D | Escena 2D separada dentro del proyecto Unity |

No se volvió a ejecutar ni certificar gráficamente la versión web. Sus afirmaciones históricas de testing no se cuentan como pruebas nuevas.

## Conservación

Respaldo previo a modificaciones: `../NivelCero_3D_v0_5_original_20261116_002458.zip` (744077 bytes).

SHA256: `5A5F1A7C40162129C00F85637A6397B1AFB556F279EFAF88EAAEF4EE638D3613`.

Git inicializado localmente. Commit `4e8f046` conserva los ocho archivos originales. No se configuró remoto ni se hizo push.

## Entorno y elección técnica de la primera etapa

No se encontró Unity Editor, Unity Hub o Blender en PATH, ubicaciones habituales o registro de aplicaciones. Hay runtime .NET y compilador de Windows Framework, pero no SDK .NET. El usuario indicó preparar el proyecto **sin instalar**. No se descargaron herramientas ni recursos comerciales.

Se fijó Unity 6000.3.3f1, una versión estable publicada de 6.3 LTS. Se verificó la existencia de la versión y la compatibilidad de la familia URP en fuentes oficiales; no se verificó Package Manager local porque falta el Editor. Las consultas HTTP a tarballs URP devolvieron 404: URP está ligado al Editor, por lo que eso no prueba que falte el paquete. No se inventó un packages-lock.json.

Fuentes: [Editor 6000.3.3f1](https://unity.com/releases/editor/whats-new/6000.3.3f1), [soporte Unity 6](https://unity.com/releases/unity-6/support), [changelog oficial URP](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/CHANGELOG.md), [Input System 1.17](https://docs.unity3d.com/ja/6000.0/Manual/com.unity.inputsystem.html). Input System 1.17.0 y Test Framework 1.4.6 se encontraron en el registro oficial de paquetes.

## Arquitectura implementada

- `Core`: CircuitPuzzle y ProgressData, sin referencia a Unity; asmdef independiente, compilable con Add-Type.
- `Gameplay`: PlayerMotor, PlayerCamera, PlayerCombat y Health.
- `World`: SectorBuilder, SecurityAgent, HostilePulse, Guardian e interacciones/coleccionables/checkpoints/Bit.
- `Session`: ciclo de partida, guardado, hack y coordinación de sistemas.
- `GameHud`: menú, opciones, HUD y circuito; IMGUI provisional.
- `Academic2D`: experiencia independiente con Tilemap y física 2D; aún necesita separar sus subsistemas a medida que se valide.
- `Editor/ProjectSetup`: configuración URP/Input, importación y build de Windows.
- Escenas serializadas con raíces y referencias estables. El contenido de nivel se construye al ejecutar; falta convertir la geometría validada en prefabs editables y composición final.

Se evitó incorporar Cinemachine/NavMesh sin poder resolver paquetes ni validar su configuración. Las cámaras son C# directo. Los próximos pasos deben verificar primero la importación y los controles, luego mejorar la navegación y los assets. No confundir esta primera reconstrucción con las fases completas de arte/pulido/entrega.

## Arte y licencias

No se incorporaron assets externos. El atlas 2D proviene de `Tools/GenerateTiles.ps1`; su procedencia se registra junto al PNG. La geometría 3D y el audio son creados en código. El material previo se preserva como recibido, sin atribuirle una licencia nueva. Faltan Zero, armas y Guardián modelados/riggeados, animaciones, materiales, música y efectos de calidad final.

## Continuidad académica

Se mantienen Zero, Bit, GL1TCH y el Guardián; Sector de Arranque, Memoria Fragmentada, Torre Firewall y Núcleo Corrupto conservan su identidad narrativa. No se implementan cuatro niveles. Salto/dash enseñan competencia; rutas laterales aportan autonomía y riesgo/recompensa; Bit sostiene relación y contexto. Mecánicas → dinámicas → estéticas: circuitos/combate/exploración buscan decisiones, dominio, desafío y descubrimiento. Su equilibrio y Flow deben verificarse con partidas reales.

## Actualización posterior: implementación y evidencia de cierre

Tras la autorización posterior se instalaron y utilizaron Unity **6000.3.28f1**, Hub **3.22.6**, CLI **1.0**, licencia Personal activa, Blender **4.5.10 LTS** y FFmpeg **9.0.1**. El proyecto resolvió paquetes y se ejecutó en el motor. La versión actual reemplaza la fijación inicial 6000.3.3f1; el inventario de ausencia se conserva sólo como historial.

Se añadieron modelos/rig/clips originales Blender, materiales URP, música sintetizada adaptativa, tipografía Rajdhani con OFL incluida, navegación terrestre AI Navigation/NavMesh y defensas de arena. Las cámaras siguen siendo C# propio. El nivel se ensambla por código; no se presenta como campaña de cuatro niveles ni como composición completamente convertida a autoría manual por prefabs.

Resultados: Core 7.619 aserciones/160 circuitos; EditMode 10/10; integración 11/11; escena 3D 1/1; recorrido 3D Editor 1/1 y Windows 1/1; recorrido 2D Editor 1/1 y Windows 1/1. La repetición de una suite Windows conjunta terminó 2/2. La primera ejecución había registrado un fallo 2D de finalización del jefe; se corrigió una interferencia de pausa por foco en el fixture sin editar vida, daño ni progreso. Logs y XML están identificados en `VALIDACION.md`.

El release sin tests existe en `Entrega/01_Videojuego3D/Windows/NivelCero.exe`. Arranque independiente comprobado con Direct3D / RTX 4060 y 0 excepciones no controladas observadas. No equivale a un playtest humano completo; los recorridos fuera del Editor corresponden a reproductores de pruebas con entradas normales.

Se generaron videos reales del framebuffer Unity: 3D 1920×1080, H.264 a 30 fps codificados, AAC 48 kHz, 111,502417 s; 2D 1078×904, mismos codecs/frecuencias, 30,887583 s. Hay GDD Word/PDF y presentación offline HTML/PDF de 14 diapositivas. No se atribuyen exposición oral realizada ni aprobación docente a esos archivos.

Se añadió una muestra breve a 1080p sin grabación: 59,87 FPS promedio y P95 de 16,99 ms en i5-14600KF con RTX 4060. El informe `Docs/Entrega/Rendimiento.md` distingue cadencia real de tiempo simulado. Siguen pendientes controles/ritmo/combate con personas, profiling CPU/GPU extenso, otras configuraciones, duración humana de 10–20 minutos y cotejo de AE1/AE2/AE3/AE4 originales. La aceptación del 3D como entrega principal continúa sujeta al docente.
