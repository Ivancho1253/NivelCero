# Matriz de correspondencia académica actualizada

**La experiencia 3D es una evolución del concepto original. Su aceptación como entrega principal depende de la aprobación del docente.** AE4_2D_SectorDeArranque ya tiene recorridos aprobados en Editor y Windows. El cumplimiento académico requiere cotejar la AE4 original, que no fue suministrada.

Las filas corresponden al encargo ampliado. XML: Tests/results y copias en Entrega/05_Portafolio/Pruebas. Pruebas automáticas, observación humana y aprobación docente son evidencias diferentes.

| Requisito descrito | Escena / archivo real | Evidencia ejecutada | Pendiente humano/académico |
|---|---|---|---|
| Unity y C# | Ambas; Packages, ProjectSettings, Runtime/Core | Editor 6000.3.28f1, suites y Windows ejecutados | Revisar entrega en equipo de evaluación. |
| Tilesheet y sprites | 2D; tiles.png, GenerateTiles.ps1, Academic2D.Start | Atlas original de ocho celdas importado y renderizado | Cotejar formato solicitado por consigna. |
| Tilemap | Academic2D.BuildTilemap; Grid/Tilemap/TilemapCollider2D | PlayerLandsOnTilemapAndPhysicalFirewallStopsMovement; recorrido | Mostrar estructura del Tilemap al docente. |
| Movimiento y salto 2D | Academic2D.FixedUpdate; Rigidbody2D/CapsuleCollider2D | A/D, salto y dash sobre ambos huecos en AcademicFullRouteTests, Editor/Windows | Comodidad con personas. |
| Parallax | Academic2D, tres capas background/parallax | Ejecutado durante recorrido/grabación 2D | Revisión visual de percepción/velocidades; sin métrica automática perceptual. |
| NPC interactivo | Academic2D; BitFollower/InteractionScanner | E a Bit forma parte de ambos recorridos | Comprensión/calidad del diálogo. |
| NPC autónomo C# | Bit sigue al jugador en ambas escenas | Seguimiento ejecutado durante recorrido | Demostrar seguimiento y ausencia de interferencia. |
| Jefe primer nivel | Academic2D.UpdateBoss; World/Guardian | Ambos recorridos terminan con jefe a 0; 3D hack por fase | Dificultad, justicia y anticipación. |
| Vida/daño | Academic2D.ReceiveDamage; Health | Fallo hack2D causa daño; integración 3D de armas/muerte | Feedback y casos prolongados. |
| Checkpoints/respawn | Academic2D.Respawn; Session/CheckpointBeacon | Checkpoints activados; LethalDamageEntersDefeatAndRespawnUsesSavedCheckpoint en 3D | Muerte/respawn 2D manual; no se atribuye cobertura equivalente a3D. |
| Fragmentos/opcionalidad | Academic2D; CodeShard/ProgressData | Victoria con dos principales en ambas escenas sin exigir el tercero | Interés de rutas y recompensas con personas. |
| Portal/final | Academic2D.CanExit; ProgressData.CanExit/ExitPortal | Editor/Windows; suite conjunta final 2/2 | Demostración humana y valoración del alcance. |
| Hack interactivo | CircuitPuzzle/Session/Academic2D/HUD | Core: 160 tableros; éxito/fallo; soluciones reales en recorridos | Legibilidad/dificultad sin conocer solución. |
| FPS/TPS/cámara | PlayerCamera/PlayerMotor | WASD en ambas, diagonal, pared, V probados | Hombro, sensibilidad, FOV y mareo. |
| Blaster/espada | PlayerCombat/Health | Daño, alcance, obstrucción; blaster/recarga en recorrido | Sensación del combo/bloqueo. |
| IA/navegación | SecurityAgent/Guardian/NavMeshSurface/NavMeshAgent | Enemigos afrontados y NavMesh terrestre construido | Visión, atascos y equilibrio extensivos. |
| Guardado/carga | Session/ProgressData,3D | Checkpoint/fragmentos/puerta y corrupción probados | Sesiones largas;2D no persiste entre ejecuciones. |
| Windows | Entrega/01_Videojuego3D/Windows | Release arrancado con Direct3D / RTX 4060 y 0 excepciones; recorridos aparte en builds de tests | Distribución íntegra y compatibilidad en otro equipo. |
| GDD editable/imprimible | Entrega/03_GDD, DOCX/PDF; Markdown | Archivos exportados de Google Docs presentes | Cotejar contenido y versión de entrega con consigna. |
| Video real | Entrega/04_Gameplay | 3D: 111,502 s; 2D: 30,888 s; FFprobe revisado | Evaluación docente de suficiencia de tomas. |
| Portafolio/bitácora | Entrega/05_Portafolio; Docs/Entrega/Bitacora | Capturas, XML, cambios/fallos/correcciones | Completar únicamente aportes acreditados. |
| Presentación/guion | Entrega/06_Presentacion,HTML/PDF de 14 páginas y guion de 20 min | PDF verificado con Poppler; notas y navegación local | Exposición oral no realizada/evaluada. |
| MDA/Flow/autodeterminación | GDD y guion | Intenciones de diseño documentadas | Sin medición humana de Flow, emoción o satisfacción. |

La suite Windows conjunta tuvo inicialmente 1 aprobado / 1 fallido; tras corregir la interferencia de pausa por pérdida de foco en el fixture, el XML actual registra 2 aprobados / 0 fallidos. La corrección de automatización no altera salud, progreso o dificultad.

No se inventan autorías ni aportes. Sólo Sector de Arranque está implementado; los otros tres sectores son continuidad narrativa. La muestra breve a 1080p dio 59,87 FPS promedio en RTX 4060; ver `Docs/Entrega/Rendimiento.md`. Siguen pendientes profiling CPU/GPU extenso, otras configuraciones, partidas humanas de 10–20 minutos y aprobación académica.
