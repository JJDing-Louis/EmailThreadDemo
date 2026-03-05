using System;
using System.Linq;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;

namespace EmailThreadDemo;

public class MailFun
{
    private string _Host;
    private int _Port;
    private bool _UseSsl;
    private string _Username;
    private string _Password;
    
    public MailFun(AccountInfo accountInfo, MailSettings mailSettings)
    {
        _Host = mailSettings.Host;
        _Port = mailSettings.Port;
        _UseSsl = mailSettings.UseSsl;
        _Username = accountInfo.Username;
        _Password = accountInfo.Password;
    }

    public void GetDisplayThread()
    {
        using var client = new ImapClient();

        var options = _UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        // ✅ 用你傳進來的設定
        client.Connect(_Host, _Port, options);
        client.Authenticate(_Username, _Password);

        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadOnly);

        if (inbox.Count == 0)
        {
            Console.WriteLine("Inbox is empty.");
            client.Disconnect(true);
            return;
        }

        // 建議只抓最後 N 封先驗證 Thread 是否正常
        int takeLastN = 200;
        int end = inbox.Count - 1;
        int start = Math.Max(0, inbox.Count - takeLastN);

        var items = inbox.Fetch(
            start,
            end,
            MessageSummaryItems.Envelope |
            MessageSummaryItems.References |
            MessageSummaryItems.UniqueId |
            MessageSummaryItems.InternalDate
        );

        // ✅ MailKit 4.x 正確 threading 呼叫
        var threads = items.Thread(ThreadingAlgorithm.References);

        foreach (var thread in threads)
        {
            PrintThread(thread, 0);
            Console.WriteLine();
        }

        client.Disconnect(true);
    }
    
    // 遞迴顯示執行緒結構
    private void PrintThread(MessageThread thread, int depth)
    {
        var indent = new string(' ', depth * 2);

        // thread.Message 可能為 null（dummy node）
        if (thread.Message != null)
        {
            var subj = thread.Message.Envelope?.Subject ?? "(no subject)";
            var from = thread.Message.Envelope?.From?.Mailboxes?.FirstOrDefault()?.ToString() ?? "(unknown)";
            var dt = thread.Message.InternalDate?.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "(no date)";
            Console.WriteLine($"{indent}- {subj} | {from} | {dt}");
        }
        else
        {
            Console.WriteLine($"{indent}- (dummy)");
        }

        foreach (var child in thread.Children)
            PrintThread(child, depth + 1);
    }


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