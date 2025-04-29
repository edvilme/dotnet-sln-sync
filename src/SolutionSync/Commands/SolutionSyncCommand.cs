using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using SolutionSync.Comparer;
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
    private string? overrideFrom = parseResult.GetValueForOption(SolutionSyncCommandParser.OverrideFromOption);

    private bool hasDifferences = false;
    private bool hasChanges = false;

    public static void PrintComparisonLine<T>((string Included, string Excluded, T obj) item, string aName, string bName, Func<T, string>? toStringFunc = null)
    {
        string aNameQualifier = item.Included == aName ? "\x1b[0;32m+" : "\x1b[0;31m-";
        string bNameQualifier = item.Included == bName ? "\x1b[0;32m+" : "\x1b[0;31m-";

        string objRepresentation = toStringFunc != null ? toStringFunc(item.obj) : item.obj?.ToString() ?? "null";

        Console.WriteLine($"   {aNameQualifier} {aName}:\t{objRepresentation}\x1b[0m");
        Console.WriteLine($"   {bNameQualifier} {bName}:\t{objRepresentation}\x1b[0m");
    }

    public void HandleDifference(SolutionModel solutionIncludingItem, SolutionModel solutionNotIncludingItem, Action<SolutionModel>? addFunction, Action<SolutionModel>? removeFunction)
    {
        if (diffOnly)
        {
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

        if (differentSolutionPlatforms.Count() > 0)
        {
            hasDifferences = true;
            Console.WriteLine("Platforms:");
            foreach (var platform in differentSolutionPlatforms)
            {
                PrintComparisonLine(platform, ".sln", ".slnx");

                HandleDifference(
                    solutionModelPair[platform.Included],
                    solutionModelPair[platform.Excluded],
                    addFunction: (SolutionModel s) =>
                    {
                        s.AddPlatform(platform.obj);
                    },
                    removeFunction: (SolutionModel s) =>
                    {
                        s.RemovePlatform(platform.obj);
                    });
            }
        }

        // Build Types
        var differentBuildTypes = NamedHashSetDiff.GetNamedDiff(slnSolution.BuildTypes, slnxSolution.BuildTypes, ".sln", ".slnx");
        if (differentBuildTypes.Count() > 0)
        {
            hasDifferences = true;
            Console.WriteLine("Build Types:");
            foreach (var buildType in differentBuildTypes)
            {
                PrintComparisonLine(buildType, ".sln", ".slnx");

                HandleDifference(
                    solutionModelPair[buildType.Included],
                    solutionModelPair[buildType.Excluded],
                    addFunction: (SolutionModel s) =>
                    {
                        s.AddBuildType(buildType.obj);
                    },
                    removeFunction: (SolutionModel s) =>
                    {
                        s.RemoveBuildType(buildType.obj);
                    });
            }
        }

        // Folders
        var differentSolutionFolders = NamedHashSetDiff.GetNamedDiff<SolutionFolderModel>(slnSolution.SolutionFolders, slnxSolution.SolutionFolders, ".sln", ".slnx", new SolutionItemModelEqualityComparer());
        if (differentSolutionFolders.Count() > 0)
        {
            hasDifferences = true;
            Console.WriteLine("Folders:");
            foreach (var folder in differentSolutionFolders)
            {
                PrintComparisonLine(folder, ".sln", ".slnx", (SolutionFolderModel f) => f.Path);

                HandleDifference(
                    solutionModelPair[folder.Included],
                    solutionModelPair[folder.Excluded],
                    addFunction: (SolutionModel s) =>
                    {
                        s.AddFolder(folder.obj.Path);
                    },
                    removeFunction: (SolutionModel s) =>
                    {
                        s.RemoveFolder(folder.obj);
                    });
            }
        }

        // Projects
        var differentProjects = NamedHashSetDiff.GetNamedDiff<SolutionProjectModel>(slnSolution.SolutionProjects, slnxSolution.SolutionProjects, ".sln", ".slnx", new SolutionItemModelEqualityComparer());
        if (differentProjects.Count() > 0)
        {
            hasDifferences = true;
            Console.WriteLine("Projects:");
            foreach (var project in differentProjects)
            {
                PrintComparisonLine(project, ".sln", ".slnx", (SolutionProjectModel p) => p.FilePath);

                HandleDifference(
                    solutionModelPair[project.Included],
                    solutionModelPair[project.Excluded],
                    addFunction: (SolutionModel s) =>
                    {
                        s.AddProject(
                            project.obj.FilePath, 
                            project.obj.Type,
                            project.obj.Parent != null ? s.FindFolder(project.obj.Parent.Path) : null);
                    },
                    removeFunction: (SolutionModel s) =>
                    {
                        s.RemoveProject(project.obj);
                    });
            }
        }

        if (!hasDifferences)
        {
            Console.WriteLine("No differences detected.");
            return Task.FromResult(0);
        }

        if (!hasChanges)
        {
            Console.WriteLine("No changes detected.");
            return Task.FromResult(0);
        }

        // Save changes
        Console.WriteLine("==========================");
        Console.Write("Save changes? [Y]es or [N]o? (default: Y): ");
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
