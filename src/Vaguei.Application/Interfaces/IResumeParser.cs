namespace Vaguei.Application.Interfaces;

public interface IResumeParser
{
    bool CanParse(string extension);

    Task<string> ExtractTextAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);
}
