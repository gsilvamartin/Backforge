using Backforge.Core.Models;
using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;

/// <summary>
/// Helper class for building file generation prompts
/// </summary>
public static class FilePromptBuilder
{
    /// <summary>
    /// Builds a prompt for file generation
    /// </summary>
    public static string BuildFileGenerationPrompt(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        ProjectFile fileInfo)
    {
        // Implementation of prompt building logic
        return $"""
                You are an expert software engineer tasked with generating a file for a project.

                USER REQUIREMENT CONTEXT:
                {requirementContext.UserRequirementText}

                FILE TO GENERATE:
                Path: {fileInfo.Path}
                Name: {fileInfo.Name}
                Description: {fileInfo.Description}
                Purpose: {fileInfo.Purpose}

                Your task is to generate the complete content for this file, ensuring it:
                1. Implements all functionality described in its purpose
                2. Follows best practices for the file type
                3. Includes proper error handling and documentation
                4. Integrates correctly with the project structure

                RESPOND ONLY WITH THE COMPLETE CODE FOR THE FILE.
                """;
    }

    /// <summary>
    /// Builds a prompt for retry file generation with additional context
    /// </summary>
    public static string BuildRetryFileGenerationPrompt(
        AnalysisContext requirementContext,
        ArchitectureBlueprint blueprint,
        ProjectStructure projectStructure,
        ProjectFile fileInfo,
        string previousError)
    {
        return $"""
                You are an expert software engineer tasked with generating a file for a project.
                A previous attempt to generate this file failed. Please provide a corrected implementation.

                PREVIOUS ERROR:
                {previousError}

                USER REQUIREMENT CONTEXT:
                {requirementContext.UserRequirementText}

                FILE TO GENERATE:
                Path: {fileInfo.Path}
                Name: {fileInfo.Name}
                Description: {fileInfo.Description}
                Purpose: {fileInfo.Purpose}

                SPECIAL RETRY INSTRUCTIONS:
                1. Pay careful attention to the previous error
                2. Ensure your implementation addresses the issue completely
                3. Provide a robust, well-structured solution
                4. Include thorough error handling and validation

                Your task is to generate the complete content for this file, ensuring it:
                1. Implements all functionality described in its purpose
                2. Follows best practices for the file type
                3. Includes proper error handling and documentation
                4. Integrates correctly with the project structure

                RESPOND ONLY WITH THE COMPLETE CODE FOR THE FILE.
                """;
    }
}