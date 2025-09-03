using Revo.SatSolver.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Revo.SatSolver;

/// <summary>
/// Configures the details of how the <see cref="SatSolverFactory"/>
/// performs its search.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record SatSolverOptions
{
    /// <summary>
    /// Configures how the <see cref="SatSolverFactory"/> decides to
    /// restart its search.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record RestartOptions
    {
        /// <summary>
        /// If not <c>null</c>, determines after how many
        /// conflicts the solver restarts.
        /// If <see cref="Luby"/> is <c>true</c>, this value
        /// is multiplied by the Luby sequence (<see cref="LubySequence"/>).
        /// </summary>
        public int? Interval { get; init; }
        /// <summary>
        /// Determines if the restartl <see cref="Interval"/> is
        /// multiplied by the Luby sequence (<see cref="LubySequence"/>.
        /// </summary>
        public bool Luby { get; init; }
        /// <summary>
        /// <c>true</c> if restarts should be triggered by the literal block
        /// distance development. Set the <see cref="LiteralBlockDistanceTracking"/>
        /// options to configure this trigger.
        /// </summary>
        public bool ByLiteralBlockDistance { get; init; } = true;
        /// <summary>
        /// <c>true</c> if restarts should be triggered by the propagation rate
        /// development. Set the <see cref="PropagationRateTracking"/>
        /// options to configure this trigger.
        /// </summary>
        public bool ByPropagationRate { get; init; } = true;
    }

    /// <summary>
    /// Configures when and how learned constraints will
    /// be deleted.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record ConstraintDeletionOptions
    {
        /// <summary>
        /// Constraints with literal block distances less than
        /// or euqal to this value will be kept alive forever.
        /// </summary>
        public int LiteralBlockDistanceToKeep { get; init; } = 2;
        /// <summary>
        /// The ratio of those constraints that are not saved by
        /// the <see cref="LiteralBlockDistanceToKeep"/> that
        /// will be deleted during a constraint deletion.
        /// The constraints are ordered by their activiy so that
        /// only the useless part will be deleted.
        /// </summary>
        public double RatioToDelete { get; init; } = 0.65d;
        /// <summary>
        /// If not <c>null</c>, a constraint deletion will be
        /// performed if the number of learned constraints
        /// exceeds the number of original constraints multiplied
        /// by this value.
        /// </summary>
        public double? OriginalConstraintCountFactor { get; init; } = 4d;
        /// <summary>
        /// If not null, the number of conflicts after which the learnt
        /// clause database is autmatically reduced.
        /// </summary>
        public int? ConflictInterval { get; init; } = 5000;
        /// <summary>
        /// Set to <c>true</c> if learned constraints should be
        /// reduced when the algorithm is restarted during execution.
        /// </summary>
        public bool ReduceOnRestart { get; init; } = true;
    }

    /// <summary>
    /// Configures how values can be tracked during solves.
    /// This used for <see cref="PropagationRateTracking"/>
    /// and <see cref="LiteralBlockDistanceTracking"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record ValueTrackingOptions
    {
        /// <summary>
        /// The halflife conflict count for the long
        /// term exponential moving average of the
        /// propagation rate.
        /// </summary>
        public int GlobalHalflife { get; init; } = 300;

        /// <summary>
        /// The halflife conflict count for the short
        /// term expnential moving average of the
        /// propagation rate.
        /// </summary>
        public int LocalHalflife { get; init; } = 40;

        /// <summary>
        /// The threshold for the ratio of short term
        /// to long term propagation rate. If the ratio
        /// is below (or above, depending on the usage)
        /// this threshold for <see cref="HoldForConflicts"/> 
        /// conflicts, a restart is indicated.
        /// </summary>
        public double Threshold { get; init; } = 0.75d;

        /// <summary>
        /// The number of consecutive conflicts with
        /// a ratio from short term to long term propagation rate
        /// below (or above, depending on the usage) the 
        /// <see cref="Threshold"/> required to indicate
        /// a restart.
        /// </summary>
        public int HoldForConflicts { get; init; } = 12;

        /// <summary>
        /// The number of conflicts after a restart before
        /// a new restart can be indicated.
        /// </summary>
        public int CoolDownConflicts { get; init; } = 200;
    }

    /// <summary>
    /// The recommended default options set
    /// using CDCL with a restart strategy
    /// based on a Luby sequence with base 100
    /// and depending on literal block distance
    /// average and propagation rate.
    /// </summary>
    public static SatSolverOptions Default { get; } = new();

    /// <summary>
    /// Options for a poor man's VSIDS solver without
    /// any restarts or other fancy strategies.
    /// </summary>
    public static SatSolverOptions DPLL { get; } = new ()
    {
        Mode = SatSolverMode.DPLL,
        VariableActivityDecayFactor = 0.9995,
        Restart = new()
        {
            Interval = null,
            Luby = false,
            ByLiteralBlockDistance = false,
            ByPropagationRate = false
        },
        ConstraintDeletion = new()
        {
            LiteralBlockDistanceToKeep = 0,
            OriginalConstraintCountFactor = null,
            ConflictInterval = null,
            RatioToDelete = 0
        }
    };
        
    /// <summary>
    /// The recommended default options set
    /// using CDCL with a restart strategy
    /// depending on literal block distance
    /// average and propagation rate.
    /// </summary>
    public static SatSolverOptions CDCL { get; } = new();

    /// <summary>
    /// The recommended options set for 
    /// solving the sudoku cnf using CDCL 
    /// without restarts and a maximum 
    /// literal block distance of 10.
    /// </summary>
    public static SatSolverOptions Sudoku { get; } = CDCL with
    {
        Restart = new()
        {
            Interval = null,
            Luby = false,
            ByLiteralBlockDistance = false,
            ByPropagationRate = false
        },
        MaximumLiteralBlockDistance = 10
    };


    /// <summary>
    /// Defines the <see cref="SatSolverMode"/> (<see cref="SatSolverMode.CDCL"/>
    /// or <see cref="SatSolverMode.DPLL"/> to use.
    /// </summary>
    public SatSolverMode Mode { get; init; } = SatSolverMode.CDCL;

    /// <summary>
    /// The activites of variables are incremented when a
    /// they are part of a learned constraint and decayed by
    /// this factor after each conflict.
    /// If <see cref="OnlyDpll"/> is <c>true</c>,
    /// the activities of all variables in a conflicting
    /// constraint are incremented.
    /// </summary>
    public double VariableActivityDecayFactor { get; init; } = 0.95;
    /// <summary>
    /// The activites of learned constraints are incremented 
    /// when they are created, found in the reasons for
    /// a conflicting constraint or (with half the increment)
    /// when they lead to a unit propagation:
    /// They are decayed after each conflict by this factor.
    /// </summary>
    public double ConstraintActivityDecayFactor { get; init; } = 0.999;

    /// <summary>
    /// Learned constraints with a literal block distance greater
    /// than this value will be deleted immediatly, The propagation
    /// of their unique implication point will be performed as
    /// well as activity updates, but they will not be part
    /// of the watcher structure and not counted for the
    /// average literal block distance.
    /// </summary>
    public int MaximumLiteralBlockDistance { get; init; } = 8;

    /// <summary>
    /// Configures when and how learned constraints will
    /// be deleted.
    /// </summary>
    public ConstraintDeletionOptions ConstraintDeletion { get; init; } = new();

    /// <summary>
    /// Configures how the <see cref="SatSolverFactory"/> decides to restart
    /// its search.
    /// </summary>
    public RestartOptions Restart { get; init; } = new ();

    /// <summary>
    /// Configures how the propagation rate is tracked and how it indicates restarts.
    /// </summary>
    public ValueTrackingOptions PropagationRateTracking { get; init; } = new()
    {
        CoolDownConflicts = 200,
        HoldForConflicts = 12,
        GlobalHalflife = 300,
        LocalHalflife = 40,
        Threshold = 0.7
    };

    /// <summary>
    /// Configures how the literal block distances are tracked.
    /// </summary>
    public ValueTrackingOptions LiteralBlockDistanceTracking { get; init; } = new()
    {
        CoolDownConflicts = 200,
        HoldForConflicts = 12,
        GlobalHalflife = 300,
        LocalHalflife = 40,
        Threshold = 1.3
    };

    internal void Validate()
    {
        var builder = new StringBuilder();
        if (VariableActivityDecayFactor == 0 ||
            ConstraintActivityDecayFactor == 0)
            builder.AppendLine("Decay factors must not be zero.");

        if (PropagationRateTracking.LocalHalflife == 0 ||
            PropagationRateTracking.GlobalHalflife == 0 ||
            LiteralBlockDistanceTracking.LocalHalflife == 0 ||
            LiteralBlockDistanceTracking.GlobalHalflife == 0)
            builder.AppendLine("Halflife values must not be zero.");

        if (builder.Length > 0)
            throw new ArgumentException(
                paramName: nameof(SatSolverOptions),
                message: builder.ToString());
    }
}
