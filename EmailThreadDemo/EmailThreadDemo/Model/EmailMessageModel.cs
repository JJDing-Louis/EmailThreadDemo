using System;
using System.Collections.Generic;

namespace EmailResponseService.Model;

public class EmailMessageModel
{
    /// <summary>
    /// RFC 5322 / MIME 原始內容（可選）
    /// </summary>
    public string? RawMessage { get; set; }

    /// <summary>
    /// Subject 原始值
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 正規化後主旨（可做 Thread Grouping）
    /// </summary>
    public string? NormalizedSubject { get; set; }

    /// <summary>
    /// Message-Id
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// In-Reply-To
    /// </summary>
    public string? InReplyTo { get; set; }

    /// <summary>
    /// References
    /// </summary>
    public List<string> References { get; set; } = new();

    /// <summary>
    /// 寄件時間
    /// </summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Header 中 Date 原始字串
    /// </summary>
    public string? DateRaw { get; set; }

    /// <summary>
    /// From
    /// </summary>
    public List<EmailAddressModel> From { get; set; } = new();

    /// <summary>
    /// Sender
    /// </summary>
    public EmailAddressModel? Sender { get; set; }

    /// <summary>
    /// Reply-To
    /// </summary>
    public List<EmailAddressModel> ReplyTo { get; set; } = new();

    /// <summary>
    /// To
    /// </summary>
    public List<EmailAddressModel> To { get; set; } = new();

    /// <summary>
    /// Cc
    /// </summary>
    public List<EmailAddressModel> Cc { get; set; } = new();

    /// <summary>
    /// Bcc
    /// </summary>
    public List<EmailAddressModel> Bcc { get; set; } = new();

    /// <summary>
    /// Return-Path（從 Header 取得）
    /// </summary>
    public string? ReturnPath { get; set; }

    /// <summary>
    /// 純文字本文
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// HTML 本文
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// 附件清單
    /// </summary>
    public List<EmailAttachmentModel> Attachments { get; set; } = new();

    /// <summary>
    /// 是否有附件
    /// </summary>
    public bool HasAttachments => Attachments.Count > 0;

    /// <summary>
    /// 主體 MIME Type
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// MIME-Version Header
    /// </summary>
    public string? MimeVersion { get; set; }

    /// <summary>
    /// 所有 Received Header
    /// </summary>
    public List<string> ReceivedHeaders { get; set; } = new();

    /// <summary>
    /// 所有 Headers
    /// </summary>
    public Dictionary<string, List<string>> Headers { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 是否為回覆信
    /// </summary>
    public bool IsReply => !string.IsNullOrWhiteSpace(InReplyTo);

    /// <summary>
    /// Thread Id
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// 父層 Message Id
    /// </summary>
    public string? ParentMessageId { get; set; }

    /// <summary>
    /// 郵件優先度：High / Normal / Low
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// 是否要求回條
    /// </summary>
    public bool RequestReadReceipt { get; set; }
}