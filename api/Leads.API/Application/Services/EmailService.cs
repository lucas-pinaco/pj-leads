using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarArquivoPorEmailAsync(string destinatario, byte[] arquivo, string nomeArquivo)
    {
        try
        {
            var smtpServer = _configuration["SmtpSettings:Server"];
            var smtpPort = int.Parse(_configuration["SmtpSettings:Port"]!);
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Sistema de Leads", fromEmail));
            message.To.Add(new MailboxAddress("Cliente", destinatario));
            message.Subject = "Leads Exportados";

            var builder = new BodyBuilder { TextBody = "Segue o arquivo de leads exportados em anexo." };
            builder.Attachments.Add(nomeArquivo, arquivo);

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Grava log ou trata o erro conforme necessidade
            Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
        }
    }
}