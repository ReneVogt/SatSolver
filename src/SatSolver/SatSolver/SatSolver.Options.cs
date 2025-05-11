namespace Revo.SatSolver;

public sealed partial class SatSolver
{
    /// <summary>
    /// Configures how the <see cref="SatSolver"/> decides to
    /// restart its search.
    /// </summary>
    public class RestartOptions
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
    }

    /// <summary>
    /// Configures when and how learned clauses will
    /// be deleted.
    /// </summary>
    public class ClauseDeletionOptions
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
        public double? OriginalClauseCountFactor { get; init; }
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
    }

    /// <summary>
    /// Configures how an exponential moving average of
    /// a value is tracked. This used for <see cref="Options.LiteralBlockDistanceTracking"/>.
    /// </summary>
    public class EmaOptions
    {
        /// <summary>
        /// The number of values counted as "recent".
        /// </summary>
        public int RecentCount { get; init; }

        /// <summary>
        /// The decay value for the exponential moving average.
        /// </summary>
        public double Decay { get; init; }
    }

    /// <summary>
    /// Configures the details of how the <see cref="SatSolver"/>
    /// performs its search.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// A recommended default options set.
        /// </summary>
        public static Options Default { get; } = new();

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
        public double VariableActivityDecayFactor { get; init; } = 0.95;
        /// <summary>
        /// The activites of learned clauses are incremented 
        /// when they are created, found in the reasons for
        /// a conflicting clause or (with half the increment)
        /// when they lead to a unit propagation:
        /// They are decayed after each conflict by this factor.
        /// </summary>
        public double ClauseActivityDecayFactor { get; init; } = 0.99;

        /// <summary>
        /// Learned clauses with a literal block distance greater
        /// than this value will deleted immediatly, The propagation
        /// of their unique implication point will be performed as
        /// well as activity updates, but they will not be part
        /// of the watcher structure and not counted for the
        /// average literal block distance.
        /// </summary>
        public int LiteralBlockDistanceMaximum { get; init; } = 8;

        /// <summary>
        /// Configures when and how learned clauses will
        /// be deleted.
        /// </summary>
        public ClauseDeletionOptions ClauseDeletionOptions { get; init; } = new();

        /// <summary>
        /// Configures how the <see cref="SatSolver"/> decides to restart
        /// its search.
        /// </summary>
        public RestartOptions RestartOptions { get; init; } = new ();

        public EmaOptions? LiteralBlockDistanceTracking { get; init; } = new ();
    }
}
