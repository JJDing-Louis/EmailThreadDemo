using EmailResponseService.Model;
using EmailThreadDemo.Model;

namespace EmailThreadDemo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // Google 帳戶 → 安全性 → 開啟「兩步驟驗證」
        // 在「應用程式密碼 (App passwords)」產生一組 16 碼左右的密碼
        // 程式碼中：
        // Username：仍用 louisding70121@gmail.com
        // Password：改成「App Password」（不是你的登入密碼）
        var accountInfo = new AccountInfo
        {
            Username = "louisding70121@gmail.com",
            Password = "dznvgtiefzqhglev"
        };
        
        var mailSettings = new MailSettings
        {
            Host = "imap.gmail.com",
            Port = 993,
            UseSsl = true,
            
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 465,
            SmtpUseSsl = true
        };
        
        // var a = new MailFun(accountInfo, mailSettings);
        // a.GetDisplayThread();
        
        var mail = new EmailMessageModel
        {
            Subject = "測試信件2",
            TextBody = "這是一封測試信",
            HtmlBody = "<h1>這是一封測試信</h1><p>Hello MailKit</p>",
            Priority = "High",
            RequestReadReceipt = false,
            MessageId = $"<AuotTicket>-louisding70121@gmail.com-{Guid.NewGuid()}>"
        };

        mail.From.Add(new EmailAddressModel
        {
            Name = "Louis",
            Address = "louisding70121@gmail.com"
        });

        mail.To.Add(new EmailAddressModel
        {
            Name = "LouisA",
            Address = "louistpdrgtesta@gmail.com"
        });

        // mail.Attachments.Add(new EmailAttachmentModel
        // {
        //     FileName = "test.pdf",
        //     FilePath = @"D:\Temp\test.pdf",
        //     ContentType = "application/pdf",
        //     IsInline = false
        // });

        var mailFun = new MailFun(accountInfo, mailSettings);
        mailFun.SendMail(mail);
    }
}