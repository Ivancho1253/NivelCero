# Preguntas técnicas y respuestas preparadas

Este material describe la implementación observada al 16 de noviembre de 2026. Antes de la exposición, revisar las rutas y el estado del build final. No atribuye trabajo a integrantes cuya participación no esté acreditada.

## ¿Por qué migrar desde JavaScript/WebGL?

La versión 0.5 tenía un render propio, física simplificada y sistemas concentrados en un único archivo. Unity permite utilizar colisiones, animación, navegación, herramientas de pruebas y compilación de Windows. La migración conserva historia y mecánicas centrales; no es una conversión automática de JavaScript a C#.

## ¿Se conservó el original?

Sí. Se creó un ZIP antes de modificar y se registró su SHA256. Los ocho archivos de la versión web también se conservaron en Git. La ruta y huella están documentadas en la auditoría y la bitácora.

## ¿Qué está implementado de los cuatro sectores?

Sector de Arranque tiene el nivel 3D y una interpretación académica 2D. Memoria Fragmentada, Torre Firewall y Núcleo Corrupto conservan su identidad narrativa, pero no son tres niveles adicionales terminados.

## ¿Por qué CharacterController en 3D y Rigidbody2D en la escena académica?

CharacterController facilita movimiento directo del avatar 3D y resolución de colisiones sin depender de impulsos de un cuerpo rígido. La gravedad y el dash se calculan en PlayerMotor. La escena 2D usa la simulación de Rigidbody2D y sus colliders, además del Tilemap solicitado. No se presenta CharacterController como Rigidbody.

## ¿Cómo se evita el WASD invertido y una diagonal más rápida?

Se proyecta el frente de la cámara sobre el suelo, se calcula su derecha y se combinan los ejes de entrada. El vector se limita a magnitud uno. Las mismas direcciones se usan en FPS y TPS. Existen pruebas para ambas perspectivas y normalización diagonal.

## ¿Cómo cambia la cámara sin mover al personaje?

PlayerCamera modifica la vista, hombro y representación visual; PlayerMotor conserva el cuerpo físico. La cámara comprueba obstáculos. Una prueba verifica que el cambio de perspectiva no modifica la posición del jugador y que una pared limita la cámara.

## ¿Qué son coyote time y jump buffering?

Coyote time conserva brevemente la posibilidad de salto después de abandonar un borde. Jump buffering conserva una pulsación reciente para aplicarla al aterrizar. Buscan reducir errores percibidos sin eliminar la necesidad de precisión. No se ha medido su aceptación con personas.

## ¿Se puede hacer dash infinito en el aire?

No. La disponibilidad combina cooldown y carga aérea; después de consumir la carga en el aire hay que tocar suelo para recuperarla. Una prueba comprueba esa regla.

## ¿Cómo se evita disparar a través de paredes con cámara de hombro?

El rayo desde la cámara establece el destino. Luego se verifica la salida inmediata del cuerpo y el trayecto desde el muzzle al destino. Una pared cercana puede bloquear el disparo aunque la cámara vea al enemigo. Las pruebas incluyen cobertura real.

## ¿La espada hace daño o sólo se anima?

PlayerCombat consulta colliders dentro del alcance, verifica obstrucciones y aplica daño por Health. Un conjunto evita contabilizar al mismo objetivo más de una vez en un golpe. Los combos y cooldowns controlan el ritmo. La animación comunica la acción y no sustituye la detección.

## ¿Bloquear vuelve invulnerable al jugador?

No. El bloqueo de espada reduce el daño que llega desde una dirección frontal. El cálculo usa el origen del golpe y el frente de apuntado. No anula cualquier ataque por detrás ni es una invulnerabilidad general.

## ¿Cómo se sabe que el hack es un puzzle real?

Cada módulo tiene puertos en una máscara de bits y puede rotarse. La energía sólo cruza a un vecino si ambos puertos coinciden. La validación busca una ruta entre entrada y salida. Una conexión incorrecta no abre el sistema. Las pruebas cubren reciprocidad, salida final, generación, rotación, fallo y el efecto físico sobre el firewall.

## ¿Los circuitos pueden resultar imposibles?

El generador crea primero una ruta y después rota las piezas. Eso garantiza al menos una solución. Las pruebas sin Unity resolvieron independientemente 160 circuitos de distintos tamaños y semillas. No se afirma que cada variante tenga la misma dificultad para personas.

## ¿Por qué se permiten ramas sin conectar?

