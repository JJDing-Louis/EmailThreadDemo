using System;
using System.Linq;
using EmailThreadDemo.Model;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using System;
using System.IO;
using System.Linq;
using EmailResponseService.Model;
using EmailThreadDemo.Model;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;

namespace EmailThreadDemo;

public class MailFun
{
    private readonly string _host;
    private readonly int _port;
    private readonly bool _useSsl;
    private readonly string _Smtphost;
    private readonly int _Smtpport;
    private readonly bool _SmtpuseSsl;
    private readonly string _username;
    private readonly string _password;
    

    public MailFun(AccountInfo accountInfo, MailSettings mailSettings)
    {
        //帳戶資料
        _username = accountInfo.Username;
        _password = accountInfo.Password;
        //設定
        _host = mailSettings.Host;
        _port = mailSettings.Port;
        _useSsl = mailSettings.UseSsl;
        //Smtp 設定
        _Smtphost = mailSettings.SmtpHost;
        _Smtpport = mailSettings.SmtpPort;
        _SmtpuseSsl = mailSettings.SmtpUseSsl;
    }

    public void GetDisplayThread()
    {
        using var client = new ImapClient();

        var options = _useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        client.Connect(_host, _port, options);
        client.Authenticate(_username, _password);

        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadOnly);

        if (inbox.Count == 0)
        {
            Console.WriteLine("Inbox is empty.");
            client.Disconnect(true);
            return;
        }

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

        var threads = items.Thread(ThreadingAlgorithm.References);

        foreach (var thread in threads)
        {
            PrintThread(thread, 0);
            Console.WriteLine();
        }

