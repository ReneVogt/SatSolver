using System.Runtime.CompilerServices;

namespace Revo.SatSolver.DataStructures;

sealed class Constraint
{
    public ConstraintLiteral[] Literals { get; }
    public ConstraintLiteral Watched1 { get; set; }
    public ConstraintLiteral Watched2 { get; set; }

    public int LiteralBlockDistance { get; init; }
    public double Activity { get; set; }
    public bool IsTracked { get; set; }

    public bool IsLearned { get; }
    public bool IsOmitted { get; }

    /// <summary>
    /// This constructor is used by the initial constraint creation.
    /// It sets and connects the watchers to the first literals, not regarding
    /// decision levels as they should be zero.
    /// </summary>
    /// <param name="literals">The <see cref="ConstraintLiteral"/>s contained in this constraint.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint(IEnumerable<ConstraintLiteral> literals)
    {
        Literals = [.. literals];
        Watched1 = Literals[0];
        Watched2 = Literals.Length > 1 ? Literals[1] : Watched1;
        Watched1.Watchers.Add(this);
        if (Watched2 != Watched1) Watched2.Watchers.Add(this);
    }

    /// <summary>
    /// This constructor is used for creating a constraint from a found solution,
    /// to force finding further solutions.
    /// </summary>
    /// <param name="literals">The <see cref="ConstraintLiteral"/>s contained in this constraint.</param>
    /// <param name="firstWatcher">The first watched literal (end of trail).</param>
    /// <param name="secondWatcher">The second watched literal (second last in trail).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint(IEnumerable<ConstraintLiteral> literals, ConstraintLiteral firstWatcher, ConstraintLiteral secondWatcher)
    {
        Literals = [.. literals];
        IsLearned = true;
        Watched1 = firstWatcher;
        Watched2 = secondWatcher;
        Watched1.Watchers.Add(this);
        if (Watched2 != Watched1) Watched2.Watchers.Add(this);
    }
    /// <summary>
    /// This constructor is used for creating a learned constraint.
    /// The watchers are not wired up here, because the constraint may
    /// be totally ommitted if the literal block distance is too high.
    /// </summary>
    /// <param name="literals">The <see cref="ConstraintLiteral"/>s contained in this constraint.</param>
    /// <param name="firstWatcher">The first watched literal (end of trail).</param>
    /// <param name="secondWatcher">The second watched literal (second last in trail).</param>
    /// <param name="activity">Initial activity of this constraint.</param>
    /// <param name="literalBlockDistance">Literal block distance of this learned constraint.</param>
    /// <param name="tracked"><c>true</c> if this constraint is tracked for constrain deletion.</param>
    /// <param name="omitted"><c>true</c> if this constraint is only used for jumping back, but no longer useful.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Constraint(ConstraintLiteral[] literals, ConstraintLiteral firstWatcher, ConstraintLiteral secondWatcher, double activity, int literalBlockDistance, bool tracked, bool omitted)
    {
        Literals = literals;
        IsLearned = true;
        IsTracked = tracked;
        IsOmitted = omitted;
        Activity = activity;
        LiteralBlockDistance = literalBlockDistance;
        Watched1 = firstWatcher;
        Watched1 = firstWatcher;
        Watched2 = secondWatcher;
        if (omitted) return;       
        Watched1.Watchers.Add(this);
        if (Watched2 != Watched1) Watched2.Watchers.Add(this);
    }

    public override string ToString() => string.Join(" ", Literals.Select(l => $"{(l.Orientation ? "" : "-")}{l.Variable.Index+1}"));
}
