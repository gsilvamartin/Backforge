namespace Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;

public interface IEntityRelationshipExtractor
{
    Task<List<string>> ExtractEntitiesAsync(string requirementText, CancellationToken cancellationToken);
    Task<List<string>> ExtractRelationshipsAsync(string requirementText, CancellationToken cancellationToken);
}