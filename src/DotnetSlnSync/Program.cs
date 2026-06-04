using System.CommandLine;
using System.CommandLine.Parsing;

namespace DotnetSlnSync;

class Program
{
    static int Main(string[] args)
    {
        return DotnetSlnSync.Commands.SolutionSyncCommandParser.CommandParser.Invoke(args);
    }
}