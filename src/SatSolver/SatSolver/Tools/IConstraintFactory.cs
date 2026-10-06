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
    /// Creates additional constraints, including clauses blocking found solutions.
    /// Prefers non-false watchers; false watchers are chosen at the highest
    /// decision levels. The caller must handle conflicts and establish unit
    /// implications at the required level, including when the only true literal
    /// is assigned above all false literals.
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

    void ReleaseAdditionalConstraints();
}
