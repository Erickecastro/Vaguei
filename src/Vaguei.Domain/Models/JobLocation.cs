namespace Vaguei.Domain.Models;

public sealed class JobLocation
{
    public string? Country { get; set; }

    public string? CountryCode { get; set; }

    public string? State { get; set; }

    public string? StateCode { get; set; }

    public string? City { get; set; }

    public string? RawLocation { get; set; }
}