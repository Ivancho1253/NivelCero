# Nivel Cero: Escape del Glitch

Migración inicial de WebGL 0.5.0 a Unity. **Estado: fuente de reconstrucción 0.6-dev; no es el vertical slice final ni una build verificada.**

El juego web original permanece intacto en la raíz. `JUGAR_NIVEL_CERO.bat` sigue abriendo esa versión, no el proyecto nuevo.

## Abrir el proyecto Unity

1. Agregar la carpeta `Unity` a Unity Hub en una computadora con Unity **6000.3.3f1 (6.3 LTS)**. No se instaló software en este equipo por indicación del usuario.
2. Permitir que Package Manager resuelva `Packages/manifest.json`. URP 17.3.0 corresponde a esta familia del Editor; Input System 1.17.0 y Test Framework 1.4.6 están fijados. La resolución real queda pendiente.
3. La primera importación ejecuta `ProjectSetup`: configura URP, Input System, shaders, sprites y las dos escenas de compilación. Si Unity pide reiniciar para activar Input System, aceptar. También está disponible `Nivel Cero > Preparar proyecto`.
4. Abrir `Assets/NivelCero/Scenes/SectorDeArranque3D.unity` y pulsar Play. El menú permite iniciar 3D o abrir la escena 2D. El escenario se construye al ejecutar: las escenas contienen sus componentes de arranque y referencias, no un nivel artístico ya horneado.
5. Para la alternativa académica, abrir `AE4_2D_SectorDeArranque.unity` y pulsar Play.

## Controles 3D

WASD mover; mouse mirar; espacio saltar; Shift dash; Ctrl agacharse; V primera/tercera persona; C hombro; 1 arma de fuego; 2 espada; clic izquierdo atacar; clic derecho apuntar/bloquear; R recargar; E terminal/Bit; Esc pausa.

En el hack, cada clic rota una celda. Conectar el puerto oeste de la primera celda con el puerto este de la última y pulsar **Validar circuito**. Cancelar o agotar el tiempo bloquea temporalmente esa terminal. Los circuitos exigen conexiones recíprocas; se admiten derivaciones sin conectar.

## Recorrido implementado en código, pendiente de probar en Unity

Despertar con Bit → salto/dash entre pasarelas → primer fragmento → vigilancia → terminal del firewall → ruta lateral con segundo fragmento → checkpoint de arena → hack de ALFA o BETA → 18 segundos de vulnerabilidad del Guardián → repetir hasta derrotarlo → portal. La primera ruta lateral contiene el tercer fragmento, opcional. El juego preserva los nombres de los cuatro sectores; únicamente se reconstruye el Sector de Arranque.

## Verificación disponible

Desde la raíz, ejecutar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tests/Run-CoreChecks.ps1
```

Resultado real de esta sesión: **PASS: 7619 assertions; 160 independently solved circuit boards; progression invariants.** Se compila y ejecuta el C# de lógica con `Add-Type`; no se simula Unity. Hay siete tests EditMode adicionales en `Assets/NivelCero/Tests/Editor`, todavía sin ejecutar.

Para compilar posteriormente con un Editor instalado:

```powershell
./Tools/Build-Windows.ps1 -UnityEditor 'C:\ruta\al\Editor\Unity.exe'
```

También se puede usar `Nivel Cero > Compilar Windows x64`. El script comprueba el resultado de BuildPipeline y la existencia del EXE. **No se ejecutó aquí y no hay EXE entregado.** Copiar siempre la carpeta de build completa, incluyendo datos y DLL. Probarla fuera del Editor antes de distribuirla.

## Límites actuales

- Modelos ensamblados, armas, HUD IMGUI y arquitectura modular de geometría provisional. No representan el acabado visual solicitado. No hay animaciones Animator finales, música adaptativa ni modelos PBR de producción.
- IA con estados, colisiones y visibilidad local; todavía sin NavMesh ni búsqueda de rutas. Debe probarse y sustituirse donde se atasque.
- Jefe con tres escalones de agresividad y escudo por hack; las defensas ambientales, puntos débiles diferenciados y animación de derrota siguen pendientes.
- La alarma de hack aplica cooldown; no hay aún oleadas de refuerzo. Las terminales del jefe pueden reutilizarse.
- Guardado JSON local versionado en `Application.persistentDataPath`, escritura temporal y respaldo del archivo previo; falta probar IO/carga dentro de Unity. No guarda posición arbitraria, munición o cada enemigo: restaura checkpoint y progresión.
- Opciones de sensibilidad, FOV, inversión y volumen persistentes; resolución/pantalla/calidad se ajustan para la sesión y requieren validación.
- No se midieron duración de partida, rendimiento, 1080p/60 FPS, clipping ni recorrido completo. No se ejecutó ninguna escena Unity.
- No se generaron GDD Word/PDF finales, videos ni presentación: deben describir una versión ejecutada y validada, según el orden de prioridades solicitado.

Ver [auditoría y decisiones](Docs/AUDITORIA.md), [matriz académica](Docs/MATRIZ_AE4.md) y [pruebas pendientes](Docs/VALIDACION.md).
