using Revo.SatSolver.DataStructures;
using System.Diagnostics;

namespace Revo.SatSolver.CDCL;

sealed class ConstraintMinimizer(SatSolver.Options _options) : IMinimizeConstraints
{
    readonly int _maxDepth = _options.MaximumClauseMinimizationDepth;
    public void MinimizeConstraint(HashSet<ConstraintLiteral> literals, int decisionLevel)
    {
        Debug.WriteLine($"Minimizing constraint ({literals.Count}).");
        literals.RemoveWhere(l => l.Variable.DecisionLevel != decisionLevel && IsRedundant(l, 0));
        Debug.WriteLine($"Minimized constraint to {literals.Count}.");
        bool IsRedundant(ConstraintLiteral literal, int depth)
        {
            if (depth > _maxDepth) return false;
            var reason = literal.Variable.Reason;
            if (reason is null) return false;
            var ignoreLiteral = literal.Orientation ? literal.Variable.NegativeLiteral : literal.Variable.PositiveLiteral;
            return reason.Literals.All(r =>
                ignoreLiteral == literal ||
                literals.Contains(r) ||
                IsRedundant(r, depth+1));
        }
    }
}
