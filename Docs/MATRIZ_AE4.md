# Matriz de correspondencia académica

**La experiencia 3D es una evolución del concepto original. Su aceptación como entrega principal depende de la aprobación del docente.** La escena 2D es una base complementaria; no se considera acreditada hasta jugarla de principio a fin y cotejar la consigna AE4 original, que no fue suministrada.

| Requisito del adjunto | Escena | Archivo / elemento | Evidencia actual | Verificación faltante |
|---|---|---|---|---|
| Unity y C# | Ambas | Unity/Packages, ProjectSettings, Assets | Fuentes y escenas presentes | Importar y compilar |
| Tilesheet | AE4_2D_SectorDeArranque | Art/Academic/tiles.png, Tools/GenerateTiles.ps1 | PNG original generado 256×32 | Importación Sprite en Editor |
| Tilemap | 2D | Academic2D.BuildTilemap, Tilemap, TilemapCollider2D | Código implementado | Mostrar mapa y colisiones ejecutadas |
| Sprites | 2D | Academic2D.Start/Visual, atlas de 8 sprites | Atlas inspeccionado y división por código | Verificar render y transparencia |
| Movimiento y salto | 2D | Academic2D.FixedUpdate | Rigidbody2D, CapsuleCollider2D y buffer/coyote implementados | Recorrido real |
| Parallax | 2D | background/parallax, tres capas | Código implementado | Capturar desplazamiento real |
| NPC con interacción | Ambas | Academic2D/InteractionScanner | E y diálogos condicionados | Interacción durante gameplay |
| NPC autónomo | Ambas | Academic2D.Update, BitFollower | Seguimiento por C# | Demostrar seguimiento |
| Jefe primer nivel | Ambas | Academic2D.UpdateBoss; Guardian | Vida, fases, ataques y derrota en código | Derrotarlo sin herramientas QA |
| Vida/checkpoints | Ambas | Academic2D, Health/Session/CheckpointBeacon | Implementación | Morir y reaparecer |
| Fragmentos | Ambas | Academic2D.shards; CodeShard/ProgressData | Core pasó pruebas de obligatoriedad/opcionalidad | Recoger en escenas |
| Portal y final | Ambas | CanExit / ProgressData.CanExit / ExitPortal | Core pasó gating de salida | Completar ambas escenas |
| Física 3D, FPS/TPS, combate y hack | 3D | Gameplay, CircuitPuzzle, Session, World | Core de circuitos pasó tests; integración escrita | Compilar y probar integración |
| Build Windows | Ambas | Editor/ProjectSetup.BuildWindows | Procedimiento preparado | Generar y ejecutar EXE |
| Portfolio y evidencia | Docs | AUDITORIA/VALIDACION | Registro real de esta sesión | Capturas y gameplay reales |
| GDD, video, presentación | Entrega | Pendiente | Sin archivos inventados | Crear tras validar producto |

No se atribuyen aportes a integrantes no acreditados. El estado de cada fila refleja el trabajo realizado, no la aprobación académica.
