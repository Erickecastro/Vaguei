using Vaguei.Application.Services;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Models;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;
using Vaguei.Domain.Enums;

if (args.Length == 0)
{
    Console.WriteLine("Vaguei");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine(
        "dotnet run --project src/Vaguei.Cli -- <curriculo>");
    Console.WriteLine();
    Console.WriteLine("Formatos suportados:");
    Console.WriteLine("- ODT");
    Console.WriteLine("- DOCX");
    Console.WriteLine("- PDF");
    Console.WriteLine("- TXT");
    return;
}

var filePath = args[0];

if (!File.Exists(filePath))
{
    Console.WriteLine(
        $"Arquivo não encontrado: {filePath}");
    return;
}

var extension = Path.GetExtension(filePath);

var parserService = new ResumeParserService(
[
    new OdtResumeParser(),
    new DocxResumeParser(),
    new PdfResumeParser(),
    new TextResumeParser()
]);

try
{
    var parser = parserService.GetParser(extension);

    await using var fileStream =
        File.OpenRead(filePath);

    var text = await parser.ExtractTextAsync(
        fileStream);

    var analyzer = new ResumeAnalyzer();

    var profile = analyzer.Analyze(text);

    Console.WriteLine("==================================");
    Console.WriteLine("           VAGUEI");
    Console.WriteLine("==================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Arquivo: {Path.GetFileName(filePath)}");

    Console.WriteLine(
        $"Formato: {extension}");

    Console.WriteLine();

    Console.WriteLine("Conteúdo extraído:");
    Console.WriteLine("----------------------------------");
    Console.WriteLine(text);

    Console.WriteLine();
    Console.WriteLine("==================================");
    Console.WriteLine("Perfil identificado");
    Console.WriteLine("==================================");
    Console.WriteLine();

    Console.WriteLine($"Nome: {profile.Name}");
    Console.WriteLine($"Cargo: {profile.ProfessionalTitle}");

    Console.WriteLine();
    Console.WriteLine("Tecnologias identificadas:");

    foreach (var skill in profile.Skills.OrderBy(
                 skill => skill))
    {
        Console.WriteLine($"- {skill}");
    }

    Console.WriteLine();
    Console.WriteLine("Experiências profissionais:");

    foreach (var experience in profile.Experiences)
    {
        Console.WriteLine();

        Console.WriteLine(
            $"Cargo: {experience.Position}");

        Console.WriteLine(
            $"Empresa: {experience.Company}");

        var endPeriod = experience.IsCurrent
            ? "Atual"
            : experience.EndYear?.ToString() ?? "?";

        Console.WriteLine(
            $"Período: {experience.StartYear} — {endPeriod}");

        Console.WriteLine("Descrição:");

        Console.WriteLine(
            experience.Description);
    }

    var preferences =
        new JobSearchPreferences();

    var queryBuilder =
        new JobSearchQueryBuilder();

    var query =
        queryBuilder.Build(
            profile,
            preferences);

    Console.WriteLine();
    Console.WriteLine("==================================");
    Console.WriteLine("Consulta gerada");
    Console.WriteLine("==================================");
    Console.WriteLine();

    Console.WriteLine("Palavras-chave:");

    if (query.Keywords.Count == 0)
    {
        Console.WriteLine("- Nenhuma");
    }
    else
    {
        foreach (var keyword in query.Keywords)
        {
            Console.WriteLine($"- {keyword}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Localizações:");

    if (query.Locations.Count == 0)
    {
        Console.WriteLine("- Qualquer localização");
    }
    else
    {
        foreach (var location in query.Locations)
        {
            Console.WriteLine($"- {location}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Modelos de trabalho:");

    if (query.WorkModels.Count == 0)
    {
        Console.WriteLine("- Qualquer modelo");
    }
    else
    {
        foreach (var workModel in query.WorkModels)
        {
            Console.WriteLine($"- {workModel}");
        }
    }

    using var httpClient =
        new HttpClient();

    var jobSource =
        new ArbeitnowJobSource(httpClient);

    var jobs =
        await jobSource.SearchAsync(query);

    var freshnessFilter =
        new JobFreshnessFilter();

    var freshJobs =
        freshnessFilter.Filter(
            jobs,
            preferences,
            DateTimeOffset.UtcNow);

    var jobMatcher =
        new JobMatcher();

    var matchedJobs =
        freshJobs
            .Select(
                job =>
                    jobMatcher.Match(
                        profile,
                        job,
                        preferences))
            .OrderByDescending(
                result => result.Score)
            .ThenByDescending(
                result => result.Job.PublishedAt)
            .Take(10)
            .ToList();

    Console.WriteLine();
    Console.WriteLine("==================================");
    Console.WriteLine("Vagas encontradas");
    Console.WriteLine("==================================");
    Console.WriteLine();

    if (matchedJobs.Count == 0)
    {
        Console.WriteLine(
            "Nenhuma vaga encontrada.");
    }

    foreach (var match in matchedJobs)
    {
        var job =
            match.Job;

        Console.WriteLine(
            $"Cargo: {job.Title}");

        Console.WriteLine(
            $"Empresa: {job.Company}");

        Console.WriteLine(
            $"Local: {job.Location.RawLocation}");

        Console.WriteLine(
            $"Modelo: {job.WorkModel}");

        Console.WriteLine(
            $"Compatibilidade: {match.Score:F2}%");

        Console.WriteLine(
            $"Publicada: {job.PublishedAt:dd/MM/yyyy HH:mm} UTC");

        Console.WriteLine(
            $"Fonte: {job.Source}");

        Console.WriteLine(
            $"URL: {job.Url}");

        Console.WriteLine(
            "Motivos:");

        foreach (var reason in match.Reasons)
        {
            var symbol =
                reason.Kind switch
                {
                    JobMatchReasonKind.Positive =>
                        "+",

                    JobMatchReasonKind.Negative =>
                        "-",

                    _ =>
                        "~"
                };

            Console.WriteLine(
                $"{symbol} {reason.Description}");
        }

        Console.WriteLine(
            "----------------------------------");
    }
}
catch (NotSupportedException exception)
{
    Console.WriteLine(exception.Message);
}
catch (InvalidDataException exception)
{
    Console.WriteLine(
        $"Não foi possível ler o currículo: {exception.Message}");
}