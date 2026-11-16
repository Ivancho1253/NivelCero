# Nivel Cero: Escape del Glitch

Documento de diseño y estado técnico · 16 de noviembre de 2026

## 1. Alcance de este documento

Este GDD describe la implementación Unity y separa el producto comprobado de los objetivos de calidad pendientes. El proyecto conserva una copia de la versión web 0.5.0 y añade un nivel 3D de principio a fin y una escena académica 2D. No se acredita una campaña completa de cuatro sectores.

Las consignas originales AE1, AE2, AE3 y AE4 no fueron suministradas en esta sesión. Se trabajó con el encargo ampliado y con el código recibido. Las relaciones académicas propuestas requieren cotejo contra los documentos originales. La aceptación de la evolución 3D como entrega principal está sujeta a la aprobación del docente; la escena 2D permite demostrar una base ajustada a la consigna 2D descrita en el encargo.

Este documento es Markdown editable. No constituye por sí mismo la entrega Word/PDF, una presentación, un video ni una certificación docente.

## 2. High Concept

Zero despierta dentro de un sistema digital que GL1TCH está corrompiendo. Con la ayuda de Bit, debe recuperar Fragmentos de Código, intervenir circuitos, superar defensas y derrotar al Guardián para abrir una salida.

**Género:** acción, aventura y plataformas de ciencia ficción. **Modo:** un jugador. **Plataforma de destino:** Windows PC, teclado y mouse. **Perspectiva principal:** primera y tercera persona intercambiables. **Experiencia complementaria:** plataformas 2D dentro del mismo proyecto.

La identidad combina instalaciones industriales oscuras, metal, energía cian de orientación, señales rojas de amenaza y fragmentos cálidos que contrastan con el entorno. La intervención del sistema mediante circuitos es el rasgo que vincula combate, exploración e historia.

## 3. Narrativa y universo

| Entidad | Función |
|---|---|
| Zero | Avatar protagonista y agente del jugador dentro del sistema. |
| Bit | Inteligencia artificial acompañante; sigue a Zero, aporta contexto y pistas breves. |
| GL1TCH | Proceso antagonista que corrompe el sistema y se expresa a través de mensajes. |
| Guardián del Sistema | Entidad de seguridad del primer sector; sus defensas deben hackearse para permitir daño. |

Los cuatro sectores originales mantienen su nombre y función narrativa: **Sector de Arranque**, **Memoria Fragmentada**, **Torre Firewall** y **Núcleo Corrupto**. Sólo el Sector de Arranque tiene un recorrido implementado. La salida anuncia Memoria Fragmentada; no carga un segundo nivel terminado.

Los diálogos están definidos en C#. Bit no utiliza un modelo de lenguaje ni una conexión de red. El contenido narrativo se presenta mediante mensajes contextuales, interacción y eventos de progresión.

## 4. Pilares de diseño

1. **Comprender antes de dominar:** enseñar movimiento y salto antes del encuentro de seguridad; explicar circuitos antes del Guardián.
2. **Modificar para avanzar:** el hack desbloquea una barrera real y habilita ventanas de daño del jefe.
3. **Riesgo legible:** anticipación sonora y visual de ataques, obstáculos físicos y retorno a checkpoints.
4. **Exploración con recompensa:** rutas laterales, una pieza opcional y celdas de reparación.
5. **Continuidad de identidad:** conservar personajes, fragmentos, dash, hack y sistema digital corrupto al cambiar de tecnología.

El recorrido automatizado prueba que las reglas permiten llegar a la victoria. No demuestra por sí solo diversión, dificultad equilibrada ni calidad percibida.

## 5. Game loop

**Explorar → leer el entorno → superar un salto o amenaza → recuperar código o activar un checkpoint → resolver un circuito → afrontar una defensa nueva → derrotar al Guardián → alcanzar el portal.**

En una repetición local, el jugador puede corregir su trayectoria, cambiar de arma o perspectiva y usar los checkpoints. La progresión de este slice introduce mecánicas ya disponibles; no contiene un árbol completo de habilidades desbloqueables.

## 6. Controles 3D

| Entrada | Acción |
|---|---|
| W / A / S / D | Desplazarse respecto al plano horizontal de la cámara. |
| Mouse | Orientar la cámara. |
| Espacio | Saltar. |
| Shift | Dash direccional. |
| Ctrl | Agacharse; comprobar espacio antes de levantarse. |
| V | Alternar primera y tercera persona. |
| C | Cambiar hombro en tercera persona. |
| Click izquierdo | Disparar o atacar con espada. |
| Click derecho | Apuntar con blaster o bloquear frontalmente con espada. |
| R | Recargar el blaster. |
| 1 / 2 | Pulse Blaster / Energy Blade. |
| E | Interactuar con Bit o una terminal cercana. |
| Esc | Pausa; durante hack, cancelar la conexión con sus consecuencias. |

