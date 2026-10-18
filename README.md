# NO MIRES DOS VECES

Juego de terror psicológico en primera persona para Game Jam. Tema: **Something's Off**.
Duración aproximada: 4–7 minutos. Unity **6000.5.5f1** · C# · sin assets externos (todo son cubos, materiales de color y sonidos generados por código).

## Estado de verificación

- Solo se hizo una comprobación **básica y automática** fuera de Unity: delimitadores balanceados y referencias cruzadas entre scripts (que cada miembro usado exista). Eso **no equivale a compilar**: no hubo compilador de C# ni analizador sintáctico disponible.
- **No se ha compilado ni jugado dentro de Unity 6000.5.5f1.** Pueden aparecer errores de compilación o de comportamiento. Antes de dar el juego por terminado, sigue la lista de pruebas de más abajo y corrige cualquier error de consola (si te aparece alguno, pásamelo y lo arreglamos).
- No se ha probado la compilación de Windows (`.exe`).
- Los nombres de las propiedades de `PlayerSettings` del script de Editor están escritos de memoria para Unity 6; si alguna falla al compilar, bórrala de `NoMiresDosVecesTools.cs` (son opcionales).

## Cómo instalarlo

1. Instala **Unity Hub** y el editor **6000.5.5f1**.
2. En Unity Hub: *New project* → plantilla **Universal 3D** (o *3D (Built-in)*). Nómbralo `NoMiresDosVeces`.
3. Cierra Unity o déjalo abierto. Copia la carpeta **`Assets/`** de este paquete **dentro** de la carpeta `Assets/` de tu proyecto, fusionando carpetas (deben quedar `Assets/Scripts` y `Assets/Editor`).
4. Vuelve a Unity y espera a que compile. La consola (*Window → General → Console*) no debe mostrar errores en rojo.
5. Menú superior: **No Mires Dos Veces → 1. Crear escena y configurar proyecto**.
6. Pulsa **Play**.

> Alternativa: aunque no hagas el paso 5, al pulsar Play en cualquier escena el juego se crea solo (`GameBootstrap`). El paso 5 es necesario para compilar.

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Moverse |
| Ratón | Mirar |
| E | Reportar anomalía / abrir la puerta |
| Esc | Pausa |
| R | Reiniciar (en pausa, victoria o derrota) |
| F1 | Solo Editor y Development Build: activa todas las anomalías al instante (para probar) |

## Cómo se juega

1. Recorre el pasillo y memoriza cómo es.
2. Pasado un tiempo, **tres cosas cambian**:
   - **Cuadro:** se mueve y se ladea (solo cuando no lo estás viendo). A los 35 s.
   - **Luz:** parpadea y se apaga de repente. A los 90 s.
   - **Silla:** desaparece cuando dejas de mirarla (tiene que haberla visto antes). A los 150 s.
3. Apunta a la anomalía y pulsa **E** para reportarla.
4. Hay objetos normales (otros cuadros, lámparas y sillas). Reportarlos, o reportar algo que aún no ha cambiado, cuenta como **error**. Con **3 errores pierdes**.
5. Con las 3 anomalías reportadas, la puerta del fondo se desbloquea. Ábrela con **E** para ganar.

Una anomalía solo cuenta **una vez**: volver a pulsar E sobre una ya reportada muestra "Ya has reportado esto." y no suma ni acierto ni error. Además hay 0,4 s de enfriamiento entre pulsaciones.

## Estructura

