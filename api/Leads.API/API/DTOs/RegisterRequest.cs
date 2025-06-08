namespace Leads.API.API.DTOs
{
    public class RegisterRequest
    {
        public string NomeUsuario { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Perfil { get; set; }
    }

}
