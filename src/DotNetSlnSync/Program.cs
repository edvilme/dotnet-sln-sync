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

            if (slnFileCount * slnFileCount == 1)
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

    static void HandleItemDifferences(SolutionModel slnSolution, SolutionModel slnxSolution, SolutionItemModel item)
    {
        bool itemInSlnSolution = slnSolution.FindItemById(item.Id) is not null;
        bool itemInSlnxSolution = slnxSolution.FindItemById(item.Id) is not null;

        if (itemInSlnSolution && itemInSlnSolution)
        {
            return;
        }

        SolutionModel solutionToAddTo = itemInSlnSolution ? slnxSolution : slnSolution;
        SolutionModel solutionToRemoveFrom = itemInSlnSolution ? slnSolution : slnxSolution;

        string[] lineTextBuilder = [
            itemInSlnSolution ? "\x1b[0;32m(✓) .SLN\x1b[0m" : "\x1b[0;31m (x) .SLN\x1b[0m",
            " ",
            itemInSlnxSolution ? "\x1b[0;32m(✓) .SLNX\x1b[0m" : "\x1b[0;31m (x) .SLNX\x1b[0m",
            "\t", item is SolutionProjectModel ? $"Project: {((SolutionProjectModel) item).FilePath}" : $"Folder: {((SolutionFolderModel) item).Path}"
        ];
        Console.WriteLine(string.Join("", lineTextBuilder));

        Console.Write("\t \x1b[3m(+) Add to all solutions, (-) Remove from all solutions\x1b[0m\t");
        ConsoleKeyInfo action = Console.ReadKey();
        Console.WriteLine("\x1b[M\x1b[2k\x1b[M");

        switch (action.Key)
        {
            case ConsoleKey.OemPlus:
                if (item is SolutionFolderModel)
                {
                    solutionToAddTo.AddFolder(((SolutionFolderModel) item).Path);
                }
                if (item is SolutionProjectModel)
                {
                    solutionToAddTo.AddProject(((SolutionProjectModel)item).FilePath, ((SolutionProjectModel)item).TypeId.ToString(), ((SolutionProjectModel)item).Parent);
                }
                break;
            case ConsoleKey.OemMinus:
                if (item is SolutionFolderModel)
                {
                    solutionToRemoveFrom.RemoveFolder((SolutionFolderModel) item);
                }
                if (item is SolutionProjectModel)
                {
                    solutionToRemoveFrom.RemoveProject((SolutionProjectModel) item);
                }
                break;
            default:
                break;
        }
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

        // Get solution items differences
        HashSet<SolutionItemModel> slnSolutionItems = slnSolution.SolutionItems.ToHashSet();
        HashSet<SolutionItemModel> slnxSolutionItems = slnxSolution.SolutionItems.ToHashSet();

        // Get difference
        var differentSolutionItems = slnSolutionItems.Except(slnxSolutionItems).Concat(slnxSolutionItems.Except(slnSolutionItems));
        if (differentSolutionItems.Count() == 0)
        {
            Console.WriteLine("\x1b[0;32mNo differences found\x1b[0m");
        }
        else
        {
            Console.WriteLine("Projects:");
            foreach (SolutionItemModel item in differentSolutionItems)
            {
                HandleItemDifferences(slnSolution, slnxSolution, item);
            }

            await SolutionSerializers.SlnFileV12.SaveAsync(solutionFilePaths["sln"], slnSolution, CancellationToken.None);
            await SolutionSerializers.SlnXml.SaveAsync(solutionFilePaths["slnx"], slnxSolution, CancellationToken.None);

            Console.WriteLine($"\x1b[0;32mUpdated files: {solutionFilePaths["sln"]} {solutionFilePaths["slnx"]} \x1b[0m");
        }
        return 0;
    }
}