Opciones disponibles: sensibilidad, FOV entre 60 y 110, inversión vertical, volumen, ventana/pantalla completa, resoluciones 1280×720 y 1920×1080 y selección entre los niveles de calidad del proyecto. Se conservan mediante `PlayerPrefs`. Su presencia no equivale a una evaluación de accesibilidad completa.

## 7. Movimiento y cámaras

`PlayerMotor` utiliza `CharacterController`, aceleración y frenado, movimiento diagonal normalizado, gravedad, coyote time y jump buffering. El dash tiene cooldown y una carga en el aire que se recupera al tocar el suelo. El daño y el movimiento se bloquean de acuerdo con los estados de partida.

`PlayerCamera` implementa directamente el seguimiento, mirada, cambio de perspectiva, hombro, apuntado y comprobaciones de obstáculos. No depende de Cinemachine. La geometría visual de Zero no obstruye la primera persona; el arma tiene presentación específica para esa vista y representación en mano en tercera persona cuando está disponible el esqueleto.

Hay pruebas automatizadas de WASD en ambas perspectivas, normalización diagonal, colisiones, salto, carga aérea y obstrucción de cámara. Queda pendiente valorar comodidad, sensibilidad inicial, mareo y precisión con jugadores humanos.

## 8. Combate

**Pulse Blaster:** cargador de 18 disparos, recarga de 1,25 segundos, consumo real, recoil y trazas de impacto. Un primer raycast determina el objetivo de la cámara y un segundo comprueba el trayecto desde el arma/avatar. También se comprueba el espacio inmediato delante del cuerpo para impedir disparar desde dentro de una pared. El daño normal por disparo es 20 en la configuración actual.

**Energy Blade:** tres pasos de combo, ventanas de encadenamiento, alcance limitado, consulta de colisiones y comprobación de obstrucciones. Cada objetivo se contabiliza una vez por golpe. El bloqueo reduce daño frontal; no protege ataques desde cualquier dirección.

`Health` centraliza vida, invulnerabilidad, daño, curación y eventos de reacción/muerte. Las pruebas de integración verifican daño de ambas armas, cobertura, recarga implícita del recorrido, muerte y respawn. El recorrido principal automatizado utiliza el blaster; no constituye una prueba humana de satisfacción del combo de espada.

## 9. Hack de circuitos

`CircuitPuzzle` es una clase C# independiente de Unity. Cada módulo guarda puertos norte, este, sur y oeste mediante una máscara de bits. Girar cambia esa máscara. El flujo parte del oeste de la celda inicial y requiere conexiones recíprocas hasta la salida este de la última celda. La validación recorre la conectividad; una celda final energizada sin salida correcta no basta. Las ramas sin conectar están permitidas: es un puzzle de conectividad, no de presión hermética.

La generación determinista por semilla crea una ruta solucionable y la mezcla mediante rotaciones. El estado inicial no se entrega ya resuelto. La interfaz permite girar, reiniciar y validar módulos. El mundo queda pausado y el cursor libre mientras se interviene.

En 3D, una terminal abre el firewall y las terminales de arena exponen el núcleo del Guardián. Cancelar/fallar bloquea temporalmente el terminal. El escudo sólo cae después de una solución validada; no se utiliza una barra de progreso como sustituto del puzzle.

## 10. Enemigos y navegación

`SecurityAgent` distingue drone, unidad terrestre y torreta. Su máquina de estados incluye patrulla, investigación, persecución, anticipación de ataque, ataque y muerte. La detección comprueba distancia y obstáculos. Al perder visión, investiga la última posición y vuelve a patrullar.

La unidad terrestre usa `NavMeshAgent`. La escena editable conserva el NavMesh construido y serializado. `SectorBuilder` mantiene `NavMeshSurface` para reconstruirlo al volver a generar el mundo. Drone y torreta utilizan lógica adecuada a su movilidad; el drone limita su movimiento con consultas físicas y la torreta permanece fija. Los proyectiles comprueban el desplazamiento completo de cada paso para respetar colisiones.

No se acredita IA táctica avanzada, coberturas coordinadas ni navegación aérea volumétrica. Las pruebas no sustituyen una exploración extensa de situaciones de atasco.

## 11. Guardián y arena

