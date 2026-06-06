using System.CommandLine;
using System.CommandLine.Invocation;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>publish</c> command — packages a catalog for remote distribution.
/// </summary>
public static class PublishCommand
{
    /// <summary>
    /// Creates the <c>publish</c> command.
    /// </summary>
    public static Command Create()
    {
        var inputArgument = new Argument<FileInfo>("input", "Path to ai-catalog.json file");
        var outputOption = new Option<DirectoryInfo>("--output", "Output directory for packaged catalog artifacts")
        {
            IsRequired = true
        };
        outputOption.AddAlias("-o");

        var cmd = new Command("publish", "Package a catalog for remote distribution (zip, sign, host)")
        {
            inputArgument,
            outputOption
        };

        cmd.SetHandler((InvocationContext context) =>
        {
            _ = context.ParseResult.GetValueForArgument(inputArgument);
            _ = context.ParseResult.GetValueForOption(outputOption);
            context.Console.WriteLine("Not yet implemented");
        });

        return cmd;
    }
}
