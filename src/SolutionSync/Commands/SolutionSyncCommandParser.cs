using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionSync.Commands;

public class SolutionSyncCommandParser
{
    public static Parser CommandParser;

    public static RootCommand RootCommand = new RootCommand("dotnet sln-sync");

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

        CommandParser = new Parser(RootCommand);
    }
}
