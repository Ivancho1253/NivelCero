# Revisión contra la consigna original AE4

Fecha: 16 de noviembre de 2026. Proyecto: Nivel Cero: Escape del Glitch.

## Dictamen

La entrega tiene una base técnica importante y los archivos principales ya existen. No se puede afirmar cumplimiento total: faltan componentes expresamente enumerados para el NPC 2D, el encuentro del jefe 2D tiene una realización audiovisual básica, el video 2D no alcanza la duración mínima y el GDD/portafolio no cubren con suficiente detalle todos los aspectos teóricos y grupales solicitados. También hay que reconciliar la evolución desde AE1 y AE2 con el producto actual.

La recomendación es presentar la escena 2D como evidencia directa de las actividades de proceso y el 3D como evolución adicional, salvo acuerdo con la cátedra. La página 3 permite elegir motor para el prototipo, pero la página 6 pide específicamente Unity, Tilemaps, player, parallax y NPC 2D. Tener un 3D no elimina esas actividades.

## Fuentes y alcance de verificación

- Consigna: `C:/Users/coqui/Downloads/AE4.pdf`, 7 páginas; páginas 6 y 7 también revisadas visualmente.
- Antecedente: `C:/Users/coqui/Downloads/AE1_DDV_Cabrera_Flores (1).pdf`, 14 páginas.
- Antecedente: `C:/Users/coqui/Downloads/AE2_DDV_IvanCabrera_JoaquinFlores (2).pdf`, 17 páginas.
- Entrega actual: `Entrega/03_GDD/Nivel_Cero_GDD.pdf`, 9 páginas; documentación, fuentes C#, escenas y configuración del proyecto.
- Evidencia existente: `Entrega/05_Portafolio/Pruebas/windows-final-routes.xml` registra 2 recorridos aprobados, 0 fallidos. Se leyó el registro; no se volvió a ejecutar Unity ni se jugó una partida nueva en esta revisión.
- FFprobe ejecutado durante esta revisión: MP4 3D = 111,502417 segundos; MP4 2D = 30,887583 segundos.

Los documentos se trataron como material para evaluar, no como autorización para implementar cambios, subir entregas o dar por cumplida una exposición. No se modificó el juego ni sus entregables anteriores. Este informe no certifica ausencia de errores críticos ni anticipa una nota.

## Matriz de la consigna

