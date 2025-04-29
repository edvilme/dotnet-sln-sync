using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionSync.Commands;

internal class SolutionSyncCommand(
    ParseResult parseResult) : CommandBase(parseResult)
{
    private string[] solutionFilesPath = parseResult.GetValueForArgument(SolutionSyncCommandParser.SolutionFilesPathsArgument);
    private bool diffOnly = parseResult.GetValueForOption(SolutionSyncCommandParser.DiffOnlyOption);

    private bool hasDifferences = false;
    private bool hasChanges = false;

    public static void PrintDiff<T>((string Included, string Excluded, T obj) item, string aName, string bName, Func<T, string>? toStringFunc = null)
    {
        string aNameQualifier = item.Included == aName ? "\x1b[0;32m+" : "\x1b[0;31m-";
        string bNameQualifier = item.Included == bName ? "\x1b[0;32m+" : "\x1b[0;31m-";

        string objRepresentation = toStringFunc != null ? toStringFunc(item.obj) : item.obj?.ToString() ?? "null";

        Console.WriteLine($"{aNameQualifier} {aName}:\t{objRepresentation}\x1b[0m");
        Console.WriteLine($"{bNameQualifier} {bName}:\t{objRepresentation}\x1b[0m");
    }

    public void UpdateSolutionDifferences(SolutionModel solutionIncludingItem, SolutionModel solutionNotIncludingItem, Action<SolutionModel>? addFunction, Action<SolutionModel>? removeFunction)
    {
        if (diffOnly)
        {
            Console.WriteLine();
            return;
        }

        Console.Write($"\t[A]dd to both, [R]emove from both, or [S]kip? (default: S): ");
        ConsoleKey key = Console.ReadKey(true).Key;
        switch (key)
        {
            case ConsoleKey.A:
                Console.WriteLine("Add");
                addFunction?.Invoke(solutionNotIncludingItem);
                hasChanges = true;
                break;
            case ConsoleKey.R:
                Console.WriteLine("Remove");
                removeFunction?.Invoke(solutionIncludingItem);
                hasChanges = true;
                break;
            default:
                Console.WriteLine("Skip");
                break;
        }
        Console.WriteLine();
    }

    private void ProcessDifferences<T>(
    IEnumerable<(string Included, string Excluded, T obj)> differences,
    string categoryName,
    Dictionary<string, SolutionModel> solutionModelPair,
    Action<SolutionModel, T> addFunction,
    Action<SolutionModel, T> removeFunction,
    Func<T, string>? toStringFunc = null)
    {
        if (!differences.Any()) return;

        hasDifferences = true;
        Console.WriteLine($"{categoryName}:");

        foreach (var item in differences)
        {
            PrintDiff(item, ".sln", ".slnx", toStringFunc);

            UpdateSolutionDifferences(
                solutionModelPair[item.Included],
                solutionModelPair[item.Excluded],
                addFunction: s => addFunction(s, item.obj),
                removeFunction: s => removeFunction(s, item.obj));
        }
    }

    public override Task<int> ExecuteAsync()
    {
        Dictionary<string, SolutionModel> solutionModelPair = SolutionFilesFinder.GetSolutionModelPairAsync(solutionFilesPath, CancellationToken.None).Result;

        SolutionModel slnSolution = solutionModelPair[".sln"];
        SolutionModel slnxSolution = solutionModelPair[".slnx"];

        Console.WriteLine($"Solution file (a): {slnSolution.Description}");
        Console.WriteLine($"Solution file (b): {slnxSolution.Description}");
        Console.WriteLine();

        // Platforms
        var differentSolutionPlatforms = NamedHashSetDiff.GetNamedDiff(slnSolution.Platforms, slnxSolution.Platforms, ".sln", ".slnx");
        ProcessDifferences(
            differentSolutionPlatforms,
            "Platforms",
            solutionModelPair,
            (s, p) => s.AddPlatform(p),
            (s, p) => s.RemovePlatform(p));

        // Build Types
        var differentBuildTypes = NamedHashSetDiff.GetNamedDiff(slnSolution.BuildTypes, slnxSolution.BuildTypes, ".sln", ".slnx");
        ProcessDifferences(
            differentBuildTypes,
            "Build Types",
            solutionModelPair,
            (s, b) => s.AddBuildType(b),
            (s, b) => s.RemoveBuildType(b));

        // Folders
        var differentSolutionFolders = NamedHashSetDiff.GetNamedDiff<SolutionFolderModel>(
            slnSolution.SolutionFolders,
            slnxSolution.SolutionFolders,
            ".sln",
            ".slnx",
            new SolutionItemModelEqualityComparer());
        differentSolutionFolders.Sort((x, y) => x.obj.Path.CompareTo(y.obj.Path));
        ProcessDifferences(
            differentSolutionFolders,
            "Folders",
            solutionModelPair,
            (s, f) => s.AddFolder(f.Path),
            (s, f) => s.RemoveFolder(f),
            f => f.Path);

        // Projects
        var differentProjects = NamedHashSetDiff.GetNamedDiff<SolutionProjectModel>(
            slnSolution.SolutionProjects,
            slnxSolution.SolutionProjects,
            ".sln",
            ".slnx",
            new SolutionItemModelEqualityComparer());
        ProcessDifferences(
            differentProjects,
            "Projects",
            solutionModelPair,
            (s, p) => s.AddProject(
                p.FilePath,
                p.Type,
                p.Parent != null ? s.FindFolder(p.Parent.Path) : null),
            (s, p) => s.RemoveProject(p),
            p => $"{p.FilePath}\t Solution Folder: {p.Parent?.Path ?? "."}");

        if (diffOnly)
        {
            return Task.FromResult(hasDifferences ? 1 : 0);
        }

        if (!hasDifferences || !hasChanges)
        {
            return Task.FromResult(0);
        }

        // Save changes
        Console.WriteLine("===========================================");
        Console.Write("Save changes? [Y]es or [N]o? (default: N): ");
        ConsoleKey key = Console.ReadKey(true).Key;

        if (key != ConsoleKey.Y)
        {
            return Task.FromResult(0);
        }

        slnSolution.SerializerExtension?.Serializer.SaveAsync(slnSolution.Description!, slnSolution, CancellationToken.None).Wait();
        slnxSolution.SerializerExtension?.Serializer.SaveAsync(slnxSolution.Description!, slnxSolution, CancellationToken.None).Wait();
        Console.WriteLine("Changes saved.");
        return Task.FromResult(0);
    }
}
