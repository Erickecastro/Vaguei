namespace Vaguei.Application.Models;

public sealed record JobSourceSearchSummary(
    string Source,
    int JobCount,
    bool Succeeded);
