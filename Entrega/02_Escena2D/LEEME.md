# Escena académica 2D

La escena 2D forma parte del mismo proyecto y ejecutable que el Sector de Arranque 3D.

1. Abrir `../01_Videojuego3D/Windows/NivelCero.exe`.
2. Elegir **Escena académica 2D** en el menú principal.
3. Usar A/D, Espacio para saltar, Shift para dash, mouse para apuntar, clic izquierdo para atacar, 1/2 para cambiar arma, E para interactuar y Esc para pausa.

El recorrido incluye Tilemap y colisiones 2D reales, tres capas de parallax, Bit, checkpoints, fragmentos, circuito de rotación, firewall, Guardián de tres fases y portal. Dos fragmentos principales y el jefe son obligatorios. El fragmento de la ruta superior es opcional.

Para editar: extraer `../01_Videojuego3D/Nivel_Cero_Unity.zip`, agregar su carpeta `Unity` a Unity Hub y abrirla con **6000.3.28f1**. La escena está en `Assets/NivelCero/Scenes/AE4_2D_SectorDeArranque.unity`; el controlador está en `Assets/NivelCero/Scripts/Academic/Academic2D.cs`. La geometría Tilemap se genera al ejecutar esa escena; el atlas original está en `Assets/NivelCero/Art/Academic` y su generador en `Tools/GenerateTiles.ps1`.

El video real está en `../04_Gameplay/Nivel_Cero_Gameplay_2D.mp4`. Los XML del portafolio acreditan un recorrido automatizado completo tanto en Editor como en Windows, con entradas normales, circuito y combate. No acreditan una partida humana ni aprobación docente. Consultar la matriz AE4: las consignas académicas originales no fueron recibidas para su cotejo.
