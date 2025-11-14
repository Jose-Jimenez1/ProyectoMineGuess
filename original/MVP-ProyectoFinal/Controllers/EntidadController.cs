using Microsoft.AspNetCore.Mvc;
using MVP_ProyectoFinal.Models;
using System.Text.Json;

namespace MVP_ProyectoFinal.Controllers
{
    public class EntidadController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("EntidadGanado") == "true")
            {
                var nombreSecreto = HttpContext.Session.GetString("EntidadSecreta");
                TempData["MensajeVictoria"] = $"¡Adivinaste la entidad! Era: {nombreSecreto}";
            }
            else
            {
                var entidadSecretaActual = HttpContext.Session.GetString("EntidadSecreta");
                if (string.IsNullOrEmpty(entidadSecretaActual))
                {
                    var entidadSecretaNueva = RepositorioEntidades.ObtenerEntidadAleatoria();
                    if (entidadSecretaNueva != null)
                    {
                        HttpContext.Session.SetString("EntidadSecreta", entidadSecretaNueva.Nombre);
                        HttpContext.Session.SetString("IntentosEntidad", "[]");
                    }
                }
            }
            var intentosJson = HttpContext.Session.GetString("IntentosEntidad") ?? "[]";
            var intentos = JsonSerializer.Deserialize<List<ResultadoIntentoEntidadVM>>(intentosJson);
            return View(intentos);
        }

        [HttpPost]
        public IActionResult Adivinar(string nombreEntidad)
        {
            var nombreSecreto = HttpContext.Session.GetString("EntidadSecreta");
            if (string.IsNullOrEmpty(nombreSecreto)) return RedirectToAction("Reiniciar");

            var intentosJson = HttpContext.Session.GetString("IntentosEntidad") ?? "[]";
            var todosLosIntentos = JsonSerializer.Deserialize<List<ResultadoIntentoEntidadVM>>(intentosJson) ?? new List<ResultadoIntentoEntidadVM>();

            if (todosLosIntentos.Any(intento => intento.NombreEntidad.Equals(nombreEntidad, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"Ya intentaste con '{nombreEntidad}'. Prueba con otra.";
                return RedirectToAction("Index");
            }

            var entidadIntentada = RepositorioEntidades.ObtenerPorNombre(nombreEntidad);
            if (entidadIntentada == null)
            {
                TempData["Error"] = "Esa entidad no existe. Intenta con otra.";
                return RedirectToAction("Index");
            }

            var entidadSecreta = RepositorioEntidades.ObtenerPorNombre(nombreSecreto);
            if (entidadSecreta == null)
            {
                TempData["Error"] = "Error crítico. Empezando de nuevo.";
                return RedirectToAction("Reiniciar");
            }

            
            var longitudNombreIntentado = entidadIntentada.Nombre.Replace(" ", "").Length;
            var longitudNombreSecreto = entidadSecreta.Nombre.Replace(" ", "").Length;
            var coincideInicial = entidadIntentada.Nombre.Length > 0
                && entidadSecreta.Nombre.Length > 0
                && char.ToUpperInvariant(entidadIntentada.Nombre[0]) == char.ToUpperInvariant(entidadSecreta.Nombre[0]);

            var resultado = new ResultadoIntentoEntidadVM
            {
                NombreEntidad = entidadIntentada.Nombre,
                Tipo = entidadIntentada.Tipo,
                ColorTipo = entidadIntentada.Tipo == entidadSecreta.Tipo ? "verde" : "rojo",
                Vida = entidadIntentada.Vida,
                ColorVida = entidadIntentada.Vida == entidadSecreta.Vida ? "verde" : "rojo",
                HintVida = entidadIntentada.Vida < entidadSecreta.Vida ? "▲" : (entidadIntentada.Vida > entidadSecreta.Vida ? "▼" : ""),
                Ataque = entidadIntentada.Ataque,
                ColorAtaque = entidadIntentada.Ataque == entidadSecreta.Ataque ? "verde" : "rojo",
                HintAtaque = entidadIntentada.Ataque < entidadSecreta.Ataque ? "▲" : (entidadIntentada.Ataque > entidadSecreta.Ataque ? "▼" : ""),
                Dimension = entidadIntentada.Dimension,
                ColorDimension = entidadIntentada.Dimension == entidadSecreta.Dimension ? "verde" : "rojo",
                YearLanzamiento = entidadIntentada.YearLanzamiento,
                ColorAnio = entidadIntentada.YearLanzamiento == entidadSecreta.YearLanzamiento ? "verde" : "rojo",
                HintAnio = entidadIntentada.YearLanzamiento < entidadSecreta.YearLanzamiento ? "▲" : (entidadIntentada.YearLanzamiento > entidadSecreta.YearLanzamiento ? "▼" : ""),
                LongitudNombre = longitudNombreIntentado,
                CoincideInicial = coincideInicial ? "Sí" : "No",
                ColorLongitudNombre = longitudNombreIntentado == longitudNombreSecreto ? "verde" : "rojo",
                ColorCoincideInicial = coincideInicial ? "verde" : "rojo",
                HintLongitudNombre = longitudNombreIntentado < longitudNombreSecreto ? "▲" : (longitudNombreIntentado > longitudNombreSecreto ? "▼" : "")
            };


            todosLosIntentos.Add(resultado);
            HttpContext.Session.SetString("IntentosEntidad", JsonSerializer.Serialize(todosLosIntentos));

            if (entidadIntentada.Nombre.Equals(entidadSecreta.Nombre, StringComparison.OrdinalIgnoreCase))
            {
                TempData["MensajeVictoria"] = $"¡Adivinaste la entidad! Era: {entidadSecreta.Nombre}";
                HttpContext.Session.SetString("EntidadGanado", "true");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Reiniciar()
        {
            HttpContext.Session.Remove("EntidadSecreta");
            HttpContext.Session.Remove("IntentosEntidad");
            HttpContext.Session.Remove("EntidadGanado");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult SugerirEntidades(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<string>());
            var todasLasEntidades = RepositorioEntidades.ObtenerTodos();
            var sugerencias = todasLasEntidades
                .Where(e => e.Nombre.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Nombre)
                .Take(5)
                .ToList();
            return Json(sugerencias);
        }
    }
}