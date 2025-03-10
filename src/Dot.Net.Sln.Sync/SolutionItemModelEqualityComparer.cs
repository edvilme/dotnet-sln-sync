using Microsoft.VisualStudio.SolutionPersistence.Model;

public class SolutionItemModelEqualityComparer : IEqualityComparer<SolutionItemModel>
{
    public bool Equals(SolutionItemModel? x, SolutionItemModel? y)
    {
        if (x is SolutionProjectModel xProject && y is SolutionProjectModel yProject)
        {
            return xProject.FilePath == yProject.FilePath;
        }
        if (x is SolutionFolderModel xFolder && y is SolutionFolderModel yFolder)
        {
            return xFolder.ActualDisplayName == yFolder.ActualDisplayName;
        }
        return x?.Id == y?.Id;
    }

    public int GetHashCode(SolutionItemModel obj)
    {
        // Use the same properties from Equals(...) to build a consistent hash
        // e.g. return HashCode.Combine(obj.Id, obj.Name);
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