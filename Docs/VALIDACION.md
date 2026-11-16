# Registro verificable

## Ejecutado

- Respaldo ZIP y SHA256 antes de modificar el proyecto.
- Verificación de herramientas en PATH, ubicaciones habituales y registro: Unity/Hub/Blender ausentes.
- Consulta de versión Editor y paquetes en fuentes oficiales.
- `Tests/Run-CoreChecks.ps1`: C# real de Core compilado mediante Add-Type. Resultado: 7619 aserciones, 160 circuitos resueltos independientemente, tamaños 2–5, 40 semillas por tamaño. Cubre reciprocidad, salida, rotación, reinicio, determinismo, errores de límites y progresión/archivos inválidos.
- Generación del atlas PNG original 256×32; ocho celdas con píxeles visibles.
- `Tools/Check-Project.ps1`: JSON de manifiesto/assemblies válido, 33 GUID de assets únicos, referencias de ambas escenas resueltas y los ocho archivos WebGL sin cambios respecto al commit de respaldo. Es una comprobación estructural; no compila Unity ni verifica la importación YAML.

Los tests Core no validan la UI Unity, el renderizado ni el guardado en disco de Session. Los tests NUnit EditMode están escritos y no ejecutados.

## Primera validación necesaria con Unity

1. Importar proyecto y resolver paquetes; comprobar consola sin errores de compilación. Ejecutar `Nivel Cero > Preparar proyecto`, reiniciar Editor si Input System lo solicita.
2. Ejecutar Test Runner > EditMode. Conservar XML y log reales; corregir cualquier fallo antes de continuar.
3. Recorrer ambas escenas desde sus menús. No usar teletransporte ni editar vida/fragmentos durante la prueba de terminación.

| Área | Prueba manual / PlayMode necesaria | Estado |
|---|---|---|
| Dirección | W/S/A/D a yaw 0/90/180 en FPS y TPS; diagonal a igual velocidad | Pendiente |
| Cámara | Mouse, V, C, apuntado, paredes, FPS sin cabeza visible | Pendiente |
| Movimiento | Saltos, techo, crouch, dash aéreo único, recuperación en suelo | Pendiente |
| Combate | Impacto real, pared cámara/muzzle, recarga, alcance/combo, bloqueo frontal | Pendiente |
| Salud | Recibir daño, morir, respawn, limpiar proyectiles | Pendiente |
| Hack | Rotar, ruta incorrecta no abre, solución abre, timeout/cancelación/cooldown | Pendiente |
| Jefe | Escudo invulnerable, hack abre 18 s, vuelve escudo, tres escalones de ataque, derrota | Pendiente |
| IA | Patrulla, detección, pérdida de visión, persecución, paredes, muerte, ausencia de atasco | Pendiente |
| Progresión | Dos principales, opcional, checkpoints, guardar/cerrar/cargar, archivo corrupto, portal | Pendiente |
| Estado | Pausa, foco de ventana, cursor en hack/menús, reanudar sin salto de cámara | Pendiente |
| 2D | Tilemap colisiona, salto/dash, parallax, Bit E, boss, portal y muerte | Pendiente |
| Entorno | Shader/materiales sin magenta, audio, Windows EXE fuera del Editor | Pendiente |
| Calidad | Duración 10–20 min, frame time/GPU a 1080p, opciones/pantalla completa | Pendiente |

## Pendientes de producto

Tras confirmar el recorrido, reemplazar arte provisional, autorizar/importar modelos con licencia verificable, rig y Animator, navegación terrestre, defensas de arena, VFX/audio/música, UI final, prefabs y escenas editables. Luego realizar playtesting, profiling, compilación real y verificación de Windows. Sólo después preparar GDD Word/PDF, video real de 1–3 minutos, bitácora con capturas verificadas y presentación de 20 minutos.

No se declara completada la definición de terminado del encargo. No existe evidencia de jugabilidad Unity hasta ejecutar estos pasos.
