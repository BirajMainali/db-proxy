namespace DbProxy.Server.Models;

public class ServerConfiguration
{
    public int Port { get; set; }
    public string CertificatePassword { get; set; } = string.Empty;
    public EmailConfiguration Email { get; set; } = new();
}

public class EmailConfiguration
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string TextTemplate { get; set; } = string.Empty;
}