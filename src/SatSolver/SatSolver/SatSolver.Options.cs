using Revo.SatSolver.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    /// <summary>
    /// Configures how the <see cref="SatSolver"/> decides to
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
        /// If not <c>null</c>, the solver will restart if the ratio
        /// of the recent literal block distances found in learned
        /// clauses to the over all literal block distance average
        /// is greater than this threshold.
        /// To use this threshold, <see cref="Options.LiteralBlockDistanceTracking"/> 
        /// must be configured.
        /// </summary>
        public double? LiteralBlockDistanceThreshold { get; init; }
        /// <summary>
        /// If not <c>null</c>, the solver will restart if the ratio
        /// of the recent propagation rates (propagations per conflict)
        /// to the over all propagation rate average is smaller than 
        /// this threshold.
        /// To use this threshold, <see cref="Options.PropagationRateTracking"/> 
        /// must be configured.
        /// </summary>
        public double? PropagationRateThreshold { get; init; }
    }

    /// <summary>
    /// Configures when and how learned clauses will
    /// be deleted.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record ClauseDeletionOptions
    {
        /// <summary>
        /// Clauses with literal block distances less than
        /// or euqal to this value will be kept alive forever.
        /// </summary>
        public int LiteralBlockDistanceToKeep { get; init; } = 2;              
        /// <summary>
        /// The ratio of those clauses that are not saved by
        /// the <see cref="LiteralBlockDistanceToKeep"/> that
        /// will be deleted during a clause deletion.
        /// The clauses are ordered by their activiy so that
        /// only the useless part will be deleted.
        /// </summary>
        public double RatioToDelete { get; init; } = 0.5;
        /// <summary>
        /// If not <c>null</c>, a clause deletion will be
        /// performed if the number of learned clauses
        /// exceeds the number of original clauses multiplied
        /// by this value.
        /// </summary>
        public double? OriginalClauseCountFactor { get; init; } = 5d;
        /// <summary>
        /// If not <c>null</c>, a clause deletion will be
        /// performed when the ratio of the recent literal 
        /// block distances found in learned clauses to the 
        /// over all literal block distance average is 
        /// greater than this threshold.
        /// To use this threshold, <see cref="Options.LiteralBlockDistanceTracking"/> 
        /// must be configured.
        /// </summary>
        public double? LiteralBlockDistanceThreshold { get; init; }
        /// <summary>
        /// If not <c>null</c>, the solver will perform a clause
        /// deletion if the ratio of the recent propagation rates 
        /// (propagations per conflict) to the over all propagation 
        /// rate average is smaller than this threshold.
        /// To use this threshold, <see cref="Options.PropagationRateTracking"/> 
        /// must be configured.
        /// </summary>
        public double? PropagationRateThreshold { get; init; }
    }

    /// <summary>
    /// Configures how an exponential moving average of
    /// a value is tracked. This used for <see cref="Options.LiteralBlockDistanceTracking"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record EmaOptions
    {
        /// <summary>
        /// The number of values counted as "recent".
        /// </summary>
        public int RecentCount { get; init; } = 100;

        /// <summary>
        /// The decay value for the exponential moving average.
        /// </summary>
        public double Decay { get; init; } = 0.999d;
    }

    /// <summary>
    /// Configures how propagation rate will be tracked.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record PropagationRateTrackingOptions
    {
        /// <summary>
        /// The number of propagation rates counted as "recent".
        /// </summary>
        public int SampleSize { get; init; } = 100;

        /// <summary>
        /// The decay value for the exponential 
        /// moving average of propagation rates..
        /// </summary>
        public double Decay { get; init; } = 0.999d;

        /// <summary>
        /// The number of conflicts for which the propagations
        /// should be counted to get a propagation rate.
        /// </summary>
        public int ConflictInterval { get; init; } = 100;
    }

    /// <summary>
    /// Configures the details of how the <see cref="SatSolver"/>
    /// performs its search.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public record Options
    {
        /// <summary>
        /// The recommended default options set
        /// using CDCL with a restart strategy
        /// based on a Luby sequence with base 100
        /// and depending on literal block distance
        /// average and propagation rate.
        /// </summary>
        public static Options Default { get; } = new();

        /// <summary>
        /// Options for a poor man's VSIDS solver without
        /// any restarts or other fancy strategies.
        /// </summary>
        public static Options PoorMansVSIDS { get; } = new ()
        {
            OnlyPoorMansVSIDS = true,
            VariableActivityDecayFactor = 0.9995,
            Restart = new()
            {
                Interval = null,
                LiteralBlockDistanceThreshold = null,
                Luby = false,
                PropagationRateThreshold = null
            },
            ClauseDeletion = new()
            {
                LiteralBlockDistanceThreshold = null,
                LiteralBlockDistanceToKeep = 0,
                OriginalClauseCountFactor = null,
                PropagationRateThreshold = null,
                RatioToDelete = 0
            }
        };
        
        /// <summary>
        /// The recommended default options set
        /// using CDCL with a restart strategy
        /// based on a Luby sequence with base 100
        /// and depending on literal block distance
        /// average and propagation rate.
        /// </summary>
        public static Options CDCL { get; } = Default;

        /// <summary>
        /// If this is <c>true</c>, no clause learning will
        /// be performed. Only the activites of variables 
        /// found in a conflicting clause will be increased.
        /// </summary>
        public bool OnlyPoorMansVSIDS { get; init; }

        /// <summary>
        /// The activites of variables are incremented when a
        /// they are part of a learned clause and decayed by
        /// this factor after each conflict.
        /// If <see cref="OnlyPoorMansVSIDS"/> is <c>true</c>,
        /// the activities of all variables in a conflicting
        /// clause are incremented.
        /// </summary>
        public double VariableActivityDecayFactor { get; init; } = 0.999;
        /// <summary>
        /// The activites of learned clauses are incremented 
        /// when they are created, found in the reasons for
        /// a conflicting clause or (with half the increment)
        /// when they lead to a unit propagation:
        /// They are decayed after each conflict by this factor.
        /// </summary>
        public double ClauseActivityDecayFactor { get; init; } = 0.999;

        /// <summary>
        /// Learned clauses with a literal block distance greater
        /// than this value will be deleted immediatly, The propagation
        /// of their unique implication point will be performed as
        /// well as activity updates, but they will not be part
        /// of the watcher structure and not counted for the
        /// average literal block distance.
        /// </summary>
        public int MaximumLiteralBlockDistance { get; init; } = 8;

        /// <summary>
        /// The maximum recursion depth for clause minimization.
        /// </summary>
        public int MaximumClauseMinimizationDepth { get; init; } = 9;

        /// <summary>
        /// Configures when and how learned clauses will
        /// be deleted.
        /// </summary>
        public ClauseDeletionOptions ClauseDeletion { get; init; } = new();

        /// <summary>
        /// Configures how the <see cref="SatSolver"/> decides to restart
        /// its search.
        /// </summary>
        public RestartOptions Restart { get; init; } = new ();

        /// <summary>
        /// Configures how the literal block distances are tracked.
        /// </summary>
        public EmaOptions LiteralBlockDistanceTracking { get; init; } = new ();

        /// <summary>
        /// Configures how the propagation rate is tracked.
        /// </summary>
        public PropagationRateTrackingOptions PropagationRateTracking { get; init; } = new();
    }
}
