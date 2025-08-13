using Revo.SatSolver;

namespace SatSolverBenchmark;
static class TestOptions
{
    public static SatSolver.Options Default { get; } = new SatSolver.Options()
    {
        OnlyPoorMansVSIDS = true,

        VariableActivityDecayFactor = 0.9995,

        ClauseActivityDecayFactor = 0.999,
        MaximumLiteralBlockDistance = 25,
        MaximumClauseMinimizationDepth = 2,

        ClauseDeletion = new()
        {
            LiteralBlockDistanceToKeep = 2,
            OriginalClauseCountFactor = 5,
            RatioToDelete = 0.5,
            LiteralBlockDistanceThreshold = 1.3,
            PropagationRateThreshold = 0.5
        },

        Restart = new()
        {
            Interval = null,
            Luby = false,
            LiteralBlockDistanceThreshold = null,
            PropagationRateThreshold = null
        },

        LiteralBlockDistanceTracking = new()
        {
            Decay = 0.999,
            RecentCount = 100
        },

        PropagationRateTracking = new()
        {
            ConflictInterval = 500,
            Decay = 0.999,
            SampleSize = 50
        }
    };
}
