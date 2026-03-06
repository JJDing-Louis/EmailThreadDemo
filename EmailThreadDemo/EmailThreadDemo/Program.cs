using EmailThreadDemo.Model;

namespace EmailThreadDemo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // Google 帳戶 → 安全性 → 開啟「兩步驟驗證」
        //
        // 在「應用程式密碼 (App passwords)」產生一組 16 碼左右的密碼
        //
        //     程式碼中：
        //
        // Username：仍用 louisding70121@gmail.com
        //
        //     Password：改成「App Password」（不是你的登入密碼）
        var accountInfo = new AccountInfo
        {
            Username = "louisding70121@gmail.com",
            Password = "dznvgtiefzqhglev"

        };
        
        var mailSettings = new MailSettings
        {
            Host = "imap.gmail.com",
            Port = 993,
            UseSsl = true
        };
        
        var a = new MailFun(accountInfo, mailSettings);
        a.GetDisplayThread();
    }
}