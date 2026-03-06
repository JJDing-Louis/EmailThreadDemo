using EmailThreadDemo.Model;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security; 
using EmailResponseService.Model;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Search;

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

    public List<EmailMessageModel> GetDisplayThread()
    {
        var result = new List<EmailMessageModel>();

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
            return result;
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
            TraverseThread(thread, inbox, result, 0);
        }

        client.Disconnect(true);

        return result;
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
    
    /// <summary>
    /// 透過Message-ID查找(需完全相符)
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns></returns>
    public MimeMessage GetMailByMessageId(string messageId)
    {
        using var client = new ImapClient();

        client.Connect(_host, _port, _useSsl);
        client.Authenticate(_username, _password);

        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadOnly);

        // 搜尋 Message-ID
        var results = inbox.Search(SearchQuery.HeaderContains("Message-Id", messageId));

        if (results.Count == 0)
            return null;

        // 取第一封
        var message = inbox.GetMessage(results[0]);

        client.Disconnect(true);

        return message;
    }
    
    /// <summary>
    /// 透過Message-ID與寄件人前綴去查找(需完全相符)
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns></returns>
    public MimeMessage? GetMailBySenderAndMessageIdPrefix(string senderEmail, string messageIdPrefix)
    {
        using var client = new ImapClient();

        client.Connect(_host, _port, _useSsl);
        client.Authenticate(_username, _password);

        var folder = client.Inbox;
        folder.Open(FolderAccess.ReadOnly);

        // 先用 IMAP 做初步篩選
        var query = SearchQuery.FromContains(senderEmail)
            .And(SearchQuery.HeaderContains("Message-Id", messageIdPrefix));

        var results = folder.Search(query);

        foreach (var uid in results)
        {
            var message = folder.GetMessage(uid);

            // 再用程式端做精準判斷
            var fromMatch = message.From.Mailboxes
                .Any(m => string.Equals(m.Address, senderEmail, StringComparison.OrdinalIgnoreCase));

            var messageIdMatch = !string.IsNullOrWhiteSpace(message.MessageId) &&
                                 message.MessageId.StartsWith(messageIdPrefix, StringComparison.OrdinalIgnoreCase);

            if (fromMatch && messageIdMatch)
            {
                client.Disconnect(true);
                return message;
            }
        }

        client.Disconnect(true);
        return null;
    }

    #region Private(Send)

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

    #endregion
    
    #region Public(DisplayThread)    
    
    private static List<EmailAddressModel> ConvertAddressList(InternetAddressList? addresses)
    {
        var result = new List<EmailAddressModel>();

        if (addresses == null || addresses.Count == 0)
            return result;

        foreach (var address in addresses)
        {
            switch (address)
            {
                case MailboxAddress mailbox:
                    result.Add(new EmailAddressModel
                    {
                        Name = mailbox.Name,
                        Address = mailbox.Address
                    });
                    break;

                case GroupAddress group:
                    if (group.Members != null)
                    {
                        foreach (var member in group.Members.Mailboxes)
                        {
                            result.Add(new EmailAddressModel
                            {
                                Name = member.Name,
                                Address = member.Address
                            });
                        }
                    }
                    break;
            }
        }

        return result;
    }

    private static EmailAddressModel? ConvertMailbox(MailboxAddress? mailbox)
    {
        if (mailbox == null)
            return null;

        return new EmailAddressModel
        {
            Name = mailbox.Name,
            Address = mailbox.Address
        };
    }
    
    private void TraverseThread(MessageThread thread, IMailFolder inbox, List<EmailMessageModel> list, int depth)
    {
        if (thread.Message != null)
        {
            var uid = thread.Message.UniqueId;
            var message = inbox.GetMessage(uid);

            var model = new EmailMessageModel
            {
                Subject = message.Subject,
                //MailTread需要看的東西
                MessageId = message.MessageId,
                InReplyTo = message.InReplyTo,
                References = message.References?.ToList() ?? new List<string>(),
                //
                Date = message.Date != DateTimeOffset.MinValue ? message.Date : null,
                DateRaw = message.Headers["Date"],

                From = ConvertAddressList(message.From),
                Sender = ConvertMailbox(message.Sender),
                ReplyTo = ConvertAddressList(message.ReplyTo),
                To = ConvertAddressList(message.To),
                Cc = ConvertAddressList(message.Cc),
                Bcc = ConvertAddressList(message.Bcc),

                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,

                ContentType = message.Body?.ContentType?.MimeType,
                MimeVersion = message.MimeVersion?.ToString(),

                ReceivedHeaders = message.Headers
                    .Where(h => h.Field.Equals("Received", StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)
                    .ToList()
            };

            list.Add(model);

            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}-{model.MessageId}|" +
                              $"{(!string.IsNullOrEmpty(model.InReplyTo)?model.InReplyTo:"OOO")}|" +
                              $"{(model.References.Count!=0?string.Join(",",model.References):"OOO")}|" +
                              $"{model.Subject}|" +
                              $"{model.From.FirstOrDefault()?.Address}|" +
                              $"{model.Date}");
        }

        foreach (var child in thread.Children)
        {
            TraverseThread(child, inbox, list, depth + 1);
        }
    }
    
    #endregion

    #region Private(Response)

    

    #endregion
    


}
