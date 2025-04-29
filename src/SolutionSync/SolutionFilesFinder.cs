using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionSync;

internal static class SolutionFilesFinder
{
    public static string GetSolutionPathFromFileOrDirectory(string fileOrDirectoryPath)
    {
        if (File.Exists(fileOrDirectoryPath)
            && (fileOrDirectoryPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || fileOrDirectoryPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)))
        {
            return fileOrDirectoryPath;
        }

        if (Directory.Exists(fileOrDirectoryPath))
        {
            return Directory.GetFiles(fileOrDirectoryPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault()
                ?? Directory.GetFiles(fileOrDirectoryPath, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault()
                ?? throw new FileNotFoundException(fileOrDirectoryPath);
        }

        throw new FileNotFoundException(fileOrDirectoryPath);
    }

    public static Dictionary<string, string> GetSolutionPairPathFromDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            string[] slnFilePaths = Directory.GetFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly);
            string[] slnxFilePaths = Directory.GetFiles(directoryPath, "*.slnx", SearchOption.TopDirectoryOnly);
            int slnFileCount = slnFilePaths.Length;
            int slnxFileCount = slnxFilePaths.Length;

            if (slnFileCount * slnxFileCount == 1)
            {
                return new Dictionary<string, string>{
                    {".sln", slnFilePaths.First()},
                    {".slnx", slnxFilePaths.First()}
                };
            }

            throw new ArgumentException("Could not find files to sync");
        }

        throw new DirectoryNotFoundException(directoryPath);
    }

    public static Dictionary<string, string> GetSolutionPair(string[] paths)
    {
        if (paths.Length <= 1)
        {
            return GetSolutionPairPathFromDirectory(paths.FirstOrDefault(Environment.CurrentDirectory));
        }

        if (paths.Length == 2)
        {
            string? solutionA = GetSolutionPathFromFileOrDirectory(paths[0]);
            string? solutionB = GetSolutionPathFromFileOrDirectory(paths[1]);

            string solutionAExtension = Path.GetExtension(solutionA);
            string solutionBExtension = Path.GetExtension(solutionB);

            if (solutionAExtension == solutionBExtension)
            {
                throw new ArgumentException("Both solution files must have different extensions.");
            }

            return new Dictionary<string, string>
            {
                { solutionAExtension, solutionA },
                { solutionBExtension, solutionB }
            };
        }

        throw new ArgumentException("Invalid number of solution files provided. Please provide either one directory or two solution files.");
    }

    public static async Task<Dictionary<string, SolutionModel>> GetSolutionModelPairAsync(string[] paths, CancellationToken cancellationToken)
    {
        Dictionary<string, string> solutionFilePaths = GetSolutionPair(paths);

        Dictionary<string, SolutionModel> solutionModels = new();

        foreach (KeyValuePair<string, string> pair in solutionFilePaths)
        {
            ISolutionSerializer solutionSerializer = SolutionSerializers.GetSerializerByMoniker(pair.Value)
                ?? throw new SolutionException();
            SolutionModel solutionModel = await solutionSerializer.OpenAsync(pair.Value, cancellationToken);
            // Store path here for future reference
            solutionModel.Description = pair.Value;
            solutionModels.Add(pair.Key, solutionModel);
        }

        return solutionModels;
    }
}