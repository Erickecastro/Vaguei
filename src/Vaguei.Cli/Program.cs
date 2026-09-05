using Vaguei.Application.Services;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Models;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;
using Vaguei.Domain.Enums;

var filePath = args.FirstOrDefault(argument =>
    !argument.Equals(
        "--show-raw",
        StringComparison.OrdinalIgnoreCase));

var showRawResume = args.Any(argument =>
    argument.Equals(
        "--show-raw",
        StringComparison.OrdinalIgnoreCase));

if (string.IsNullOrWhiteSpace(filePath))
{
    Console.WriteLine("Vaguei");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine(
        "dotnet run --project src/Vaguei.Cli -- <curriculo> [--show-raw]");
    Console.WriteLine();
    Console.WriteLine("Formatos suportados:");
    Console.WriteLine("- ODT");
    Console.WriteLine("- DOCX");
    Console.WriteLine("- PDF");
    Console.WriteLine("- TXT");
    return;
}

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

    if (showRawResume)
    {
        Console.WriteLine();
        Console.WriteLine("Conteúdo extraído:");
        Console.WriteLine("----------------------------------");
        Console.WriteLine(text);
    }

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
    Console.WriteLine(
        "Relevância das competências:");

    foreach (var skill in
             profile.DetailedSkills
                 .OrderByDescending(
                     skill =>
                         skill.Relevance)
                 .ThenBy(
                     skill =>
                         skill.Name))
    {
        Console.WriteLine(
            $"- {skill.Name}: {skill.Relevance}");

        if (skill.Evidence.Count > 0)
        {
            Console.WriteLine(
                $"  Evidências: {string.Join(", ",
                    skill.Evidence.Select(item => item.Source))}");
        }
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

    using var httpClient =
        new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

    var orchestrator =
        new JobSearchOrchestrator(
        [
            new ArbeitnowJobSource(httpClient),
            new AshbyJobSource(httpClient),
            new GreenhouseJobSource(httpClient),
            new InHireJobSource(httpClient),
            new LeverJobSource(httpClient),
            new SmartRecruitersJobSource(httpClient),
            new WorkableJobSource(httpClient)
        ]);

    var searchResult = await orchestrator.SearchAsync(
        profile,
        preferences,
        DateTimeOffset.UtcNow);

    var query = searchResult.Query;

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

    var matchedJobs = searchResult.Matches
        .Take(10)
        .ToList();

    Console.WriteLine();
    Console.WriteLine(
        $"Vagas coletadas: {searchResult.CollectedJobCount}");
    Console.WriteLine(
        $"Vagas únicas e válidas: {searchResult.UniqueJobCount}");

    foreach (var failure in searchResult.SourceFailures)
    {
        Console.WriteLine(
            $"Fonte indisponível: {failure.Source} — {failure.Message}");
    }

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

        if (job.SkillRequirements.Count > 0)
        {
            Console.WriteLine(
                $"Competências da vaga: {string.Join(", ",
                    job.SkillRequirements.Select(requirement =>
                        $"{requirement.Name} ({requirement.Level})"))}");
        }

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
