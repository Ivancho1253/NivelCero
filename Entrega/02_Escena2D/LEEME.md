# Escena académica 2D (versión 0.7)

La escena 2D forma parte del mismo proyecto y del mismo ejecutable que el Sector de Arranque 3D.

1. Abrir `../01_Videojuego3D/Windows/NivelCero.exe`.
2. Elegir **Escena académica 2D** en el menú principal.
3. Controles: A/D o flechas para moverse, Espacio o W para saltar (mantener = salto más alto), Shift para el dash, mouse para apuntar, clic izquierdo para atacar, 1/2 para cambiar de arma, E para usar la terminal o hablar con Bit, Esc para pausar o cancelar el hack. En Opciones: volumen general, música, efectos y reducir efectos.

## Qué demuestra

- **Tilemap**: seis Tilemaps pintados y guardados en la escena (suelo con `TilemapCollider2D`, pasarela de un sentido con `PlatformEffector2D`, fosos, fondo, decoración, corrupción). Tiles editables en `Assets/NivelCero/Art/Academic/Tiles`.
- **Jugador con físicas**: `Rigidbody2D` y `CapsuleCollider2D`; animaciones de reposo, carrera, salto, caída, aterrizaje, dash, daño y muerte; brazo apuntado en 9 ángulos.
- **Parallax**: tres capas pintadas con factores distintos, teñidas por zona.
- **NPC Bit**: `SpriteRenderer`, `Rigidbody2D` cinemático, `CircleCollider2D` trigger y zona de interacción por triggers; sigue, guía hacia fragmentos y terminal, reacciona y habla con E.
- **Jefe del nivel 1**: el Guardián, con presentación, tres fases, ataques anunciados, ventanas de núcleo expuesto, sonidos propios y animación de muerte.

El diseño completo está en el GDD (§14). El arte y su generador se describen en `Assets/NivelCero/Art/Academic/README.md`, y el audio en `Docs/Arte/AUDIO.md` del proyecto.

## Editar

Extraer `../01_Videojuego3D/Nivel_Cero_Unity.zip`, agregar su carpeta `Unity` a Unity Hub y abrirla con **6000.3.28f1**. Escena: `Assets/NivelCero/Scenes/AE4_2D_SectorDeArranque.unity`. Código: `Assets/NivelCero/Scripts/Academic/`. Para regenerar los Tilemaps desde el diseño: `Nivel Cero > Construir escena 2D editable`.

Las pruebas automatizadas recorren la escena con teclado y mouse sintéticos. No equivalen a partidas humanas ni a una aprobación docente.
