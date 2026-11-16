# Recursos 3D originales

El generador `Tools/Art/generate_models.py` crea mallas, materiales y animaciones originales mediante Blender 4.5 LTS. No descarga modelos ni incorpora recursos externos. El resultado es arte industrial estilizado y reproducible; su existencia no equivale a una validación visual dentro del juego.

## Archivos previstos

`Unity/Assets/NivelCero/Art/Models/` recibe Zero, PulseBlaster, EnergyBlade, Guardian, Bit, Terminal, FloorPanel, WallPanel, Pillar, DoorFrame, SecurityDoor, Crate, Pipe y SafetyRail como FBX separados. Incluye un manifiesto con versión real, conteos de triángulos y clips exportados. El archivo Blender editable está en `Tools/Art/Source/`, fuera de Assets para que Unity pueda importar los FBX sin exigir Blender instalado. Las placas de Zero se combinan en una malla con submallas de material y los pesos de huesos conservados.

Zero tiene una armadura segmentada de metal oscuro, visor continuo cian, articulaciones, piezas de protección, mochila y esqueleto mecánico con pesos por segmento. Las piezas se construyen con perfiles biselados, superficies curvas y placas ahusadas. Los clips son Idle, Walk, Run, Jump, Fall, Dash, Shoot, Reload, Melee1, Melee2, Melee3, Hurt, Death e Interact. Los movimientos son originales y procedurales; requieren revisión del Animator y del contacto con las armas en el juego.

## Importación Unity

- Zero: Animation Type `Generic`, Avatar Definition `Create From This Model`, raíz `Root`; root motion desactivado porque CharacterController desplaza al jugador. Loop Time en Idle, Walk, Run y Fall. Clips de acción sin loop.
- Escala de origen: metros. Exportación FBX con `axis_forward=-Z`, `axis_up=Y`, unidades aplicadas y sin leaf bones adicionales. Comprobar que Zero mira hacia el +Z local en Unity antes de conectar las armas.
- Materiales: nombres `NC_Armor`, `NC_Titanium`, `NC_Edge`, `NC_Rubber`, `NC_Cyan`, `NC_White`, `NC_Amber`, `NC_Red`, `NC_Violet`. Remapear a materiales URP/Lit; copiar color, metallic, roughness invertida como smoothness y emisión. Unity no conserva de manera fiable los nodos Principled de Blender.
- Collider del jugador: una cápsula/CharacterController, no colisiones de cada placa. Props: colliders sencillos independientes de la geometría de detalle; Generate Colliders solo si se comprueba el coste.
- Armas: el blaster se diseña con grip en el origen y cañón en -Y de Blender; la espada está alineada al Z vertical de Blender. Ajustar el socket de la mano en prefab y verificar disparo/alcance. No usar el movimiento del modelo para calcular daño.
- Bit y Guardián son mallas ensambladas con partes nombradas; su flotación, giro, exposición del núcleo y derrota pueden controlarse desde Unity sin simular piezas físicas.

## Ejecución

Desde la raíz del repositorio: `blender --background --python-exit-code 1 --python Tools/Art/generate_models.py`. El script exige Blender instalado y no lo instala. La salida `NIVEL_CERO_ART_OK` y `ArtManifest.json` sirven para verificar que terminó la exportación; no demuestran por sí solos que los assets estén importados, animados o probados en Unity.

Reimportación y validación de pesos, huesos y variación de poses de cada clip: añadir `-- --validate`. Lámina de inspección con Cycles: añadir `-- --render-inspection`. Las pruebas verifican variación de matrices de huesos en nueve tiempos por clip; no evalúan automáticamente la calidad artística de las poses.

## Estado de validación

Ejecutado con Blender 4.5.10 LTS: se generaron y reimportaron los 14 FBX. Zero conserva una malla con pesos en todos sus vértices, 18 huesos y los 14 clips. `ImportValidation.json` registra los resultados observados y `ArtManifest.json` los conteos de triángulos: Zero 17.716, Guardián 19.368 y Bit 3.268.

Se renderizó e inspeccionó `ArtInspection.png` desde la geometría real mediante Cycles. Esta imagen es una lámina de inspección de modelos, no una captura de gameplay. Pendiente: revisar orientación, materiales, animaciones y agarre de armas dentro de Unity y observarlos durante gameplay real.
