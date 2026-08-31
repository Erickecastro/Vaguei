using Vaguei.Application.Interfaces;

namespace Vaguei.ResumeParser.Services;

public sealed class ResumeParserService
{
    private readonly IReadOnlyCollection<IResumeParser> _parsers;

    public ResumeParserService(
        IEnumerable<IResumeParser> parsers)
    {
        _parsers = parsers.ToArray();
    }

    public IResumeParser GetParser(string extension)
    {
        var parser = _parsers.FirstOrDefault(
            parser => parser.CanParse(extension));

        return parser ??
               throw new NotSupportedException(
                   $"Formato de currículo não suportado: {extension}");
    }
}