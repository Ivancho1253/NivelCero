# Identidad sonora — Nivel Cero 0.7

Todo el audio es **original**: se sintetiza con `Tools/Audio/generate_audio.py` (Python + numpy, semillas fijas, reproducible) y se guarda como WAV PCM 16 bit / 44,1 kHz en `Unity/Assets/NivelCero/Resources/Audio/`. No se usaron muestras, loops, presets ni servicios externos; no hay licencias de terceros que registrar. Ejecutar el juego no requiere ningún servicio.

## Criterios

| Función | Timbre | Duración / dinámica |
|---|---|---|
| Aliado, interfaz, Bit | Senos y triángulos limpios, FM suave en la "voz" de Bit | Cortos, ataque rápido, picos −9 a −14 dBFS |
| Movimiento de Zero | Ruido filtrado metálico (pasos), barrido de ruido + sub (dash), golpe grave (aterrizaje) | 0,15–0,35 s; pasos en 3 variantes con ±6 % de tono |
| Armas | Cuadrada con caída de tono + clic (pulso); barrido de ruido + zumbido (hoja) | 0,2–0,3 s |
| Amenaza | Sierras filtradas, ruido, graves con caída de tono, distorsión suave | Más largos y fuertes (−3 a −7 dBFS) |
| Corrupción / GL1TCH | Bitcrush, cortes rítmicos de compuerta | Irregular |
| Recompensa | Campanas en arpegio ascendente (re mayor sobre la tónica re) | 1–2 s con reverberación |
| Error | Segunda menor disonante repetida | 0,46 s |

## Catálogo

**Música** (misma tonalidad re menor y 84 BPM para mezclar sin choques): `music_explore` (45,7 s, pad + pulso arpegiado + campana cada 4 compases), `music_boss` (22,9 s, ostinato de bajo, bombo, caja metálica, platillos, golpes de segunda menor), `amb_system` (24 s, zumbido 50/100 Hz, aire filtrado, golpes metálicos lejanos y servos). Los bucles pliegan su cola de reverberación al inicio (sin cortes en la unión; salto medido < 0,02 de amplitud).

**Escena y jugador**: `step_0..2`, `jump`, `land`, `dash`, `hurt`, `death`, `respawn`.
**Armas e impactos**: `blaster`, `blade`, `hit_enemy`, `deflect`, `enemy_shot`.
**Guardián**: `boss_activate`, `boss_charge`, `boss_fire`, `boss_slam`, `boss_hurt`, `boss_phase`, `boss_death`, `shield_open`, `shockwave`, `block_fall`, `block_impact`, `door_close`.
**Sistema**: `terminal_open`, `rotate`, `hack_error`, `hack_success`, `alarm`, `fragment`, `checkpoint`, `portal_open`, `portal_enter`, `firewall_loop`.
**Interfaz**: `ui_move`, `ui_confirm`, `ui_back`, `pause_in`, `pause_out`, `stinger_victory`, `stinger_defeat`.
**Voces**: `bit_chirp_0..2`, `glitch_voice_0..1`.

## Mezcla e implementación

- `SoundBank` (efectos): pool de 18 voces 2D y 12 voces 3D, variación de tono ±4 % por defecto, intervalo mínimo por sonido para evitar ráfagas idénticas y robo de la voz más antigua en vez de apilar (evita saturación).
- `MusicDirector`: fundido cruzado exploración ↔ combate (entrada lenta 0,35/s, salida 0,7/s), ambiente siempre debajo y filtro pasa-bajos (900 Hz) en pausa, menús y hack. Stingers de victoria y derrota.
- 3D: efectos de enemigos, Guardián, portal y pasos se reproducen posicionados (rolloff logarítmico, 3–40 m); la interfaz y el feedback del jugador son 2D.
- Normalización en origen: música −5/−6 dBFS, ambiente −10, efectos de −3 (jefe) a −14 (interfaz). Importación: música en Streaming/Vorbis; efectos descomprimidos.
- Controles separados en Opciones (ambas escenas): volumen general, **música** y **efectos**, guardados en `PlayerPrefs`.

## Verificación

Se comprobaron automáticamente duración, picos y uniones de bucle (salida del generador). Los clips se importaron y reprodujeron en las grabaciones de Unity. **La calidad percibida de la mezcla (equilibrio entre música y efectos, fatiga, preferencias) requiere escucha humana**: no se acredita con estas pruebas.
