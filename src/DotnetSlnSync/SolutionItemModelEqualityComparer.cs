using Microsoft.VisualStudio.SolutionPersistence.Model;
using System.Diagnostics.CodeAnalysis;

namespace DotnetSlnSync;

internal class SolutionItemModelEqualityComparer : IEqualityComparer<SolutionItemModel>
{
    public bool Equals(SolutionItemModel? x, SolutionItemModel? y)
    {
        if (x is SolutionProjectModel xProject && y is SolutionProjectModel yProject)
        {
            return Equals(xProject.FilePath, yProject.FilePath)
                && Equals(xProject.Parent, yProject.Parent);
        }
        if (x is SolutionFolderModel xFolder && y is SolutionFolderModel yFolder)
        {
            return Equals(xFolder.Path, yFolder.Path);
        }
        return x?.Id == y?.Id;
    }

    public int GetHashCode([DisallowNull] SolutionItemModel obj)
    {
        if (obj is SolutionProjectModel objProject)
        {
            return objProject.FilePath.GetHashCode();
        }
        if (obj is SolutionFolderModel objFolder)
        {
            return objFolder.ActualDisplayName.GetHashCode();
        }
        return obj.Id.GetHashCode();
    }
}