namespace EmailResponseService.Model;

public class EmailAddressModel
{
    /// <summary>
    /// 顯示名稱，例如 Alice
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Email Address，例如 alice@example.com
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 原始字串，例如 Alice &lt;alice@example.com&gt;
    /// </summary>
    public string? Raw { get; set; }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Address))
            return $"{Name} <{Address}>";

        return Address ?? Raw ?? string.Empty;
    }
}