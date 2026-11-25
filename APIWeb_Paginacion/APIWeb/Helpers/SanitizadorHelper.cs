using System.Text.RegularExpressions;

namespace APIWeb.Helpers
{
    public static class SanitizadorHelper
    {
        // Patrones peligrosos de XSS e inyección HTML/JS
        private static readonly Regex PatronesPeligrosos = new(
            @"(<|>|javascript:|vbscript:|data:text/html|on\w+\s*=|<\s*script|<\s*iframe|<\s*img|<\s*svg)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        /// <summary>
        /// Verifica si el texto contiene etiquetas HTML o código JavaScript malicioso.
        /// </summary>
        public static bool ContieneCodigoMalicioso(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return false;
            return PatronesPeligrosos.IsMatch(texto);
        }

        /// <summary>
        /// Rechaza la entrada lanzando una excepción si detecta HTML/JS, o devuelve el texto limpio (sin espacios extra).
        /// Ideal para nombres, descripciones, direcciones y categorías.
        /// </summary>
        public static string ValidarYLimpiarTexto(string? texto, string nombreCampo = "El campo")
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            if (ContieneCodigoMalicioso(texto))
                throw new ArgumentException($"{nombreCampo} contiene caracteres o etiquetas HTML/JavaScript no permitidos.");

            return texto.Trim();
        }

        /// <summary>
        /// Valida que un correo electrónico tenga formato estándar y no contenga etiquetas HTML/JS.
        /// </summary>
        public static string ValidarCorreo(string? correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                throw new ArgumentException("El correo electrónico es obligatorio.");

            correo = correo.Trim();

            if (ContieneCodigoMalicioso(correo))
                throw new ArgumentException("El correo contiene caracteres no permitidos.");

            var regexCorreo = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!regexCorreo.IsMatch(correo))
                throw new ArgumentException("El formato del correo electrónico no es válido.");

            return correo.ToLowerInvariant();
        }

        /// <summary>
        /// Valida un número telefónico (solo dígitos, +, espacios o guiones, entre 7 y 15 dígitos).
        /// </summary>
        public static string ValidarTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                throw new ArgumentException("El teléfono es obligatorio.");

            telefono = telefono.Trim();

            if (ContieneCodigoMalicioso(telefono))
                throw new ArgumentException("El teléfono contiene caracteres no permitidos.");

            var regexTelefono = new Regex(@"^\+?[0-9\s\-]{7,15}$");
            if (!regexTelefono.IsMatch(telefono))
                throw new ArgumentException("El formato del teléfono no es válido.");

            return telefono;
        }

        /// <summary>
        /// Valida que una URL sea http o https limpia (sin javascript:, HTML ni parámetros raros).
        /// </summary>
        public static string ValidarUrlLimpia(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            if (ContieneCodigoMalicioso(url))
                throw new ArgumentException("La URL contiene código o protocolos peligrosos.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("La URL debe comenzar con http:// o https://.");
            }

            // Bloquea parámetros de consulta sospechosos o caracteres especiales fuera de estándar
            if (!string.IsNullOrEmpty(uriResult.Query) || url.Contains('<') || url.Contains('>'))
                throw new ArgumentException("La URL no debe contener parámetros de consulta ni caracteres especiales.");

            return uriResult.ToString();
        }
    }
}