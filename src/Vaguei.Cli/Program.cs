using Vaguei.Application.Services;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;
using Vaguei.Collectors.Sources;
using Vaguei.Domain.Models;

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

using var httpClient = new HttpClient();

var jobSource = new ArbeitnowJobSource(httpClient);

var query = new JobSearchQuery
{
    Keywords =
    [
        ".NET",
        "C#",
        "ASP.NET Core"
    ],
    IncludeRemote = true
};

Console.WriteLine();
Console.WriteLine("==================================");
Console.WriteLine("Vagas encontradas");
Console.WriteLine("==================================");
Console.WriteLine();

var jobs = await jobSource.SearchAsync(query);

foreach (var job in jobs.Take(10))
{
    Console.WriteLine($"Cargo: {job.Title}");
    Console.WriteLine($"Empresa: {job.Company}");
    Console.WriteLine($"Local: {job.Location}");
    Console.WriteLine($"Fonte: {job.Source}");
    Console.WriteLine($"URL: {job.Url}");
    Console.WriteLine("----------------------------------");
}