namespace Leads.API.Domain.POCO
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int TokenLifetimeMinutes { get; set; }
    }

}