        client.Disconnect(true);
    }

    private void PrintThread(MessageThread thread, int depth)
    {
        var indent = new string(' ', depth * 2);

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

    public void SendMail(EmailMessageModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        if (model.From == null || model.From.Count == 0)
            throw new ArgumentException("From 不可為空", nameof(model));

        if ((model.To == null || model.To.Count == 0) &&
            (model.Cc == null || model.Cc.Count == 0) &&
            (model.Bcc == null || model.Bcc.Count == 0))
            throw new ArgumentException("至少要有一位收件人（To/Cc/Bcc）", nameof(model));

        var message = new MimeMessage();

        // From
        foreach (var addr in model.From.Where(IsValidEmailAddress))
        {
            message.From.Add(ToMailboxAddress(addr));
        }

        if (!message.From.Any())
            throw new ArgumentException("From 沒有有效的 Email Address", nameof(model));

        // Sender
        if (model.Sender != null && IsValidEmailAddress(model.Sender))
        {
            message.Sender = ToMailboxAddress(model.Sender);
        }

        // Reply-To
        if (model.ReplyTo != null)
        {
            foreach (var addr in model.ReplyTo.Where(IsValidEmailAddress))
            {
                message.ReplyTo.Add(ToMailboxAddress(addr));
            }
        }

        // To
        foreach (var addr in model.To.Where(IsValidEmailAddress))
        {
            message.To.Add(ToMailboxAddress(addr));
        }

        // Cc
        foreach (var addr in model.Cc.Where(IsValidEmailAddress))
        {
            message.Cc.Add(ToMailboxAddress(addr));
        }

        // Bcc
        foreach (var addr in model.Bcc.Where(IsValidEmailAddress))
        {
            message.Bcc.Add(ToMailboxAddress(addr));
        }

        message.Subject = model.Subject ?? string.Empty;
        message.Date = model.Date ?? DateTimeOffset.Now;

        // Threading headers
        if (!string.IsNullOrWhiteSpace(model.InReplyTo))
            message.InReplyTo = model.InReplyTo;

        if (model.References != null)
        {
            foreach (var reference in model.References.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                message.References.Add(reference);
            }
        }

        // Message-Id
        if (!string.IsNullOrWhiteSpace(model.MessageId))
            message.MessageId = model.MessageId;

        // Priority
        ApplyPriority(message, model.Priority);

        // Read receipt
        if (model.RequestReadReceipt)
        {
            var receiptTo = model.ReplyTo.FirstOrDefault(IsValidEmailAddress)
                            ?? model.From.FirstOrDefault(IsValidEmailAddress);

            if (receiptTo != null)
            {
                message.Headers.Add("Disposition-Notification-To", receiptTo.Address);
            }
        }

        // Custom headers
        if (model.Headers != null)
        {
            foreach (var header in model.Headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                    continue;

                // 避免覆蓋 MimeMessage 已經處理的標準欄位
                if (IsReservedHeader(header.Key))
                    continue;

                foreach (var value in header.Value.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    message.Headers.Add(header.Key, value);
                }
            }
        }

        var builder = new BodyBuilder
        {
            TextBody = string.IsNullOrWhiteSpace(model.TextBody) ? null : model.TextBody,
            HtmlBody = string.IsNullOrWhiteSpace(model.HtmlBody) ? null : model.HtmlBody
        };

        // 附件 / inline 資源
        if (model.Attachments != null)
        {
            foreach (var attachment in model.Attachments)
            {
                AddAttachment(builder, attachment);
            }
        }

        // 如果 TextBody / HtmlBody / Attachments 都沒有，至少給空本文
        if (builder.TextBody == null && builder.HtmlBody == null && !model.HasAttachments)
        {
            builder.TextBody = string.Empty;
        }

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();

        var options = _SmtpuseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        smtp.Connect(_Smtphost, _Smtpport, options);
        smtp.Authenticate(_username, _password);
        smtp.Send(message);
        smtp.Disconnect(true);

        Console.WriteLine("Mail sent successfully.");
    }

    private static MailboxAddress ToMailboxAddress(EmailAddressModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Address))
            throw new ArgumentException("Email address 不可為空");

        return new MailboxAddress(model.Name ?? string.Empty, model.Address);
    }

    private static bool IsValidEmailAddress(EmailAddressModel? model)
    {
        return model != null && !string.IsNullOrWhiteSpace(model.Address);
    }

    private static void ApplyPriority(MimeMessage message, string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return;

        switch (priority.Trim().ToLowerInvariant())
        {
            case "high":
                message.Priority = MessagePriority.Urgent;
                message.Headers.Replace("X-Priority", "1");
                message.Headers.Replace("Importance", "high");
                break;

            case "low":
                message.Priority = MessagePriority.NonUrgent;
                message.Headers.Replace("X-Priority", "5");
                message.Headers.Replace("Importance", "low");
                break;

            default:
                message.Priority = MessagePriority.Normal;
                message.Headers.Replace("X-Priority", "3");
                message.Headers.Replace("Importance", "normal");
                break;
        }
    }

    private static bool IsReservedHeader(string headerName)
    {
        var reserved = new[]
        {
            "From", "To", "Cc", "Bcc", "Reply-To",
            "Subject", "Date", "Message-Id",
            "In-Reply-To", "References",
            "Mime-Version", "Content-Type"
        };

        return reserved.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddAttachment(BodyBuilder builder, EmailAttachmentModel attachment)
    {
        if (attachment == null)
            return;

        MimeEntity? entity = null;

        // 1. 優先使用 FilePath
        if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            if (!File.Exists(attachment.FilePath))
                throw new FileNotFoundException($"找不到附件檔案: {attachment.FilePath}");

            if (attachment.IsInline)
            {
                var inline = builder.LinkedResources.Add(attachment.FilePath);
                ApplyAttachmentMetadata(inline, attachment);
                entity = inline;
            }
            else
            {
                var file = builder.Attachments.Add(attachment.FilePath);
                ApplyAttachmentMetadata(file, attachment);
                entity = file;
            }
        }
        // 2. 其次使用 ContentBytes
        else if (attachment.ContentBytes != null && attachment.ContentBytes.Length > 0)
        {
            var fileName = attachment.FileName ?? "attachment.bin";

            ContentType? contentType = null;
            if (!string.IsNullOrWhiteSpace(attachment.ContentType) &&
                MimeKit.ContentType.TryParse(attachment.ContentType, out var parsedType))
            {
                contentType = parsedType;
            }

            if (attachment.IsInline)
            {
                var inline = contentType != null
                    ? builder.LinkedResources.Add(fileName, attachment.ContentBytes, contentType)
                    : builder.LinkedResources.Add(fileName, attachment.ContentBytes);

                ApplyAttachmentMetadata(inline, attachment);
                entity = inline;
            }
            else
            {
                var file = contentType != null
                    ? builder.Attachments.Add(fileName, attachment.ContentBytes, contentType)
                    : builder.Attachments.Add(fileName, attachment.ContentBytes);

                ApplyAttachmentMetadata(file, attachment);
                entity = file;
            }
        }

        if (entity == null)
            throw new ArgumentException(
                $"附件 {attachment.FileName ?? "(未命名)"} 缺少 FilePath 或 ContentBytes，無法附加。");
    }

    private static void ApplyAttachmentMetadata(MimeEntity entity, EmailAttachmentModel attachment)
    {
        if (entity is MimePart part)
        {
            if (!string.IsNullOrWhiteSpace(attachment.FileName))
                part.FileName = attachment.FileName;

            if (!string.IsNullOrWhiteSpace(attachment.ContentId))
                part.ContentId = attachment.ContentId;
            else if (attachment.IsInline && string.IsNullOrWhiteSpace(part.ContentId))
                part.ContentId = MimeKit.Utils.MimeUtils.GenerateMessageId();

            if (attachment.IsInline)
                part.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            else
                part.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
        }
    }
}
