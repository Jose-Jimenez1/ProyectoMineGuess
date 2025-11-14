
# MineGuess (Estética original + API integrada)

- **MVP-ProyectoFinal**: tu proyecto original, SIN cambios visuales (Views, CSS y layout intactos).
- **MineGuess.Api**: API mínima (.NET 8 + SQLite) lista para usarse en paralelo.

## Cómo correr desde Visual Studio
1. Abre `MineGuessOriginalIntegrated.sln` en Visual Studio 2022.
2. En **Set Startup Projects...** selecciona **Multiple startup projects**:
   - `MineGuess.Api` → Start (arranca en http://localhost:5280/swagger)
   - `MVP-ProyectoFinal` → Start
3. F5.

> Por defecto, el juego usa sus datos locales (idénticos a antes). Si quieres consumir la API para más datos, te configuro un flag `UseApi` después y lo conectamos sin tocar vistas.


### Cargar dataset completo (1.0.0 → latest)
Con la API corriendo, ejecuta:
```bash
curl -X POST "http://localhost:5280/api/v1/ingest/full?from=1.0.0&to=latest"
```
Esto reconstruye la base con **todos los bloques y entidades** detectados por versión usando *minecraft-data* y enriquece stats de entidades (salud/ataque/dimensiones) desde Minecraft Wiki.


### Ingestar TODO el dataset desde la wiki
Con ambos proyectos corriendo, ejecuta:
```bash
curl -X POST http://localhost:5280/api/v1/ingest/wiki
```
Esto rellenará la BD con bloques y entidades (1.0 → actual) usando:
- **Piston Meta** para el historial de versiones.
- **Minecraft Fandom Wiki** para infoboxes (propiedades).
