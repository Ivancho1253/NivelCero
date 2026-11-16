# Guion de presentación oral · 20 minutos

Nivel Cero: Escape del Glitch · versión de guion del 16 de noviembre de 2026

El guion propone una exposición y demostración en vivo. No acredita que la exposición ya haya ocurrido. Preparar antes el build y los registros reales disponibles; si una captura o video todavía no está terminado, no anunciarlo como material entregado. Los nombres y aportes personales deben completarse únicamente con información comprobada.

| Tiempo | Duración | Bloque |
|---|---|---|
| 00:00–02:00 | 2 min | Identidad, historia y alcance. |
| 02:00–04:00 | 2 min | Migración y decisiones de diseño. |
| 04:00–07:00 | 3 min | Demostración 3D: movimiento y combate. |
| 07:00–10:00 | 3 min | Demostración 3D: hack, Guardián y salida. |
| 10:00–12:00 | 2 min | Arquitectura y física. |
| 12:00–14:00 | 2 min | IA, navegación y anticipación. |
| 14:00–16:00 | 2 min | Demostración de la escena académica 2D. |
| 16:00–18:00 | 2 min | Arte, audio y decisiones MDA. |
| 18:00–19:00 | 1 min | Pruebas y evidencia. |
| 19:00–20:00 | 1 min | Límites, continuidad académica y cierre. |

**Total: 20 minutos.** Las preguntas posteriores quedan fuera de este bloque; si el docente exige incluirlas, acordar otra distribución antes de presentar.

## 00:00–02:00 · Identidad y alcance

“Nivel Cero: Escape del Glitch es una experiencia de acción, plataformas y exploración. Zero despierta dentro de un sistema digital corrupto. Bit lo acompaña y GL1TCH controla la amenaza. Para escapar, hay que recuperar código, intervenir sistemas y derrotar al Guardián.”

Mostrar el título y explicar los cuatro sectores originales. Señalar expresamente que el mapa implementado es Sector de Arranque. Memoria Fragmentada, Torre Firewall y Núcleo Corrupto son continuidad narrativa y trabajo futuro.

“La evolución principal es 3D, pero conservamos una escena 2D completamente recorrible para demostrar las tecnologías académicas solicitadas. La aceptación del 3D como entrega principal depende del docente. Las consignas originales todavía necesitan cotejo.”

## 02:00–04:00 · De WebGL a Unity

Mostrar brevemente la estructura original y el respaldo, sin ejecutar funciones QA como evidencia de gameplay. Explicar tres problemas concretos: colisiones manuales limitadas al suelo, hack que cambiaba estado al pulsar E y daño basado en dash sin armas.

“La versión Unity usa CharacterController y física del motor, circuitos con conexiones recíprocas, armas con daño y obstrucción y guardado JSON. Conservamos la historia y cambiamos la implementación para integrar las mecánicas.”

Indicar Unity 6000.3.28f1, URP, Input System y AI Navigation. No afirmar que se usó Cinemachine o Shader Graph si la implementación no los utiliza.

## 04:00–07:00 · Movimiento y combate 3D

Iniciar desde Nueva partida. Mostrar W/A/S/D con mouse, alternar V y cambiar hombro con C. Hacer un salto y dash; explicar la carga aérea. Usar un obstáculo para mostrar que el personaje y la cámara respetan geometría.

Disparar al drone, mostrar consumo de cargador y recarga con R. Cambiar a espada con 2 y mostrar un ataque/bloqueo cuando sea seguro. Si no se consigue mostrar una acción durante el tiempo disponible, describir su estado real y remitir a su prueba; no fingir un resultado.

“La dirección se calcula sobre el plano de la cámara. El daño no depende sólo de una animación: hay alcance, colisiones y comprobaciones de paredes.”

## 07:00–10:00 · Circuitos y Guardián

Abrir una terminal con E. Antes de resolver, mostrar que una conexión incompleta no habilita el sistema. Girar módulos y conectar entrada/salida. Explicar puertos recíprocos y validación; el cursor está libre y el mundo pausado durante el puzzle.

