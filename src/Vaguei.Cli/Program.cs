using Vaguei.Application.Services;
using Vaguei.ResumeParser.Parsers;
using Vaguei.ResumeParser.Services;

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
    new PdfResumeParser()
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

    Console.WriteLine(
        $"Nome: {profile.Name}");

    Console.WriteLine(
        $"Cargo: {profile.Summary}");

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