using Backforge.Core.Models;

namespace Backforge.Core.Services.Interfaces;

public interface IDecisionEngine
{
    Task<DecisionPoint> MakeDecisionAsync(string decisionContext, List<string> options);
    Task<string> ExplainReasoningAsync(DecisionPoint decision);
    Task<bool> ValidateDecisionAsync(DecisionPoint decision, string expectedOutcome);
    Task<DecisionPoint> RefineDecisionAsync(DecisionPoint previousDecision, string feedback);
}