Mostrar el cambio concreto del firewall. En la arena, explicar que el Guardián exige hack por fase, tiene exposición limitada y vuelve a protegerse. Evadir un ataque anunciado y atacar durante una ventana.

Si un recorrido humano excede el bloque, pasar a una captura real ya registrada o a un video real que efectivamente exista. Identificar ese cambio como reproducción de una sesión grabada. No usar teletransporte, invulnerabilidad ni derrota artificial para presentarlo como recorrido completo.

La demostración del final puede usar el registro automatizado como evidencia técnica, aclarando que no es una partida humana ni sustituye la demostración de control.

## 10:00–12:00 · Arquitectura y física

Mostrar un diagrama o el árbol de scripts: Core, Gameplay, World, Session, HUD y Academic2D. Explicar por qué CircuitPuzzle y ProgressData no dependen del motor.

“Session coordina estados y guardado; no calcula el movimiento ni los golpes. CharacterController mueve al avatar 3D; Rigidbody2D y Collider2D se usan en la escena académica. Los datos cargados se validan antes de modificar puertas, fragmentos o checkpoints.”

Explicar con un ejemplo el doble raycast del blaster: el objetivo de la cámara no autoriza atravesar una pared frente al avatar.

## 12:00–14:00 · IA y navegación

Mostrar estados de SecurityAgent y la superficie de navegación. Describir patrulla, detección con visibilidad, investigación de última posición, persecución, aviso y ataque.

“La unidad terrestre usa NavMeshAgent y una superficie construida con colliders al iniciar. El drone utiliza lógica de vuelo restringida por consultas físicas; la torreta es fija. No se trata de la misma navegación para los tres tipos.”

Mostrar un aviso del Guardián o láser de arena y explicar por qué anticipar el daño permite decisiones. No atribuir cobertura táctica coordinada ni razonamiento avanzado a esta IA.

## 14:00–16:00 · Escena académica 2D

Abrir `AE4_2D_SectorDeArranque` mediante el menú previsto. Mostrar atlas, Tilemap y Rigidbody2D en el proyecto y luego el juego. Recorrer un hueco, señalar parallax y hablar con Bit. Mostrar el circuito que abre el firewall.

Explicar que existen dos fragmentos principales y uno opcional en una plataforma superior. Indicar que una prueba automatizada completó esta escena sin recoger el opcional y sin morir. Si el tiempo no permite terminarla en vivo, mostrar el registro real de la prueba y aclarar su naturaleza.

## 16:00–18:00 · Arte, sonido y experiencia

Mostrar Zero, las armas, Bit y Guardián como modelos originales de Blender, distinguiendo lámina de inspección y captura de gameplay. Señalar rig, clips y materiales URP. Acreditar Rajdhani y su licencia OFL incluida.

Explicar música sintetizada y transición de exploración a jefe. Presentar dos cadenas MDA: circuitos → decisiones/corrección → descubrimiento; salto/dash → dominio del movimiento → desafío y sensación.

Relacionar competencia, autonomía y relación con aprendizaje, ruta opcional y Bit. Hablar de Flow como intención pendiente de comprobar con personas.

## 18:00–19:00 · Evidencia técnica

Mostrar la tabla de `Bitacora.md` y los XML: 10 EditMode, 11 integración, una escena completa, un recorrido 3D en Editor, uno 3D Windows y uno 2D. Son resultados de suites independientes, no pruebas únicas sumadas ni encuestas de usuarios.

“Los recorridos usan entradas normales y comandos reales del circuito. Las pruebas aisladas sí preparan estados para explorar casos particulares. Ambas evidencias responden preguntas distintas.”

## 19:00–20:00 · Cierre honesto

“El resultado comprobado es un primer sector recorrible en 3D y una experiencia académica 2D, con combate y hack integrados. La muestra breve a 1080p dio 59,87 FPS promedio en la RTX 4060 utilizada. Quedan pendientes playtests humanos, profiling extenso y cotejo de consignas. No afirmamos que se hayan validado 10–20 minutos por partida ni un mínimo constante de 60 FPS en otras PC.”

Indicar el estado real del build final y del video al momento de exponer; este guion no los certifica. Cerrar mostrando los próximos sectores como continuidad del concepto y no como contenido terminado.
