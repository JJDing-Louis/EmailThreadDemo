using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmailResponseService.Model;
using MimeKit;


namespace EmailResponseService.Tool
{
    public static class MimeMessageMapper
    {
        public static EmailMessageModel ToModel(MimeMessage message, bool includeRawMessage = false)
        {
            var model = new EmailMessageModel
            {
                Subject = message.Subject,
                NormalizedSubject = NormalizeSubject(message.Subject),
                MessageId = message.MessageId,
                InReplyTo = message.InReplyTo,
                References = message.References?.ToList() ?? new List<string>(),
                Date = message.Date != DateTimeOffset.MinValue ? message.Date : null,
                DateRaw = message.Headers["Date"],
                From = ConvertAddressList(message.From),
                Sender = ConvertMailbox(message.Sender),
                ReplyTo = ConvertAddressList(message.ReplyTo),
                To = ConvertAddressList(message.To),
                Cc = ConvertAddressList(message.Cc),
                Bcc = ConvertAddressList(message.Bcc),
                ReturnPath = message.Headers["Return-Path"],
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
                ContentType = message.Body.ContentType.MimeType,
                MimeVersion = message.MimeVersion?.ToString(),
                ReceivedHeaders = message.Headers
                    .Where(h => h.Field.Equals("Received", StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value)
                    .ToList()
            };

            foreach (var header in message.Headers)
            {
                if (!model.Headers.TryGetValue(header.Field, out var values))
                {
                    values = new List<string>();
                    model.Headers[header.Field] = values;
                }

                values.Add(header.Value);
            }

            foreach (var attachment in message.Attachments)
            {
                model.Attachments.Add(ConvertAttachment(attachment));
            }

            if (includeRawMessage)
            {
                using var ms = new MemoryStream();
                message.WriteTo(ms);
                model.RawMessage = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }

            return model;
        }

        private static List<EmailAddressModel> ConvertAddressList(InternetAddressList addresses)
        {
            var result = new List<EmailAddressModel>();

            foreach (var address in addresses)
            {
                switch (address)
                {
                    case MailboxAddress mailbox:
                        result.Add(new EmailAddressModel
                        {
                            Name = mailbox.Name,
                            Address = mailbox.Address,
                            Raw = mailbox.ToString()
                        });
                        break;

                    case GroupAddress group:
                        foreach (var member in group.Members.Mailboxes)
                        {
                            result.Add(new EmailAddressModel
                            {
                                Name = member.Name,
                                Address = member.Address,
                                Raw = member.ToString()
                            });
                        }
                        break;

                    default:
                        result.Add(new EmailAddressModel
                        {
                            Raw = address.ToString()
                        });
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
                Address = mailbox.Address,
                Raw = mailbox.ToString()
            };
        }

        private static EmailAttachmentModel ConvertAttachment(MimeEntity entity)
        {
            var model = new EmailAttachmentModel
            {
                ContentType = entity.ContentType?.MimeType,
                ContentDisposition = entity.ContentDisposition?.Disposition,
                ContentId = entity.ContentId
            };

            if (entity is MimePart part)
            {
                model.FileName = part.FileName;
                model.IsInline = string.Equals(
                    part.ContentDisposition?.Disposition,
                    "inline",
                    StringComparison.OrdinalIgnoreCase);

                using var ms = new MemoryStream();
                part.Content.DecodeTo(ms);
                model.Size = ms.Length;
            }
            else if (entity is MessagePart messagePart)
            {
                model.FileName = messagePart.ContentDisposition?.FileName
                                 ?? messagePart.ContentType?.Name;

                model.IsInline = false;
            }

            return model;
        }

        private static string? NormalizeSubject(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return subject;

            var value = subject.Trim();

            while (true)
            {
                var upper = value.ToUpperInvariant();

                if (upper.StartsWith("RE:"))
                {
                    value = value[3..].Trim();
                    continue;
                }

                if (upper.StartsWith("FW:"))
                {
                    value = value[3..].Trim();
                    continue;
                }

                if (upper.StartsWith("FWD:"))
                {
                    value = value[4..].Trim();
                    continue;
                }

                break;
            }

            return value;
        }
    }
}