namespace EmailThreadDemo.Model;

public class Email
{
    
}

public class AccountInfo
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class MailSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
}