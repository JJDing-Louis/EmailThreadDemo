namespace EmailThreadDemo.Model;

public class MailSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public bool SmtpUseSsl { get; set; }
}