El Guardián 3D tiene 360 de integridad y tres fases basadas en vida. Comienza protegido. Un hack válido lo expone durante un máximo de 18 segundos; al cambiar de fase, se protege otra vez. Esto obliga a integrar circuitos y armas en el encuentro, en lugar de resolverlo sólo manteniendo disparo.

Los ataques marcan previamente la posición del jugador. La cantidad/agresividad de los pulsos aumenta con la fase. En fases posteriores se activan láseres de arena con aviso antes del daño. La derrota actualiza el progreso, guarda y permite la salida cuando se cumplen los fragmentos requeridos.

El recorrido 3D comprobado usa cuatro hacks: acceso y una exposición por cada fase del Guardián. El combate no pretende acreditar un jefe con todos los patrones o el pulido audiovisual de una producción comercial.

## 12. Composición del Sector de Arranque 3D

| Tramo | Función y contenido |
|---|---|
| Despertar | Presentación de Zero, Bit y orientación visual. |
| Entrenamiento | Plataformas, huecos, salto y dash. |
| Seguridad | Drone, primeros impactos y checkpoint. |
| Acceso restringido | Terminal que controla un firewall físico. |
| Exploración | Unidad terrestre, rutas laterales y fragmento principal adicional. |
| Núcleo de Seguridad | Checkpoint, torreta, terminales, láseres y Guardián. |
| Escape | Portal y mensaje hacia Memoria Fragmentada. |

Hay tres fragmentos con IDs estables: dos obligatorios y uno opcional. El portal exige los dos principales y la derrota del jefe. Existen dos rutas laterales que se reconectan al recorrido; no se presentan cuatro mapas completos.

La geometría de colisión sigue siendo simple para mantener estabilidad, mientras los modelos y paneles aportan detalle visual. El contenido se ensambla al ejecutar mediante código; no todos sus elementos están convertidos en una escena de autoría manual con prefabs independientes.

## 13. Bit, recompensas y checkpoints

Bit acompaña a Zero sin ocupar el collider del jugador, introduce información y responde a E. Los fragmentos son la recompensa de progreso; el opcional y las celdas de reparación recompensan desviarse del camino. Los checkpoints restauran integridad y fijan el lugar de retorno.

El guardado 3D usa JSON en `Application.persistentDataPath`, valida versión, IDs, checkpoint y tiempo antes de aplicar datos y escribe mediante archivo temporal y reemplazo. Se verificó carga de checkpoint, fragmentos y puerta, así como recuperación ante datos corruptos. No hay guardado en la nube ni transferencia automática desde la versión web.

## 14. Escena académica 2D

`AE4_2D_SectorDeArranque` es independiente y ejecutable dentro del mismo proyecto. `Academic2D` construye un `Grid`, `Tilemap`, tiles del atlas original y `TilemapCollider2D`; Zero usa `Rigidbody2D` y `CapsuleCollider2D`. Incluye cámara de seguimiento, tres capas de parallax, Bit autónomo/interactivo, dos huecos, tres fragmentos, checkpoints, terminal de circuito, firewall, Guardián de tres fases y portal.

Sus controles son A/D, Espacio, Shift, mouse, click izquierdo, 1/2, E y Esc. No se atribuye a esta escena el cambio FPS/TPS ni la recarga 3D. El progreso 2D se conserva durante la partida, no entre ejecuciones.

La terminal requiere un circuito 3×3. Una validación errónea resta tiempo; agotarlo produce daño y cooldown. El jefe tiene daño y descargas reales, pero su defensa no exige repetir hack por fase como la versión 3D. El recorrido normal bajo la plataforma superior permite terminar con dos fragmentos: el tercero es realmente opcional.

Una prueba de recorrido completo ejecutada en Unity terminó con dos fragmentos principales, gate desbloqueado, jefe sin vida y cero muertes. No se presenta este bot como una partida humana.

## 15. MDA, Flow y autodeterminación

| Mecánicas | Dinámicas buscadas | Estéticas buscadas |
|---|---|---|
| Salto, dash y plataformas | Leer distancias, corregir trayectoria y dominar impulsos. | Desafío y sensación. |
| Circuitos con puertos recíprocos | Formular, probar y corregir una conexión. | Descubrimiento y competencia. |
| Escudo del Guardián + hack | Alternar intervención, evasión y ataque oportuno. | Tensión y dominio progresivo. |
| Rutas laterales y fragmento opcional | Decidir entre seguridad, exploración y recompensa. | Autonomía y descubrimiento. |
| Bit y mensajes de GL1TCH | Interpretar el sistema como un conflicto narrativo. | Fantasía y relación. |

