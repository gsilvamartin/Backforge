using Backforge.Core.Models;
using Backforge.Core.Services.Interfaces;

namespace Backforge.Core.Services;

public class DecisionEngine: IDecisionEngine
{
    public Task<DecisionPoint> MakeDecisionAsync(string decisionContext, List<string> options)
    {
        throw new NotImplementedException();
    }

    public Task<string> ExplainReasoningAsync(DecisionPoint decision)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ValidateDecisionAsync(DecisionPoint decision, string expectedOutcome)
    {
        throw new NotImplementedException();
    }

    public Task<DecisionPoint> RefineDecisionAsync(DecisionPoint previousDecision, string feedback)
    {
        throw new NotImplementedException();
    }
}