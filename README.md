# `dotnet slnxsync`

Use this .NET Tool to manually sync .sln and .slnx solution files.

[![NuGet package](https://img.shields.io/nuget/v/dotnet-sln-sync.svg)](https://nuget.org/packages/dotnet-sln-sync)

The tool will prompt you to add or remove the projects/solution folders from the solution files, one by one.

![image](https://github.com/user-attachments/assets/e30554cf-203b-45f4-9e71-397a437c2ac9)

After execution, both files should have the same projects and solution folders.

![image](https://github.com/user-attachments/assets/3af596d7-e092-44cb-a980-8010cbb67777)

## Installation
This tool is available to download via NuGet
```bash
dotnet tool install --global dotnet-sln-sync
```

## Usage
Call this tool manually
```bash
slnxsync <SLN_FILE> <SLNX_FILE>
```

When working with teams, it might be useful to create a [git hook](https://git-scm.com/docs/githooks) that calls this tool on commit, or before pushing to ensure no discrepancies exist between the files. 

## Contributing
This is an experimental tool, but feel free to create new issues or pull requests. 


# Dependencies
This project uses [vs-solutionpersistence](https://github.com/microsoft/vs-solutionpersistence)
