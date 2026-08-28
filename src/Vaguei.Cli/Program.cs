using Vaguei.Application.Interfaces;
using Vaguei.ResumeParser.Parsers;

if (args.Length == 0)
{
    Console.WriteLine("Vaguei");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("dotnet run --project src/Vaguei.Cli -- <curriculo.odt>");
    return;
}

var filePath = args[0];

if (!File.Exists(filePath))
{
    Console.WriteLine($"Arquivo não encontrado: {filePath}");
    return;
}

var extension = Path.GetExtension(filePath);

IResumeParser parser = new OdtResumeParser();

if (!parser.CanParse(extension))
{
    Console.WriteLine($"Formato ainda não suportado: {extension}");
    return;
}

await using var fileStream = File.OpenRead(filePath);

var text = await parser.ExtractTextAsync(fileStream);

Console.WriteLine("==================================");
Console.WriteLine("           VAGUEI");
Console.WriteLine("==================================");
Console.WriteLine();

Console.WriteLine($"Arquivo: {Path.GetFileName(filePath)}");
Console.WriteLine($"Formato: {extension}");
Console.WriteLine();

Console.WriteLine("Conteúdo extraído:");
Console.WriteLine("----------------------------------");
Console.WriteLine(text);