El Flow se busca mediante introducción escalonada, objetivos visibles, feedback inmediato y checkpoints que limitan el coste del error. Todavía no existe evidencia de equilibrio desafío/habilidad para distintas personas; requiere observación de partidas, registro de atascos, comprensión del hack y frecuencia de muerte.

La autodeterminación se aborda mediante **competencia** —mejora del dominio de salto/circuito/combate—, **autonomía** —perspectiva, arma, rutas y recompensa opcional— y **relación** —Bit como acompañante narrativo—. Son intenciones fundamentadas de diseño, no resultados psicológicos medidos.

## 16. Dirección artística y audio

Los modelos se generaron de forma original mediante Blender 4.5.10 LTS: Zero, armas, Guardián, Bit y piezas industriales. Zero dispone de rig y clips para movimiento y acciones; se utilizan Animator y eventos de gameplay para su presentación. Los materiales se adaptan a URP/Lit, con metal oscuro, emisivos y luces de orientación. Hay niebla, bloom moderado, tonemapping y agrupación del entorno estático por materiales.

El atlas 2D es original y reproducible mediante `Tools/GenerateTiles.ps1`; su estilo de píxel no pretende ser el mismo acabado semi-realista del 3D. La música y efectos son sintetizados por código: `AdaptiveAudio`, `Tone` y audio de combate/escena 2D. La intensidad musical cambia al activarse el Guardián.

La fuente **Rajdhani**, utilizada por la interfaz 3D, procede de Indian Type Foundry y está acompañada por **SIL Open Font License 1.1** en `Unity/Assets/NivelCero/Resources/Fonts/OFL.txt`. Debe conservarse ese texto al redistribuir los archivos tipográficos. Los recursos originales recibidos se conservan sin inventarles una licencia distinta.

El detalle de generación de modelos y validación de importación está en `Docs/ARTE_ORIGINAL.md`. Una lámina renderizada de Blender sirve para inspección artística; no sustituye una captura de gameplay.

## 17. Arquitectura real

| Responsabilidad | Implementación |
|---|---|
| Núcleo sin motor | `Core/CircuitPuzzle.cs`, `Core/ProgressData.cs`, assembly `NivelCero.Core`. |
| Movimiento y cámara | `Gameplay/PlayerMotor.cs`, `PlayerCamera.cs`. |
| Combate, vida y animación | `PlayerCombat.cs`, `Health.cs`, `AvatarAnimation.cs`, `DashTrail.cs`. |
| Ciclo, estados, hack y guardado | `Session.cs`. |
| Nivel y catálogo de arte | `World/SectorBuilder.cs`, `GameAssets.cs`, herramientas Editor. |
| IA y proyectiles | `World/SecurityAgent.cs`, `Guardian.cs`, `ArenaHazard.cs`. |
| Interacción y progresión | `InteractionScanner.cs`, `Terminal.cs`, `CodeShard.cs`, `CheckpointBeacon.cs`, `ExitPortal.cs`, `RepairCell.cs`, `BitFollower.cs`. |
| Interfaz y sonido | `GameHud.cs`, `AdaptiveAudio.cs`, `World/Tone.cs`. |
| Experiencia académica | `Academic/Academic2D.cs`. |
| Pruebas | NUnit/Unity Test Framework en `Tests/Editor`, `Tests/PlayMode`, más comprobaciones Core sin Unity. |

Unity: **6000.3.28f1**. Paquetes fijados: URP **17.3.0**, Input System **1.17.0**, AI Navigation **2.0.16**, Test Framework **1.4.6**. Las dependencias efectivas se registran en `Unity/Packages`. La implementación de cámaras no usa Cinemachine; los efectos no necesitan un grafo Shader Graph propio.

El entorno utilizado incluye Hub **3.22.6**, Unity CLI **1.0**, licencia Personal activa, Blender **4.5.10 LTS** y FFmpeg **9.0.1**. La instalación posterior fue autorizada; el inventario inicial sin Unity corresponde a una etapa histórica.

## 18. Decisiones respecto de WebGL 0.5.0

Se preservó el original antes de modificar el proyecto. El juego web sin dependencias quedó como referencia y respaldo, no como supuesto ejecutable Unity. La migración sustituye físicas horizontales manuales por colisiones del motor, hack instantáneo por circuitos, daño por dash por armas reales, progreso en memoria por guardado JSON 3D y personajes geométricos por modelos originales. C deja de alternar perspectiva y pasa a cambiar el hombro; V conserva el cambio FPS/TPS.

