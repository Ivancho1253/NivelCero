# Registro de validación actualizado

Corte: 16 de noviembre de 2026. Este documento sustituye el estado inicial de pruebas pendientes. Distingue integración, recorridos automáticos, arranque de distribución y revisión humana. Ningún bot se presenta como partida de una persona.

## Entorno utilizado

Unity Editor **6000.3.28f1**, Hub **3.22.6**, CLI **1.0** y licencia Personal activa. Blender **4.5.10 LTS** para modelos originales y FFmpeg **9.0.1** para video. URP 17.3.0, Input System 1.17.0, AI Navigation 2.0.16 y Test Framework 1.4.6 están fijados en el proyecto. La ausencia de herramientas registrada al principio corresponde a una etapa anterior; las instalaciones posteriores fueron autorizadas.

## Pruebas ejecutadas

| Archivo en Tests/results/ | Resultado | Alcance |
|---|---|---|
| core-checks.txt | 7.619 aserciones; 160 circuitos resueltos | Core sin motor: reciprocidad, salida, rotación, generación y progreso. |
| editmode.xml | 10 aprobadas, 0 fallidas | Siete Core y tres pruebas 2D que entran en Play Mode. |
| playmode.xml | 11 aprobadas, 0 fallidas | Movimiento/cámara, armas/paredes, salto/dash, hack/escudo, muerte/respawn y guardado. |
| fullscene.xml | 1 aprobada, 0 fallidas | Arranque/renderizado de escena 3D y arte original. |
| fullroute.xml | 1 aprobada, 0 fallidas | Recorrido 3D completo en Editor mediante acciones normales. |
| windows-fullroute.xml | 1 aprobada, 0 fallidas | Recorrido 3D en reproductor Windows de pruebas. |
| academic-fullroute.xml | 1 aprobada, 0 fallidas | Recorrido 2D Editor: dos fragmentos requeridos, hack/jefe y cero muertes. |
| windows-academic-fullroute.xml | 1 aprobada, 0 fallidas | Recorrido 2D en reproductor Windows de pruebas. |
| windows-final-routes.xml | 2 aprobadas, 0 fallidas en repetición | Ambos recorridos en suite Windows conjunta; hubo un fallo inicial descrito abajo. |
| gameplay-recording.xml | 1 aprobada, 0 fallidas | Ejecución 3D grabada desde el framebuffer Unity. |
| gameplay-recording-academic.xml | 1 aprobada, 0 fallidas | Ejecución 2D grabada desde el framebuffer Unity. |

Son ejecuciones de suites con cobertura superpuesta, no una suma de pruebas únicas. Los recorridos no escriben posición, salud, fragmentos, jefe ni victoria: usan teclado/mouse sintéticos, inicio normal y rotaciones/validación reales del circuito. Las pruebas aisladas sí preparan estados específicos.

### Incidencia de la suite Windows conjunta

La primera ejecución conjunta registró **1 aprobada y 1 fallida**: el recorrido académico dejó al jefe con 27 de integridad. Se investigó interferencia de foco entre ventanas. El fixture se ajustó para desactivar únicamente la pausa automática por pérdida de foco durante automatización, sin cambiar salud, daño, recorrido ni dificultad. El XML actual confirma **2 aprobadas y 0 fallidas** en la repetición. El fallo inicial no se convierte retroactivamente en aprobado. Logs: Tests/unity-windows-final-routes.log y Tests/unity-windows-final-routes-2.log.

## Cobertura y revisión pendiente

