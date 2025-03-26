using Backforge.Core.Models.Architecture;
using Backforge.Core.Models.StructureGenerator;

namespace Backforge.Core.Services.ProjectInitializerCore.Interfaces;

/// <summary>
/// Interface for building prompts for LLM services
/// </summary>
public interface IProjectInitializerPromptBuilder
{
    /// <summary>
    /// Builds the prompt for generating initialization commands
    /// </summary>
    /// <param name="blueprint">The architecture blueprint</param>
    /// <param name="projectStructure">The project structure to create</param>
    /// <returns>Prompt string for the LLamaService</returns>
    string BuildInitializationPrompt(ArchitectureBlueprint blueprint, ProjectStructure projectStructure);
}