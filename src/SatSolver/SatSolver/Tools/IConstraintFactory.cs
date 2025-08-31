using Revo.SatSolver.DataStructures;

namespace Revo.SatSolver.Tools;

interface IConstraintFactory
{
    /// <summary>
    /// Used by the initial constraint creation.
    /// It sets and connects the watchers to the first literals, not regarding
    /// decision levels as they should be zero.
    /// </summary>
    /// <param name="literals">The <see cref="ConstraintLiteral"/>s contained in this constraint.</param>
    Constraint CreateInitialConstraint(IEnumerable<ConstraintLiteral> literals);

    /// <summary>
    /// Used for creating a constraint from a found solution 
    /// to force finding further solutions.or for additional 
    /// constraints added via <see cref="ISatSolver.AddClause(Clause)"/>,
    /// The watchers will be set to the unassigned literals or the
    /// literals with the highest decision levels.
    /// </summary>
    Constraint CreateAdditionalConstraint(IEnumerable<ConstraintLiteral> literals);

    /// <summary>
    /// Used for creating a learned constraint.
    /// The watchers are not wired up here, because the constraint may
    /// be totally ommitted if the literal block distance is too high.
    /// </summary>
    /// <param name="literals">The <see cref="ConstraintLiteral"/>s contained in this constraint.</param>
    /// <param name="decisionLevel">The current decision level.</param>
    /// <param name="activity">Initial activity of this constraint.</param>
    /// <param name="maximumLiteralBlockDistance">The maximum literal block distance for constraints to be kept alive.</param>
    /// <param name="literalBlockDistanceDeletionLimit">The literal block distance limit for permanently learned constraints.</param>
    /// <param name="jumpBackLevel">The decision level we can jump back to with the created constraint.</param>
    Constraint CreateLearnedConstraint(ConstraintLiteral[] learnedLiterals, int decisionLevel, double activity, int maximumLiteralBlockDistance, int literalBlockDistanceDeletionLimit, out int jumpBackLevel);

    void ReleaseConstraint(Constraint constraint);

    void ReleaseLearnedConstraints(double ratio);
}
