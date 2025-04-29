using System.CommandLine;
using System.CommandLine.Parsing;

namespace SolutionSync;

class Program
{
    static int Main(string[] args)
    {
        return SolutionSync.Commands.SolutionSyncCommandParser.CommandParser.Invoke(args);
    }
}