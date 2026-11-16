# Dirección artística — Nivel Cero: Escape del Glitch

Guía breve y aplicada. Rige la escena 2D (pixel art) y la 3D (formas industriales con materiales URP). Ambas comparten paleta, semántica de color, iconografía, tipografía y lenguaje de corrupción; difieren en resolución y volumen.

## 1. Idea rectora

**Una planta industrial digital, fría y funcional, en la que GL1TCH introduce errores.** El sistema sano es ordenado: módulos repetidos, ángulos rectos, señalética clara y luz de servicio. La corrupción rompe ese orden: desplaza bloques, duplica bordes y mancha superficies en violeta. Zero y Bit son pequeños, redondeados y cálidos de contorno; las defensas son pesadas, angulosas y rojas.

No se resuelve la atmósfera con más brillo. Bloom bajo, sin partículas ambientales permanentes, sin aberración cromática constante. Los efectos se reservan para eventos (daño, hack, fase del jefe) y pueden reducirse desde Opciones.

## 2. Paleta y significado

| Rol | Hex | Uso exclusivo |
|---|---|---|
| Estructura profunda | `#0B121A` `#121D28` | Fondo lejano, vacío, sombras. |
| Metal de servicio | `#1E2D3B` `#2E4354` `#4B6476` | Suelo, muros, plataformas. Valor medio para que el jugador recorte. |
| Borde transitable | `#8FA6B5` | Arista superior de todo lo que se puede pisar. |
| Aliado / interactivo | `#4FD8E0` (luz) `#1F7F8C` (sombra) | Zero, Bit, terminales, checkpoints, portal activo, guías de ruta. |
| Recompensa | `#FFC857` `#C9852B` | Fragmentos de código y nada más. |
| Amenaza | `#E8435A` `#8E1F33` | Ataques, proyectiles hostiles, firewall, peligros. Siempre con forma aguda o franjas. |
| Corrupción (GL1TCH) | `#9B4FD6` `#4A2266` | Manchas, bloques desplazados y texto del antagonista. Decorativa salvo que vaya con rojo. |
| Texto / UI | `#E6EEF2` `#9FB3C0` | Lectura. |

Regla de lectura: **cian = podés usarlo, ámbar = recogelo, rojo = te daña, violeta = el sistema está roto**. El rojo no se usa como decoración.

## 3. Forma antes que color

Cada categoría tiene también un rasgo de forma o movimiento, para no depender del color:

| Categoría | Forma | Movimiento | Icono |
|---|---|---|---|
| Interactivo | Marco rectangular con esquinas marcadas | Pulso lento de su luz | `[E]` sobre el objeto al estar cerca |
| Recompensa | Rombo facetado | Flota y rota | Rombo |
| Peligro | Triángulos, franjas diagonales | Parpadeo de aviso antes del daño | Triángulo `!` |
| Proyectil hostil | Núcleo con cola en punta | Traza recta, anunciada por retícula | — |
| Decoración | Rectángulos de bajo contraste, sin borde claro | Estática | — |
| Corrupción | Bloques desplazados horizontalmente | Salto discreto, no continuo | — |

## 4. Personajes

- **Zero**: casco redondeado claro, visor horizontal cian, cuerpo pizarra, cable trasero que sigue la inercia (rasgo asimétrico de silueta). Proporción compacta, cabeza grande, para leerse a 1,5 tiles de alto.
- **Bit**: dron esférico pequeño con un único ojo grande que expresa estados (atento, contento, alerta, preocupado), aletas laterales y antena. Siempre detrás del jugador, nunca delante del objetivo.
- **Guardián**: chasis trapezoidal pesado, hombros con placas, un núcleo-ojo central. Escudo cerrado = placas juntas y núcleo rojo apagado; vulnerable = placas abiertas y núcleo blanco-ámbar. Pierde placas al cambiar de fase.

## 5. Jerarquía del escenario

1. **Jugador, enemigo, proyectiles** — máximo contraste, contorno oscuro de 1 px.
2. **Superficies transitables** — arista superior clara `#8FA6B5`, cuerpo medio.
3. **Objetivos** — fragmento ámbar, terminal cian, portal.
4. **Arquitectura de primer plano** — tubos, consolas, baja saturación.
5. **Parallax** — tres capas cada vez más oscuras y azuladas; nunca más claras que el suelo.

Zonas 2D: **Arranque** (azul, limpio), **Archivo** (aparecen manchas violeta), **Acceso restringido** (señalética roja del firewall), **Núcleo de Seguridad** (luz baja, bordes rojos, arena cerrada), **Salida** (cian abierto cuando el portal se activa).

## 6. Tipografía e interfaz

Rajdhani Bold para títulos (mayúsculas), Rajdhani Regular para cuerpo. Paneles oscuros semiopacos con una línea superior de color de rol y esquinas en ángulo; sin degradados. Barras planas segmentadas. Botones: normal oscuro, foco con borde cian, pulsado relleno cian, deshabilitado en gris al 40 %. Instrucciones breves y contextuales, nunca sobre la acción central.

## 7. Lenguaje de corrupción y glitch

- Desplazamiento horizontal de franjas (2–4 px en 2D) y duplicado de contorno en violeta.
- Se usa sólo: al recibir daño (0,15 s), al fallar un hack, al cambiar de fase el Guardián y en mensajes de GL1TCH.
- **Opción "Reducir efectos"**: desactiva sacudida de cámara, destellos de pantalla y desplazamientos glitch; mantiene la información (iconos, texto y sonido).

## 8. Audio (resumen de identidad)

Exploración: pad grave y sostenido con pulso suave; combate: misma tonalidad con percusión metálica y bajo en ostinato. Aliado = timbres limpios y senoidales; amenaza = ruido filtrado, graves con caída de tono; recompensa = campanas ascendentes; error = intervalos disonantes cortos. Detalle en `Docs/Arte/AUDIO.md`.
