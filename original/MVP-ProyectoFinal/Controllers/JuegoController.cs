using Microsoft.AspNetCore.Mvc;
using MVP_ProyectoFinal.Models;
using System.Text.Json;
using System;

namespace MVP_ProyectoFinal.Controllers
{
    public class JuegoController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("JuegoGanado") == "true")
            {
                var nombreSecreto = HttpContext.Session.GetString("BloqueSecreto");
                TempData["MensajeVictoria"] = $"¡Felicidades, adivinaste el bloque! Era: {nombreSecreto}";
            }
            else
            {
                var bloqueSecretoActual = HttpContext.Session.GetString("BloqueSecreto");
                if (string.IsNullOrEmpty(bloqueSecretoActual))
                {
                    var bloqueSecretoNuevo = RepositorioBloques.ObtenerBloqueAleatorio();
                    if (bloqueSecretoNuevo != null)
                    {
                        HttpContext.Session.SetString("BloqueSecreto", bloqueSecretoNuevo.Nombre);
                        HttpContext.Session.SetString("Intentos", "[]");
                    }
                }
            }
            var intentosJson = HttpContext.Session.GetString("Intentos") ?? "[]";
            var intentos = JsonSerializer.Deserialize<List<ResultadoIntentoVM>>(intentosJson);
            return View(intentos);
        }

        [HttpPost]
        public IActionResult Adivinar(string nombreBloque)
        {
            var nombreSecreto = HttpContext.Session.GetString("BloqueSecreto");
            if (string.IsNullOrEmpty(nombreSecreto)) return RedirectToAction("Reiniciar");

            var intentosJson = HttpContext.Session.GetString("Intentos") ?? "[]";
            var todosLosIntentos = JsonSerializer.Deserialize<List<ResultadoIntentoVM>>(intentosJson) ?? new List<ResultadoIntentoVM>();

            if (todosLosIntentos.Any(intento => intento.NombreBloque.Equals(nombreBloque, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Error"] = $"Ya intentaste con '{nombreBloque}'. Prueba con otro.";
                return RedirectToAction("Index");
            }

            var bloqueIntentado = RepositorioBloques.ObtenerPorNombre(nombreBloque);
            if (bloqueIntentado == null)
            {
                TempData["Error"] = "Ese bloque no existe. Intenta con otro.";
                return RedirectToAction("Index");
            }

            var bloqueSecreto = RepositorioBloques.ObtenerPorNombre(nombreSecreto);
            if (bloqueSecreto == null)
            {
                TempData["Error"] = "Error crítico. Empezando de nuevo.";
                return RedirectToAction("Reiniciar");
            }

            var valorVersionIntentada = VersionComparer.ObtenerValor(bloqueIntentado.Version);
            var valorVersionSecreta = VersionComparer.ObtenerValor(bloqueSecreto.Version);

            var longitudNombreIntentado = bloqueIntentado.Nombre.Replace(" ", "").Length;
            var longitudNombreSecreto = bloqueSecreto.Nombre.Replace(" ", "").Length;
            var colorLongitud = longitudNombreIntentado == longitudNombreSecreto ? "verde" : "rojo";
            var hintLongitud = longitudNombreIntentado < longitudNombreSecreto ? "▲" :
                               (longitudNombreIntentado > longitudNombreSecreto ? "▼" : "");

            var inicialIntentado = string.IsNullOrWhiteSpace(bloqueIntentado.Nombre)
                ? '?'
                : char.ToUpper(bloqueIntentado.Nombre.Trim()[0]);
            var inicialSecreto = string.IsNullOrWhiteSpace(bloqueSecreto.Nombre)
                ? '?'
                : char.ToUpper(bloqueSecreto.Nombre.Trim()[0]);
            var colorInicial = inicialIntentado == inicialSecreto ? "verde" : "rojo";

            var palabrasIntento = string.IsNullOrWhiteSpace(bloqueIntentado.Nombre)
                ? 0
                : bloqueIntentado.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var palabrasSecretas = string.IsNullOrWhiteSpace(bloqueSecreto.Nombre)
                ? 0
                : bloqueSecreto.Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var colorPalabras = palabrasIntento == palabrasSecretas ? "verde" : "rojo";

            var resultado = new ResultadoIntentoVM
            {
                NombreBloque = bloqueIntentado.Nombre,
                Version = bloqueIntentado.Version,
                ColorVersion = valorVersionIntentada == valorVersionSecreta ? "verde" : "rojo",
                HintVersion = valorVersionIntentada < valorVersionSecreta ? "▲" : (valorVersionIntentada > valorVersionSecreta ? "▼" : ""),
                Bioma = bloqueIntentado.Bioma,
                ColorBioma = bloqueIntentado.Bioma == bloqueSecreto.Bioma ? "verde" : "rojo",
                EsDestructible = bloqueIntentado.EsDestructible ? "Sí" : "No",
                ColorDestructible = bloqueIntentado.EsDestructible == bloqueSecreto.EsDestructible ? "verde" : "rojo",
                EsDeExterior = bloqueIntentado.EsDeExterior ? "Sí" : "No",
                ColorExterior = bloqueIntentado.EsDeExterior == bloqueSecreto.EsDeExterior ? "verde" : "rojo",
                YearLanzamiento = bloqueIntentado.YearLanzamiento,
                ColorAnio = bloqueIntentado.YearLanzamiento == bloqueSecreto.YearLanzamiento ? "verde" : "rojo",
                HintAnio = bloqueIntentado.YearLanzamiento < bloqueSecreto.YearLanzamiento ? "▲" : (bloqueIntentado.YearLanzamiento > bloqueSecreto.YearLanzamiento ? "▼" : ""),
                LongitudNombre = longitudNombreIntentado,
                ColorLongitud = colorLongitud,
                HintLongitud = hintLongitud,
                Inicial = inicialIntentado.ToString(),
                ColorInicial = colorInicial,
                NumeroPalabras = palabrasIntento,
                ColorNumeroPalabras = colorPalabras
            };

            todosLosIntentos.Add(resultado);
            HttpContext.Session.SetString("Intentos", JsonSerializer.Serialize(todosLosIntentos));

            if (bloqueIntentado.Nombre.Equals(bloqueSecreto.Nombre, StringComparison.OrdinalIgnoreCase))
            {
                TempData["MensajeVictoria"] = $"¡Felicidades, adivinaste el bloque! Era: {bloqueSecreto.Nombre}";
                HttpContext.Session.SetString("JuegoGanado", "true");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Reiniciar()
        {
            HttpContext.Session.Remove("BloqueSecreto");
            HttpContext.Session.Remove("Intentos");
            HttpContext.Session.Remove("JuegoGanado");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult SugerirBloques(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<string>());
            var todosLosBloques = RepositorioBloques.ObtenerTodos();
            var sugerencias = todosLosBloques
                .Where(b => b.Nombre.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Nombre)
                .Take(5)
                .ToList();
            return Json(sugerencias);
        }
    }
}