using Vaguei.Domain.Enums;

namespace Vaguei.Domain.Models;

public sealed record CandidateLanguage(
    string Name,
    LanguageProficiency Proficiency);