La regla elegida es conectividad entre origen y destino. No simula un circuito sellado donde cualquier fuga invalida la presión. Esa decisión se comunica en el diseño; sería posible agregar otra modalidad, pero no se acredita aquí.

## ¿Qué obliga a usar hack contra el jefe?

El Guardián 3D comienza invulnerable. Una terminal con solución validada expone su núcleo durante un máximo de 18 segundos; al cambiar de fase se protege nuevamente. El recorrido comprobado hizo un hack de acceso y tres de escudo. No se derrota sólo esperando un temporizador.

## ¿Los enemigos utilizan NavMesh?

La unidad terrestre utiliza NavMeshAgent sobre una superficie creada con NavMeshSurface a partir de colliders. Drone y torreta tienen lógicas específicas: vuelo con comprobaciones físicas y ataque desde posición fija, respectivamente. No se atribuye navegación aérea 3D a NavMesh.

## ¿Cómo detectan y pierden al jugador?

SecurityAgent comprueba alcance y obstrucción visual. Si ve al jugador, actualiza su última posición; si deja de verlo, pasa por investigación antes de regresar a patrulla. Los ataques tienen un estado de anticipación antes de aplicar daño o emitir un proyectil.

## ¿Qué se guarda y qué ocurre con un archivo corrupto?

La partida 3D registra versión, checkpoint, IDs de fragmentos, acceso, jefe derrotado y tiempo. Antes de aplicar los datos se valida su formato e invariantes. Se escribe mediante archivo temporal y reemplazo, y existen pruebas para datos inválidos y restauración. La escena 2D conserva progreso durante la partida y no tiene persistencia entre ejecuciones.

## ¿La escena 2D cumple toda la AE4?

Demuestra Tilemap, sprites, Rigidbody2D, Collider2D, parallax, NPC autónomo e interactivo, vida, checkpoints, fragmentos, jefe y portal. El recorrido completo fue ejecutado mediante entradas normales. El cumplimiento académico definitivo depende de cotejar las consignas originales, que no fueron suministradas, y de la evaluación docente.

## ¿El tercer fragmento es realmente opcional?

Sí. La condición de salida distingue los dos IDs obligatorios. Tanto el recorrido 3D como el 2D comprobaron victoria con dos fragmentos. En 2D se corrigieron escalones que forzaban la ruta superior y se dejó un corredor abierto por debajo.

## ¿Qué evidencia existe fuera del Editor?

`Tests/results/windows-fullroute.xml` registra una prueba de recorrido 3D aprobada en el reproductor Windows de pruebas. Eso acredita esa ejecución técnica. El estado y verificación del build final de distribución deben registrarse por separado; no se deducen automáticamente del ejecutable de pruebas.

## ¿El bot usa trucos?

Los recorridos completos no escriben posición, salud, fragmentos, estado del escudo ni victoria. Usan teclado/mouse sintéticos, el comando normal de inicio y rotaciones/validación del circuito como la interfaz. Pueden leer posiciones para orientarse. Las pruebas de integración aisladas sí preparan estados para comprobar casos específicos; no se presentan como recorridos sin ayudas.

## ¿Se han probado 60 FPS y partidas de 10–20 minutos?

No. Son objetivos del encargo que necesitan profiling y partidas humanas. Los bots usan un paso temporal controlado y resuelven puzzles algorítmicamente; sus contadores y duración del XML no equivalen a FPS de rendimiento ni duración de una primera partida humana.

## ¿De dónde provienen los modelos, la música y la fuente?

Los modelos y clips se generan mediante scripts originales de Blender; el atlas 2D y el audio se generan en código. Rajdhani es una fuente de Indian Type Foundry distribuida bajo SIL Open Font License 1.1, cuyo texto se incluye en Resources/Fonts/OFL.txt. No se atribuye esa fuente al equipo ni se inventan licencias para el material recibido.

## ¿Qué aporta MDA y cómo se relaciona con Flow?

MDA permite explicar cómo salto/dash, circuitos y combate generan decisiones y dominio, y buscan desafío, descubrimiento y sensación. El Flow se persigue introduciendo las reglas gradualmente, con feedback y checkpoints. Su logro es una hipótesis de diseño pendiente de observar con personas.

## ¿Cuál es la limitación técnica principal del proyecto actual?

El nivel se ensambla mediante código y la escena 2D concentra varios sistemas en un componente. Funciona como slice verificable, pero una ampliación de campaña requerirá más herramientas de autoría, prefabs, separación de responsabilidades 2D, contenido, pruebas de rendimiento y playtests. No se declara un producto comercial final libre de errores.
