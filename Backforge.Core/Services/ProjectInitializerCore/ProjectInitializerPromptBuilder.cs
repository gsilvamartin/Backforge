using System.Text;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;

namespace Backforge.Core.Services.ProjectInitializerCore;

/// <summary>
/// Builder for prompts used with LLM services
/// </summary>
public class ProjectInitializerPromptBuilder : IProjectInitializerPromptBuilder
{
    /// <summary>
    /// Builds the prompt for generating initialization commands
    /// </summary>
    /// <param name="blueprint">The architecture blueprint</param>
    /// <param name="projectStructure">The project structure to create</param>
    /// <returns>Prompt string for the LLamaService</returns>
    public string BuildInitializationPrompt(ArchitectureBlueprint blueprint, ProjectStructure projectStructure)
    {
        ArgumentNullException.ThrowIfNull(blueprint, nameof(blueprint));
        ArgumentNullException.ThrowIfNull(projectStructure, nameof(projectStructure));

        var sb = new StringBuilder();

        // Blueprint information
        var architecturePatterns = string.Join(", ", blueprint.ArchitecturePatterns
            .Select(p => p.Name));

        var components = string.Join(", ", blueprint.Components
            .Select(c => $"{c.Name} ({c.Type})"));

        var technologies = string.Join(", ", blueprint.Components
            .Select(c => c.ImplementationTechnology)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct());

        // Project structure information
        sb.AppendLine("You are an expert software developer who excels at initializing new projects.");
        sb.AppendLine("Your task is to generate command-line instructions to create a specific project structure.");
        sb.AppendLine();
        sb.AppendLine(
            "Based on the following architecture blueprint and desired project structure, generate only the necessary commands to:");
        sb.AppendLine("1. Create the initial project and dependencies");
        sb.AppendLine("2. Initialize necessary tools and configurations");
        sb.AppendLine("3. Create the directory structure and files as specified");
        sb.AppendLine();

        sb.AppendLine("Blueprint Context:");
        sb.AppendLine($"- User Requirement: {blueprint.Context?.UserRequirementText ?? "N/A"}");
        sb.AppendLine($"- Architecture Patterns: {architecturePatterns}");
        sb.AppendLine($"- Components: {components}");
        sb.AppendLine($"- Implementation Technologies: {technologies}");
        sb.AppendLine();

        sb.AppendLine("Desired Project Structure:");
        sb.AppendLine($"Project Name: {projectStructure.Name}");
        sb.AppendLine($"Description: {projectStructure.Description}");
        sb.AppendLine();

        sb.AppendLine("Directories and Files to Create:");

        foreach (var rootDir in projectStructure.RootDirectories)
        {
            AppendDirectoryInfo(sb, rootDir, 0);
        }

        sb.AppendLine();
        sb.AppendLine("For each command, provide:");
        sb.AppendLine("- The exact command and arguments to run");
        sb.AppendLine("- A working directory (if it should run in a subdirectory)");
        sb.AppendLine("- Whether the command is critical (should stop the process on failure)");
        sb.AppendLine("- A brief purpose description");
        sb.AppendLine();

        sb.AppendLine("Return the commands as a structured list of objects with these properties:");
        sb.AppendLine("- Command: The executable or command to run");
        sb.AppendLine("- Arguments: The arguments for the command");
        sb.AppendLine(
            "- WorkingDirectory: The directory where the command should be executed (relative to the base directory)");
        sb.AppendLine("- CriticalOnFailure: Whether failure should stop the entire process");
        sb.AppendLine("- Purpose: Brief description of what the command does");
        sb.AppendLine();

        sb.AppendLine("Include commands for:");
        sb.AppendLine("1. Creating necessary project structure (mkdir, etc.)");
        sb.AppendLine("2. Initializing the project (dotnet new, npm init, etc. based on the technology)");
        sb.AppendLine("3. Installing required dependencies");
        sb.AppendLine("4. Creating empty files as specified in the structure");
        sb.AppendLine("5. Initializing version control if appropriate");
        sb.AppendLine();

        sb.AppendLine("DO NOT include commands that:");
        sb.AppendLine("- Generate implementation code for business logic");
        sb.AppendLine("- Configure complex settings");
        sb.AppendLine("- Run the application or tests");

        return sb.ToString();
    }

    private void AppendDirectoryInfo(StringBuilder sb, ProjectDirectory directory, int level)
    {
        var indent = new string(' ', level * 2);
        sb.AppendLine($"{indent}- Directory: {directory.Path} ({directory.Description})");

        foreach (var file in directory.Files)
        {
            sb.AppendLine($"{indent}  - File: {file.Path} ({file.Description})");
        }

        foreach (var subDir in directory.Subdirectories)
        {
            AppendDirectoryInfo(sb, subDir, level + 1);
        }
    }
}