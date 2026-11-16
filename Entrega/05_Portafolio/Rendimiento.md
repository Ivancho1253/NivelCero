# Medición breve de cadencia de cuadros

Fecha: 16 de noviembre de 2026. Evidencia: `performance-1080p.json` y `windows-performance-1080p-route.xml` en las pruebas del portafolio.

Se ejecutó el recorrido 3D completo hasta victoria en un reproductor Windows de pruebas, sin captura PNG ni audio. Se midieron intervalos reales entre `Update` mediante `System.Diagnostics.Stopwatch`; no se usaron `Time.deltaTime`, el tiempo del runner ni los FPS del video para calcular rendimiento. El bot conserva un paso de simulación fijo de 1/60 s.

| Condición | Valor |
|---|---|
| CPU | Intel Core i5-14600KF |
| GPU / API | NVIDIA GeForce RTX 4060 / Direct3D 12 |
| Sistema | Windows 11, 10.0.26100 |
| Resolución real comprobada | 1920 × 1080, ventana |
| Límite / VSync | 60 FPS / desactivado |
| Calentamiento descartado | 120 cuadros |
| Muestra | 1985 intervalos, 33,156 segundos reales |
| Promedio | 59,868 FPS; 16,703 ms por cuadro |
| Mediana | 16,667 ms |
| Percentil 95 / 99 | 16,993 / 18,778 ms |
| Máximo observado | 39,553 ms |
| Resultado funcional | Victoria, prueba aprobada |

La muestra incluye el limitador de cuadros y la instrumentación normal del reproductor de pruebas. No separa CPU/GPU ni constituye un informe completo del Profiler. No acredita un mínimo constante de 60 FPS, sesiones largas, otras configuraciones o rendimiento universal. La primera muestra, a 1078×904, se conserva aparte y no se presenta como 1080p. La duración del recorrido automatizado tampoco mide una primera partida humana.