| Requisito y ubicación en AE4 | Estado observado | Qué falta o cómo cerrar |
|---|---|---|
| Prototipo Windows .exe de un nivel completo; físicas, IA, lógica y UI/UX (pp. 3, 5 y 6) | Existe `Entrega/01_Videojuego3D/Windows/NivelCero.exe`, datos y DLL. Las dos escenas figuran habilitadas en Build Settings. Hay lógica de inicio, progresión, derrota y victoria. Los registros existentes acreditan recorridos automatizados. | Completar una partida humana del ejecutable final, revisar muerte, reintento, pausa y reinicio. La estabilidad y el pulido no quedan certificados por los XML. Mantener toda la carpeta Windows al distribuir. |
| Primer montaje 2D en Unity con Tilesheet y Tilemap (p. 6) | Implementado en `Academic2D.Start` y `BuildTilemap`: atlas, Grid, Tilemap, TilemapRenderer y TilemapCollider2D. | La geometría se crea al ejecutar. La escena guardada contiene solamente el objeto constructor y la referencia al atlas. Para mostrar un mapa pintado y configurable en edición, conviene serializar el Tilemap o preparar una demostración clara de la generación. La consigna no prohíbe generar por código; esto es un riesgo de demostración/interpretación, no una ausencia de Tilemap. |
| Player 2D con entradas y físicas (p. 6) | Implementado: Rigidbody2D, CapsuleCollider2D, A/D, salto, dash, gravedad y colisiones. | Validación humana de controles y reaparición. AE2 también documentaba flechas; el controlador actual consulta A/D. Incorporarlas o actualizar el GDD final. |
| Parallax con múltiples fondos (p. 6) | Implementado: tres capas con factores de desplazamiento distintos y cámara de seguimiento. | Mostrar el movimiento de las capas en la demo/video; valorar legibilidad con el escenario en movimiento. |
| NPC 2D con propósito, assets, GameObject, Sprite Renderer, Collider2D, Rigidbody2D, autonomía C# e interacción (p. 6) | Parcial. Bit tiene GameObject/SpriteRenderer, sigue al jugador y responde a E según distancia. | Bit no recibe Collider2D ni Rigidbody2D. `Visual()` solamente crea objeto y renderer; después de crear Bit no se agregan esos componentes. El Rigidbody2D del player no demuestra los componentes del NPC. Incorporar una configuración física apropiada al acompañante, por ejemplo cuerpo cinemático sin gravedad y collider trigger, y verificar que no obstruya al jugador. |
| Diseño integral del jefe del primer nivel: mecánicas, arte, animación, programación y sonido (p. 6) | Parcial en 2D. Hay sprite del Guardián, vida, tres fases por umbral, aviso visual, proyectiles, daño y derrota. | El jefe usa una sola imagen, oscilación vertical y cambios de color; no hay secuencia de sprites ni Animator. Esto constituye animación básica, pero aporta poca evidencia de producción integral. No se encontró audio propio en su ataque o muerte en `UpdateBoss`: los sonidos 2D presentes corresponden a jugador, recolección, daño y hack. Desarrollar y documentar animaciones y señales sonoras del jefe. La consigna no exige Animator ni una cantidad determinada de clips. |
| GDD completo con visión total, PDF y copia impresa (pp. 3, 4 y 6) | PDF y DOCX presentes. El PDF final explica implementación, mundo, cuatro sectores narrativos y alcance de un nivel. | Completar fundamentos pendientes, integrar antecedentes y corregir afirmaciones desactualizadas de que no se recibieron las consignas. Añadir portada con ambos autores y datos de cursada. Preparar impresión y verificar subida al aula; esos pasos no están acreditados localmente. |
| MDA, Estructuras Lúdicas, Autodeterminación y Psicología del Jugador (pp. 3 y 4) | Parcial. El GDD final desarrolla MDA, Flow y competencia/autonomía/relación. | Faltan un análisis explícito y completo de estructuras lúdicas y un perfil/recorrido del jugador actualizado. Relacionar participantes, objetivos, procedimientos, reglas, recursos, conflicto, límites y resultados con decisiones concretas. Recuperar público objetivo, motivaciones, frustraciones y referentes de AE1/AE2, adaptados al juego actual. Flow por sí solo no desarrolla todo el apartado de psicología. |
| Video MP4 de 1 a 3 minutos, gameplay real, sin edición excesiva (pp. 3, 5 y 7) | El 3D cumple duración: 1 min 51,5 s. El 2D dura 30,9 s y no cumple como video principal autónomo de esa versión. | Si se presenta 2D como prototipo académico, grabar 60-180 s que muestren plataformas, parallax, Bit, fragmentos, hack, jefe y cierre. Si se acepta 3D como principal, su duración ya es válida y el clip 2D puede ser complemento. La consigna no exige un video por escena ni prohíbe entradas automatizadas; debe quedar clara la procedencia de las tomas. |
| Gestión de preproducción, producción y pulido; bitácora grupal, cronograma, aportes individuales y metodologías ágiles (pp. 3, 5 y 7) | Parcial. Hay bitácora técnica con decisiones, pruebas, fallos y correcciones. | No hay reparto acreditado entre Iván y Joaquín, cronograma grupal con responsables/fechas y registro concreto de la metodología utilizada. Completar con hechos reales: tareas, responsable, período, estado, evidencia y revisiones. No es obligatorio Scrum ni inventar sprints; un registro Kanban real puede mostrar el proceso. |
| Fichaje bibliográfico, referentes, búsqueda de recursos y práctica del motor (p. 7) | AE1/AE2 incluyen referencias y comparaciones; el repositorio documenta arte original, licencia de fuente y herramientas. | Integrar bibliografía y fichas al portafolio final: autor/recurso, idea recuperada, aplicación al juego y procedencia/licencia cuando corresponda. El PDF GDD actual no tiene sección bibliográfica. No es necesario descargar assets para cumplir: se puede documentar la investigación y creación propia. |
| Exposición de 20 minutos más preguntas, defensa del GDD y live demo (pp. 3, 5 y 7) | Hay HTML/PDF de presentación, guion y preguntas técnicas. | Ensayar con ambos integrantes, distribuir intervenciones y demostrar la escena 2D. El guion actual se concentra en el 3D. La exposición y las respuestas sólo pueden acreditarse al realizarlas. |
| Entrega por aula virtual (pp. 6 y 7) | Hay archivos locales. | No se verificó subida. Presentar PDF final, distribución ejecutable completa y MP4 en el formato que admita el aula. Consultar límites de tamaño sin separar el EXE de sus dependencias. |

## Continuidad entre AE1, AE2 y la entrega actual

Los cambios de diseño no son automáticamente incumplimientos. La AE4 evalúa su fundamentación y la visión integral; hay que explicarlos y mantener consistente el documento final.

