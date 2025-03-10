using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;

class DotnetSlnSync
{
    static Dictionary<string, string> FindSolutionFilesPairInDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            string[] slnFilePaths = Directory.GetFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly);
            string[] slnxFilePaths = Directory.GetFiles(directoryPath, "*.slnx", SearchOption.TopDirectoryOnly);

            int slnFileCount = slnFilePaths.Length;
            int slnxFileCount = slnxFilePaths.Length;

            if (slnFileCount * slnxFileCount == 1)
            {
                return new Dictionary<string, string>
                {
                    { "sln", slnFilePaths.First() },
                    { "slnx", slnxFilePaths.First() }
                };
            }
            else
            {
                throw new Exception("Could not find files to sync");
            }
        }
        throw new Exception("Directory does not exist");
    }

    static IEnumerable<T> GetDifference<T>(IEnumerable<T> slnValues, IEnumerable<T> slnxValues, IEqualityComparer<T>? comparer = null)
    {
        HashSet<T> slnValuesSet = slnValues.ToHashSet(comparer);
        HashSet<T> slnxValuesSet = slnxValues.ToHashSet(comparer);
        return slnValuesSet.Except(slnxValuesSet, comparer).Concat(slnxValuesSet.Except(slnValuesSet, comparer));
    }

    static string CreateConflictLineString(string type, string name, bool inSlnFile, bool inSlnxFile)
    {
        string[] stringBuilder = [
            inSlnFile ? "\x1b[0;32m(✓) .SLN\x1b[0m" : "\x1b[0;31m (x) .SLN\x1b[0m",
            " ",
            inSlnxFile ? "\x1b[0;32m(✓) .SLNX\x1b[0m" : "\x1b[0;31m (x) .SLNX\x1b[0m",
            $"\t {type}: {name}",
            "\n\t \u001b[3m(+) Add to all solutions, (-) Remove from all solutions, (ESC) Ignore\u001b[0m\t"
         ];
        return string.Join(string.Empty, stringBuilder);
    }

    static void HandleKeyboardInput(Action onAdd, Action onRemove, Action onCancel)
    {
        ConsoleKeyInfo action = Console.ReadKey();
        Console.WriteLine("\x1b[M\x1b[2k\x1b[M");
        switch (action.Key)
        {
            case ConsoleKey.OemPlus:
                onAdd();
                break;
            case ConsoleKey.OemMinus:
                onRemove();
                break;
            default:
                onCancel();
                break;
        }
    }
    
    static void HandleItemDifferences(SolutionModel slnSolution, SolutionModel slnxSolution, SolutionItemModel item)
    {
        bool itemInSlnSolution = slnSolution.FindItemById(item.Id) is not null;
        bool itemInSlnxSolution = slnxSolution.FindItemById(item.Id) is not null;

        SolutionModel solutionToAddTo = itemInSlnSolution ? slnxSolution : slnSolution;
        SolutionModel solutionToRemoveFrom = itemInSlnSolution ? slnSolution : slnxSolution;
        Console.Write(CreateConflictLineString(
            item is SolutionProjectModel ? "Project" : "Folder", 
            item is SolutionProjectModel ? ((SolutionProjectModel) item).FilePath : ((SolutionFolderModel) item).Path,
            itemInSlnSolution, 
            itemInSlnxSolution));

        HandleKeyboardInput(
            onAdd: () =>
            {
                if (item is SolutionFolderModel)
                {
                    solutionToAddTo.AddFolder(((SolutionFolderModel)item).Path);
                }
                if (item is SolutionProjectModel)
                {
                    solutionToAddTo.AddProject(((SolutionProjectModel)item).FilePath, ((SolutionProjectModel)item).TypeId.ToString(), ((SolutionProjectModel)item).Parent);
                }
            },
            onRemove: () =>
            {
                if (item is SolutionFolderModel)
                {
                    solutionToRemoveFrom.RemoveFolder((SolutionFolderModel)item);
                }
                if (item is SolutionProjectModel)
                {
                    solutionToRemoveFrom.RemoveProject((SolutionProjectModel)item);
                }
            },
            onCancel: () => { return; });
    }

    static void HandlePlatformDifference(SolutionModel slnSolution, SolutionModel slnxSolution, string platform)
    {
        bool platformInSlnSolution = slnSolution.Platforms.Contains(platform);
        bool platformInSlnxSolution = slnxSolution.Platforms.Contains(platform);
        SolutionModel solutionToAddTo = platformInSlnSolution ? slnxSolution : slnSolution;
        SolutionModel solutionToRemoveFrom = platformInSlnSolution ? slnSolution : slnxSolution;
        Console.Write(CreateConflictLineString(
            "Platform",
            platform,
            platformInSlnSolution,
            platformInSlnxSolution));

        HandleKeyboardInput(
            onAdd: () => solutionToAddTo.AddPlatform(platform),
            onRemove: () => solutionToRemoveFrom.RemovePlatform(platform),
            onCancel: () => { return; });
    }    
    
    static void HandleBuildTypeDifference(SolutionModel slnSolution, SolutionModel slnxSolution, string buildType)
    {
        bool buildTypeInSlnSolution = slnSolution.BuildTypes.Contains(buildType);
        bool buildTypeInSlnxSolution = slnxSolution.BuildTypes.Contains(buildType);
        SolutionModel solutionToAddTo = buildTypeInSlnSolution ? slnxSolution : slnSolution;
        SolutionModel solutionToRemoveFrom = buildTypeInSlnSolution ? slnSolution : slnxSolution;
        Console.Write(CreateConflictLineString(
            "Build Type",
            buildType,
            buildTypeInSlnSolution,
            buildTypeInSlnxSolution));

        HandleKeyboardInput(
            onAdd: () => solutionToAddTo.AddBuildType(buildType),
            onRemove: () => solutionToRemoveFrom.RemoveBuildType(buildType),
            onCancel: () => { return; });
    }

    static async Task<int> Main(string[] args)
    {
        // Find solution files to compare 

        Dictionary<string, string> solutionFilePaths;
        if (args.Length <= 1)
        {
            solutionFilePaths = FindSolutionFilesPairInDirectory(args.FirstOrDefault(Directory.GetCurrentDirectory()));
        }
        else
        {
            string? slnFile = args.ToList().Find(arg => Path.Exists(arg) && Path.GetExtension(arg) == ".sln");
            string? slnxFile = args.ToList().Find(arg => Path.Exists(arg) && Path.GetExtension(arg) == ".slnx");

            if (slnFile is null || slnxFile is null)
            {
                throw new Exception("Solution file could not be found");
            }

            solutionFilePaths = new Dictionary<string, string> {
                { "sln", slnFile },
                { "slnx", slnxFile },
            };
        }

        // Open solution files
        SolutionModel slnSolution = await SolutionSerializers.SlnFileV12.OpenAsync(solutionFilePaths["sln"], CancellationToken.None);
        SolutionModel slnxSolution = await SolutionSerializers.SlnXml.OpenAsync(solutionFilePaths["slnx"], CancellationToken.None);

        // PLATFORMS
        var differentSolutionPlatforms = GetDifference<string>(slnSolution.Platforms, slnxSolution.Platforms);
        if (differentSolutionPlatforms.Count() > 0)
        {
            Console.WriteLine("Platforms:");
            foreach (string platform in differentSolutionPlatforms)
            {
                HandlePlatformDifference(slnSolution, slnxSolution, platform);
            }
        }
        
        // BUILD TYPES
        var differentBuildTypes = GetDifference<string>(slnSolution.BuildTypes, slnxSolution.BuildTypes);
        if (differentBuildTypes.Count() > 0)
        {
            Console.WriteLine("Build Types:");
            foreach (string buildType in differentBuildTypes)
            {
                HandleBuildTypeDifference(slnSolution, slnxSolution, buildType);
            }
        }

        // SOLUTION ITEMS
        var differentSolutionItems = GetDifference<SolutionItemModel>(slnSolution.SolutionItems, slnxSolution.SolutionItems, new SolutionItemModelEqualityComparer());
        if (differentSolutionItems.Count() > 0)
        {
            Console.WriteLine("Projects:");
            foreach (SolutionItemModel item in differentSolutionItems)
            {
                HandleItemDifferences(slnSolution, slnxSolution, item);
            }
        }

        await SolutionSerializers.SlnFileV12.SaveAsync(solutionFilePaths["sln"], slnSolution, CancellationToken.None);
        await SolutionSerializers.SlnXml.SaveAsync(solutionFilePaths["slnx"], slnxSolution, CancellationToken.None);
        Console.WriteLine($"\x1b[0;32mSynced solutions: {solutionFilePaths["sln"]} {solutionFilePaths["slnx"]} \x1b[0m");
        return 0;
    }
}