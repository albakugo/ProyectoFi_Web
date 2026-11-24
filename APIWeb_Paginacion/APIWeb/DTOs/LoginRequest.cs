using System.ComponentModel.DataAnnotations;


//aqui se recibe el correo, nombre, contraseña de usuario
namespace APIWeb.DTOs
    {
        public class LoginRequest
        {
            [Required]
            public string Usuario { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;

            [Required]
            //identifica pregunta matematica y el resultado
            public string CaptchaId { get; set; } = string.Empty;

            [Required]
            public string RespuestaCaptcha { get; set; } = string.Empty;
        }
    }