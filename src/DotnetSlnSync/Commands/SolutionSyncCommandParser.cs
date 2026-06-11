using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetSlnSync.Commands;

public class SolutionSyncCommandParser
{
    public static Parser CommandParser;

    public static RootCommand RootCommand = new RootCommand("Use this .NET tool to manually sync .sln and .slnx solution files.");

    public static Argument<string[]> SolutionFilesPathsArgument = new Argument<string[]>("SolutionFiles")
    {
        Description = "The solution files to sync. If not specified, all .sln files in the current directory will be used.",
        Arity = new(0, 2)
    };

    public static Option<bool> DiffOnlyOption = new Option<bool>("--diff-only")
    {
        Description = "Only show the differences between the two solution files.",
        ArgumentHelpName = "DiffOnly"
    };

    static SolutionSyncCommandParser()
    {
        RootCommand.AddArgument(SolutionFilesPathsArgument);
        RootCommand.AddOption(DiffOnlyOption);

        RootCommand.SetHandler((InvocationContext context) =>
        {
            var parseResult = context.ParseResult;
            return new SolutionSyncCommand(parseResult).ExecuteAsync();
        });

        CommandParser = new CommandLineBuilder(RootCommand)
            .UseHelp(context =>
            {
                context.HelpBuilder.CustomizeLayout(_ =>
                    HelpBuilder.Default.GetLayout().Append(WriteExamplesSection));
            })
            .Build();
    }

    private static void WriteExamplesSection(HelpContext context)
    {
        context.Output.WriteLine("Examples:");
        context.Output.WriteLine("  dotnet sln-sync [<DIRECTORY>] [--diff-only]");
        context.Output.WriteLine("  dotnet sln-sync <FILE_OR_DIR> <FILE_OR_DIR> [--diff-only]");
    }
}
