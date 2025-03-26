// File: Backforge.Core/Services/ProjectInitializerCore/PathProcessor.cs

using Backforge.Core.Models.StructureGenerator;
using Microsoft.Extensions.Logging;

namespace Backforge.Core.Services.ProjectInitializerCore;

/// <summary>
/// Processes and fixes paths in the project structure
/// </summary>
public class PathProcessor
{
    private readonly ILogger _logger;

    public PathProcessor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Normalizes the project structure to ensure proper paths and file organizations
    /// </summary>
    /// <param name="projectStructure">The project structure to normalize</param>
    /// <returns>The normalized project structure</returns>
    public ProjectStructure NormalizeProjectStructure(ProjectStructure projectStructure)
    {
        _logger.LogInformation("Normalizing project structure paths");

        try
        {
            // Fix root directory paths
            foreach (var rootDir in projectStructure.RootDirectories)
            {
                NormalizeDirectoryPaths(rootDir, "");
            }

            // Process files with paths containing directories
            foreach (var rootDir in projectStructure.RootDirectories.ToList())
            {
                ProcessFilesWithDirectoryPaths(rootDir, projectStructure.RootDirectories);
            }

            // Handle file root directories (files at the root level that should be files, not directories)
            ProcessFileRootDirectories(projectStructure);

            _logger.LogInformation("Project structure paths normalized successfully");
            return projectStructure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing project structure paths");
            throw;
        }
    }

    /// <summary>
    /// Normalizes the paths in a directory and its subdirectories
    /// </summary>
    private void NormalizeDirectoryPaths(ProjectDirectory directory, string parentPath)
    {
        // Normalize directory path
        directory.Name = NormalizePathPart(directory.Name);

        // Update the directory's path based on its parent path
        var directoryPath = string.IsNullOrEmpty(parentPath)
            ? directory.Name
            : Path.Combine(parentPath, directory.Name).Replace('\\', '/');

        directory.Path = directoryPath;

        // Normalize paths for all files in this directory
        foreach (var file in directory.Files)
        {
            // Update file name to just the file name without any directory parts
            file.Name = NormalizePathPart(Path.GetFileName(file.Name));

            // Update file path to include the directory path
            file.Path = Path.Combine(directoryPath, file.Name).Replace('\\', '/');
        }

        // Recursively process subdirectories
        foreach (var subDir in directory.Subdirectories)
        {
            NormalizeDirectoryPaths(subDir, directoryPath);
        }
    }

    /// <summary>
    /// Processes files that have directory paths in their names and creates proper directory structure
    /// </summary>
    private void ProcessFilesWithDirectoryPaths(ProjectDirectory directory, List<ProjectDirectory> rootDirectories)
    {
        // Find files with directory paths
        var filesToMove = directory.Files
            .Where(f => f.Name.Contains('/') || f.Name.Contains('\\'))
            .ToList();

        foreach (var file in filesToMove)
        {
            // Parse the directory path from the file name
            var pathParts = file.Name.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (pathParts.Length <= 1)
            {
                continue; // No directory part
            }

            // Get the actual file name (last part)
            var actualFileName = pathParts[pathParts.Length - 1];

            // Build the directory path (all parts except the last one)
            var directoryParts = pathParts.Take(pathParts.Length - 1).ToArray();

            // Find or create the target directory structure
            var targetDirectory = EnsureDirectoryStructure(directory, directoryParts);

            // Create a new file in the target directory
            var newFile = new ProjectFile
            {
                Name = actualFileName,
                Description = file.Description,
                Template = file.Template
            };

            // Update the path
            newFile.Path = Path.Combine(targetDirectory.Path, newFile.Name).Replace('\\', '/');

            // Add the file to the target directory
            targetDirectory.Files.Add(newFile);

            // Remove the original file
            directory.Files.Remove(file);
        }

        // Process subdirectories recursively
        foreach (var subDir in directory.Subdirectories.ToList())
        {
            ProcessFilesWithDirectoryPaths(subDir, rootDirectories);
        }
    }

    /// <summary>
    /// Ensures the directory structure exists, creating directories as needed
    /// </summary>
    private ProjectDirectory EnsureDirectoryStructure(ProjectDirectory parentDirectory, string[] directoryParts)
    {
        var currentDirectory = parentDirectory;

        foreach (var dirName in directoryParts)
        {
            var normalizedDirName = NormalizePathPart(dirName);
            var existingDir = currentDirectory.Subdirectories.FirstOrDefault(d =>
                string.Equals(d.Name, normalizedDirName, StringComparison.OrdinalIgnoreCase));

            if (existingDir == null)
            {
                // Create the directory if it doesn't exist
                var newDirectory = new ProjectDirectory
                {
                    Name = normalizedDirName,
                    Description = $"Directory for {normalizedDirName} files",
                    Subdirectories = new List<ProjectDirectory>(),
                    Files = new List<ProjectFile>()
                };

                // Set the path based on parent
                newDirectory.Path = Path.Combine(currentDirectory.Path, normalizedDirName).Replace('\\', '/');

                currentDirectory.Subdirectories.Add(newDirectory);
                currentDirectory = newDirectory;
            }
            else
            {
                currentDirectory = existingDir;
            }
        }

        return currentDirectory;
    }

    /// <summary>
    /// Processes root directories that are actually files
    /// </summary>
    private void ProcessFileRootDirectories(ProjectStructure projectStructure)
    {
        var fileDirs = projectStructure.RootDirectories
            .Where(dir =>
                // Check for file extensions
                Path.HasExtension(dir.Name) ||
                // Check for common root files
                IsCommonRootFile(dir.Name))
            .ToList();

        foreach (var fileDir in fileDirs)
        {
            // Convert the directory to a file
            var file = new ProjectFile
            {
                Name = fileDir.Name,
                Path = fileDir.Path,
                Description = fileDir.Description,
                Template = "" // No template by default
            };

            // Check if this file has any files that should be merged into the template
            if (fileDir.Files.Count == 1 && fileDir.Files[0].Name.Equals("content", StringComparison.OrdinalIgnoreCase))
            {
                file.Template = fileDir.Files[0].Template;
            }

            // Remove the directory from root directories
            projectStructure.RootDirectories.Remove(fileDir);

            // Find the appropriate directory for this file
            // For root files, they stay at the root level
            var projectDir = new ProjectDirectory
            {
                Name = fileDir.Name,
                Path = fileDir.Path,
                Description = fileDir.Description,
                Files = new List<ProjectFile> { file }
            };

            projectStructure.RootDirectories.Add(projectDir);
        }
    }

    /// <summary>
    /// Checks if a file is a common root file
    /// </summary>
    private bool IsCommonRootFile(string fileName)
    {
        var commonRootFiles = new[]
        {
            "Dockerfile", "docker-compose.yml", "docker-compose.yaml",
            ".gitignore", ".dockerignore", "README.md", "LICENSE",
            "package.json", "package-lock.json", "yarn.lock",
            ".env", ".env.example", ".editorconfig",
            "Makefile", "Gemfile", "Pipfile"
        };

        return commonRootFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a path part (file or directory name)
    /// </summary>
    private string NormalizePathPart(string pathPart)
    {
        // Remove any invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var normalizedPath = new string(pathPart
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        return normalizedPath;
    }
}