| Aspecto | Antecedentes | Implementación actual / ajuste documental |
|---|---|---|
| Formato y experiencia | AE1: tablero competitivo 5x5, 2-4 jugadores, tres acciones por turno, 20-30 minutos. AE2: plataformas 2D para un jugador. | El cambio mesa -> videojuego individual ya se produce en AE2. No hace falta añadir multijugador ni turnos al prototipo para reproducir AE1. Explicar qué se conservó: fragmentos, decisiones de ruta, peligros, checkpoints y portal. |
| Perspectiva y arte | AE2: cámara lateral y pixel art 2D. | Ahora el proyecto prioriza FPS/TPS 3D y conserva una escena 2D. Explicar motivo y alcance del cambio, y distinguir qué experiencia demuestra cada actividad. |
| Integridad | AE2 pp. 6 y 15: tres puntos, daño de uno. | 2D usa 100 HP y daño de 18 por proyectil del jefe; las reglas 3D también son numéricas. Documentar los valores finales y justificar el cambio, o restaurar el sistema original. |
| Aprendizaje y habilidades | AE2 pp. 10, 14 y 15: movimiento/salto en nivel 1, dash después y hack en nivel 3. | Ambas habilidades ya están disponibles en el primer nivel. Explicar que el prototipo concentra el core loop y que la progresión de la campaña está pendiente de diseño/implementación. |
| Hack | AE2 p. 8: altera temporalmente plataformas, láseres o barreras. | En 2D el puzzle abre permanentemente un firewall; en 3D también expone temporalmente al jefe. Explicar la evolución o incorporar una intervención temporal del escenario si se desea conservar esa promesa. |
| Primer nivel | AE2 pp. 12-14: tutorial, tres fragmentos (dos principales y uno opcional), Bit de Memoria opcional y salida; sin habilidades avanzadas. | Se conservan fragmentos/portal/checkpoints, se agrega jefe y se adelantan habilidades. El jefe de nivel 1 responde a una exigencia explícita de AE4. No se identificó un coleccionable separado de Bit de Memoria en el 2D: implementarlo o marcarlo como recortado. |
| Duración y campaña | AE2 contiene objetivos de 10-15 min por nivel y también 5-15 min en su resumen; cuatro sectores. | La duración humana sigue sin medirse. La AE4 pide un nivel completo, no los cuatro. No hace falta construir la campaña entera para cubrir el requisito mínimo, pero sí describir su visión en el GDD. |
| Equipo | AE1 firmado por ambos. AE2 tiene portada de ambos, pero restricciones dicen proyecto individual y atribuyen el trabajo a Iván. | Corregir esa inconsistencia en la síntesis final con aportes reales de ambos. No atribuir autorías por inferencia. |
| Playtesting | AE1 pp. 13-14 lo identifica como simulación del tablero. | No demuestra comodidad, balance o duración del ejecutable actual. Requiere observación específica del videojuego si se quieren sostener esas conclusiones. |

## Orden recomendado para cerrar

1. Completar los componentes del NPC 2D y validar su seguimiento/interacción sin interferencias físicas.
2. Reforzar presentación, animación y audio del Guardián 2D; registrar su diseño y sus patrones.
3. Grabar gameplay 2D de 1-3 minutos si será la versión principal de evaluación. El video 3D existente ya cumple el rango de duración.
4. Consolidar el GDD final: estructuras lúdicas, psicología/público, bibliografía, visión de campaña y comparación de decisiones AE1 -> AE2 -> AE4. Actualizar fuentes y exportar otra vez PDF/DOCX.
5. Completar cronograma, aportes de cada integrante y registro real de metodología en el portafolio.
6. Ejecutar una partida humana completa de la distribución final y registrar incidencias. Incluir caída, muerte por jefe, respawn, pausa, reinicio y victoria; comprobar en otra PC si está disponible. Es una recomendación de validación del requisito de estabilidad, no un número de playtests exigido por la consigna.
7. Ensayar 20 minutos de exposición más preguntas, imprimir el GDD y verificar las subidas.

## Aclaraciones para evitar trabajo innecesario

- AE4 no exige cuatro niveles implementados ni una duración concreta del nivel. Los tiempos de sesión provienen del diseño previo.
- No exige FPS/TPS, dos armas, NavMesh, guardado en nube, una cifra de FPS o un número de pruebas automáticas. Esas funciones pueden aportar calidad, pero no sustituyen las actividades 2D.
- No exige Animator, Tile Palette o pintado manual de forma explícita. La generación por código y la animación procedimental son defendibles si se demuestra su funcionamiento y calidad.
- El video 3D no está fuera de rango. El problema de duración corresponde al clip 2D si se usa como entrega principal.
- La matriz anterior `Docs/MATRIZ_AE4.md` era provisional y reconoce que no se habían recibido los originales. Este informe hace el cotejo con los PDF ahora suministrados; conviene actualizar luego las copias de entrega para evitar contradicciones.