No se trasladaron los comandos QA de teletransporte o derrota artificial del jefe del navegador. Las pruebas de recorrido operan mediante entradas normales y los mismos comandos de menú/circuito de la interfaz; las pruebas de integración aisladas sí preparan estados para comprobar casos concretos y se identifican por separado.

## 19. Evidencia ejecutada

| Evidencia local | Resultado observado |
|---|---|
| `Tests/results/core-checks.txt` | 7.619 aserciones; 160 circuitos resueltos independientemente. |
| `Tests/results/editmode.xml` | 10 aprobadas, 0 fallidas; incluye tres comprobaciones 2D que entran en Play Mode. |
| `Tests/results/playmode.xml` | 11 aprobadas, 0 fallidas; movimiento, cámara, combate, hack, muerte y guardado. |
| `Tests/results/fullscene.xml` | 1 aprobada; arranque/renderizado de escena y arte originales. |
| `Tests/results/fullroute.xml` | 1 aprobada; recorrido 3D con entradas normales hasta victoria. |
| `Tests/results/windows-fullroute.xml` | 1 aprobada; el recorrido 3D ejecutado en reproductor Windows de pruebas fuera del Editor. |
| `Tests/results/academic-fullroute.xml` | 1 aprobada; recorrido 2D sin respawn, con circuito y combate reales. |
| `Tests/results/windows-academic-fullroute.xml` | 1 aprobada; recorrido 2D en reproductor Windows de pruebas. |
| `Tests/results/windows-final-routes.xml` | 2 aprobadas, 0 fallidas en repetición de ambos recorridos Windows. |

Son ejecuciones de suites, no una suma de pruebas únicas ni partidas humanas. Los bots utilizan paso temporal controlado y resolución de circuitos mediante rotaciones; sus tiempos no validan duración para personas ni FPS de rendimiento. La evidencia Windows de pruebas no debe confundirse con el estado del build final de distribución.

La primera suite Windows conjunta tuvo un recorrido aprobado y uno fallido: el jefe 2D quedó con 27 de integridad. Se revisó la interferencia de foco y se desactivó únicamente la pausa automática por pérdida de foco dentro del fixture de automatización. La repetición actual pasó 2/2; no se alteraron vida, daño, progreso ni dificultad. La bitácora conserva la incidencia y diferencia ambas ejecuciones.

## 20. Límites y cierre pendiente

La duración objetivo de 10–20 minutos no está validada con partidas humanas. Se midió una muestra breve del recorrido 3D a 1920×1080 sin grabación: 1985 intervalos reales durante 33,156 segundos, 59,868 FPS promedio y P95 de 16,993 ms, con i5-14600KF y RTX 4060, Direct3D 12 y límite de 60 FPS. Se usó Stopwatch entre Update, no el paso de simulación. `Rendimiento.md` conserva método y límites. No acredita un mínimo constante de 60 FPS ni otras PC; faltan profiling CPU/GPU y sesiones prolongadas. No se acredita accesibilidad integral, ausencia absoluta de errores ni campaña completa. El acabado requiere revisión humana de animación, agarre de armas, audio, claridad del puzzle y ritmo.

El **build release sin tests** está generado en `Entrega/01_Videojuego3D/Windows/NivelCero.exe`, con sus archivos de soporte. Se comprobó arranque independiente con Direct3D y NVIDIA GeForce RTX 4060 y se observaron cero excepciones no controladas en el registro. Su SHA256 coincide con `Tests/results/release-startup.json`. Los recorridos completos de Windows se ejecutaron en builds de pruebas; no se acredita una partida humana completa del release.

Los **videos reales del framebuffer Unity** están en `Entrega/04_Gameplay/`: el 3D tiene 111,502417 segundos, 1920×1080, H.264 a 30 fps codificados y AAC estéreo a 48 kHz; el 2D tiene 30,887583 segundos, 1078×904 y los mismos codecs/frecuencias. FFprobe confirmó esos valores. No son un informe de rendimiento ni partidas humanas.

Se produjeron **GDD Word/PDF** exportados de Google Docs en `Entrega/03_GDD/` y **presentación offline HTML/PDF de 14 diapositivas** con guion y preguntas en `Entrega/06_Presentacion/`. El PDF de presentación fue verificado con Poppler. Estos archivos no acreditan una exposición ya realizada ni aprobación académica. Las exportaciones Word/PDF se actualizaron al cerrar la entrega y se comprobaron con Poppler y LibreOffice. El cotejo de consignas originales sigue pendiente.