| Área | Evidencia automática ya ejecutada | Revisión pendiente |
|---|---|---|
| WASD/diagonal | WasdMovesInTheCameraPlaneInBothPerspectives; DiagonalSpeedIsNormalizedAndWallsBlockMovement | Comodidad y accesibilidad con personas. |
| Cámara | CameraAvoidsWallAndPerspectiveSwitchDoesNotMovePlayer; V utilizado en recorrido | Encuadre, hombro, sensibilidad, FOV y mareo. |
| Movimiento | JumpLeavesGroundAndAirDashRequiresLandingToRecharge; saltos/dash de ambos recorridos | Precisión percibida y revisión manual de crouch/techos. |
| Combate | GunConsumesAmmoDamagesEnemyAndCannotShootThroughCover; BladeHasDamageRangeAndObstructionChecks; recarga/blaster en recorrido | Sensación del combo, bloqueo y mezcla sonora. |
| Salud | LethalDamageEntersDefeatAndRespawnUsesSavedCheckpoint; daño real en recorridos | Muerte/respawn 2D manual y sesiones largas. |
| Hack | HackRequiresValidCircuitAndFailureLocksTerminal; éxito/fallo 2D y soluciones de recorrido | Comprensión sin ayuda, legibilidad y dificultad humana. |
| Guardián | BossShieldOnlyDropsAfterAValidatedCircuit; tres fases y victoria en recorridos | Justicia de patrones y equilibrio. |
| IA | Drone, terrestre y torreta afrontados en 3D; NavMesh terrestre activo | Ampliar escenarios de visión, persecución y recuperación de atascos. |
| Progreso | Guardado/carga/corrupción, dos fragmentos y checkpoints probados | Sesiones prolongadas y migraciones futuras. |
| 2D | Tilemap/física probados; huecos, Bit E, circuito, jefe y salida en Editor/Windows | Evaluación docente y percepción de parallax/ritmo/arte. |

## Build final de distribución

Existe el build sin assemblies de tests en **Entrega/01_Videojuego3D/Windows/NivelCero.exe**, acompañado por NivelCero_Data, UnityPlayer.dll y archivos necesarios. No separar el EXE de su carpeta.

Tests/results/release-startup.json registra arranque independiente real, Direct3D con **NVIDIA GeForce RTX 4060** y **0 excepciones no controladas** observadas. SHA256 del EXE: **FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B**, coincidente con la copia de distribución. Esto acredita arranque y log de ese build; los recorridos completos fuera del Editor se ejecutaron por separado en reproductores de pruebas. No se atribuye una partida humana al release.

## Videos y documentos producidos

Verificación real mediante FFprobe:

| Archivo en Entrega/04_Gameplay/ | Video | Audio | Duración y tamaño |
|---|---|---|---|
| Nivel_Cero_Gameplay_3D.mp4 (0.7) | H.264, 1920×1080, 30 fps | AAC, 48 kHz, estéreo | 63,672 s; 33.149.135 bytes; tiempo real. |
| Nivel_Cero_Gameplay_2D.mp4 (0.7) | H.264, 1920×1080, 30 fps | AAC, 48 kHz, estéreo | 74,7 s; 11.354.494 bytes; tiempo real; pico de audio −1,6 dBFS. |

Son capturas de gameplay automatizado real desde el framebuffer Unity, no cinemáticas que simulen un recorrido. Los 30 fps describen el archivo codificado; no son profiling del juego.

Entrega/03_GDD contiene GDD Word editable y PDF exportados de Google Docs. Entrega/06_Presentacion contiene presentación HTML local y PDF de 14 páginas 16:9, guion y preguntas. Las diapositivas locales 1 y 2 sustituyen contenido de plantilla/imagen genérica por capturas reales. El PDF es visual; el HTML y las fuentes son editables según su formato. Ningún archivo acredita una exposición oral realizada.

## Límites que permanecen

- No hubo partidas humanas ni validación de 10–20 minutos de primera partida.
- Hay una muestra breve a 1080p con i5-14600KF y RTX 4060: 59,87 FPS promedio, P95 16,99 ms en 1985 intervalos reales, sin grabación. `Docs/Entrega/Rendimiento.md` y el JSON explican el método. Quedan pendientes profiling CPU/GPU, sesiones largas y otras configuraciones; no se acredita un mínimo constante de 60 FPS.
- Los contadores del bot, el paso temporal controlado y los tiempos del runner no miden FPS reales ni satisfacción.
- No se acredita ausencia absoluta de errores, accesibilidad integral ni campaña de cuatro niveles.
- AE1/AE2/AE3/AE4 originales no se suministraron: cotejo y aceptación de la evolución 3D siguen sujetos al docente.

Estructura, Core, integración, render, recorridos y arranque responden preguntas diferentes. Su evidencia no se convierte en aprobación académica o evaluación humana.