```
Assets/
  Editor/NoMiresDosVecesTools.cs   Menú: crear escena + compilar para Windows
  Scripts/
    GameManager.cs      Menú, pausa, victoria, derrota, reinicio, HUD (IMGUI)
    GameSettings.cs     Tiempos y límites ajustables
    GameBootstrap.cs    Crea el juego si la escena no lo tiene
    LevelBuilder.cs     Construye el pasillo con cubos y coloca las anomalías
    PlayerController.cs Movimiento WASD + ratón
    AnomalyReporter.cs  Tecla E, errores, apertura de la puerta
    Anomaly.cs          Base: una anomalía solo se reporta una vez
    PaintingAnomaly.cs  Anomalía 1
    LightAnomaly.cs     Anomalía 2
    ChairAnomaly.cs     Anomalía 3
    DecoyReportable.cs  Marca objetos normales (reportarlos = error)
    ExitDoor.cs         Puerta con bisagra
    GameInput.cs        Entrada compatible con Input System nuevo y antiguo
    ViewUtil.cs         "¿Lo está viendo el jugador?" (frustum + oclusión)
    MaterialUtil.cs     Materiales de color válidos en URP y Built-in
    ProceduralAudio.cs  Sonidos generados por código
```

## Ajustar la dificultad

Edita `Assets/Scripts/GameSettings.cs`: tiempos de cada anomalía, errores máximos, alcance de E.

## Compilar para Windows

Menú **No Mires Dos Veces → 2. Compilar para Windows (64-bit)**. El resultado queda en `Builds/Windows/NoMiresDosVeces.exe` (comprime toda la carpeta `Windows` para entregarla). Si compilas desde Mac/Linux, instala antes el módulo *Windows Build Support (Mono)* desde Unity Hub.

Alternativa manual: *File → Build Profiles → Windows* → añade `Assets/Scenes/Main.unity` → *Build*.

## Lista de pruebas (hazla antes de entregar)

1. **Menú:** al dar Play aparece el menú con JUGAR y SALIR; el cursor es visible.
2. **Movimiento:** JUGAR → el cursor se bloquea; WASD y ratón funcionan; no atraviesas paredes ni escritorios.
3. **Pausa:** Esc → aparece PAUSA, el tiempo se detiene; CONTINUAR vuelve; REINICIAR y MENÚ PRINCIPAL funcionan.
4. **Cuadro:** pulsa F1 mirando al cuadro de la pared izquierda (z≈6) → no cambia mientras lo ves; al girar y volver, está movido y ladeado. E sobre él → "Anomalía reportada" y vuelve a su sitio.
5. **Luz:** F1 → la lámpara central parpadea y se apaga. E apuntando a ella (mira al techo) → se enciende y suma acierto.
6. **Silla:** mira la silla de la izquierda (z≈12) durante ~2 s, pulsa F1 y aparta la vista → desaparece cuando ya no la ves (nunca delante de tus ojos). E sobre el hueco → vuelve y suma.
7. **No contar dos veces:** reporta una anomalía y pulsa E otra vez sobre ella → "Ya has reportado esto.", los contadores no cambian.
8. **Errores:** pulsa E sobre un cuadro, lámpara o silla normal → "Error 1/3" (parpadeo rojo y zumbido). Con 3 errores aparece la pantalla de derrota; REINTENTAR empieza una partida limpia.
9. **Puerta bloqueada:** con menos de 3 reportes, E en la puerta → mensaje de "no se abre", sin error.
10. **Victoria:** reporta las 3 → mensaje de desbloqueo → E en la puerta → se abre y aparece HAS ESCAPADO.
11. **Reinicio completo:** tras victoria/derrota, JUGAR DE NUEVO deja contadores a 0 y las anomalías de nuevo sin activar.
12. **Build:** compila para Windows y repite los puntos 1–10 en el `.exe`.
13. **Sin pantalla rosa:** si algo se ve rosa, el proyecto usa un pipeline raro; avísame con el nombre de la plantilla.

## Licencias

Todo el contenido es propio: código C#, geometría con primitivas de Unity y audio sintetizado en tiempo de ejecución. No hay assets de terceros ni de pago.

## Notas técnicas

- La interfaz usa IMGUI para no depender de Canvas, TextMeshPro ni fuentes.
- El jugador está en la capa 2 (*Ignore Raycast*) para que los rayos de reporte y visibilidad no choquen con él.
- Una silla desaparecida mantiene un collider *trigger* para poder reportar el hueco vacío.
- Si el proyecto tiene "Active Input Handling" en *Input System* (por defecto en Unity 6), `GameInput` usa el sistema nuevo; con *Input Manager* o *Both* también funciona.
