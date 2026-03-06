namespace EmailResponseService.Model;

public class EmailAttachmentModel
{
    /// <summary>
    /// 檔名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// MIME Content-Type，例如 application/pdf
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Content-Disposition，例如 attachment / inline
    /// </summary>
    public string? ContentDisposition { get; set; }

    /// <summary>
    /// Content-Id（inline image 常用）
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// 檔案大小（bytes）
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 是否為 inline 資源
    /// </summary>
    public bool IsInline { get; set; }
}