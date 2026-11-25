namespace APIWeb.DTOs
{
    public class CaptchaRequest
    {
        public string CaptchaId { get; set; } = string.Empty;

        public string Respuesta { get; set; } = string.Empty;
    }
}