using Revo.SatSolver;

namespace SatSolverTests;

public sealed class SatSolverOptionsTests
{
    [Theory]
    [MemberData(nameof(ProvideOptionTests))]
    public void ValidationTests(SatSolverOptions options, string[]? expectedMessages)
    {
        if (expectedMessages is null)
        {
            options.Validate();
            return;
        }

        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Equal(nameof(SatSolverOptions), exception.ParamName);
        Assert.Equal(expectedMessages.OrderBy(m => m), exception.Message.Split(Environment.NewLine).SkipLast(1).OrderBy(s => s));
    }

    public static TheoryData<SatSolverOptions, string[]?> ProvideOptionTests() =>
        new TheoryData<SatSolverOptions, string[]?>
        {
            {SatSolverOptions.Default, null},
            {SatSolverOptions.DPLL, null},
            {SatSolverOptions.CDCL, null},

            {new () { ConstraintActivityDecayFactor = 0 },
            ["Decay factors must not be zero."] },
            
            {new () { VariableActivityDecayFactor = 0 },
            ["Decay factors must not be zero."] },
            
            {
                new () 
                {
                    ConstraintActivityDecayFactor = 0,
                    VariableActivityDecayFactor = 0 
                },
                ["Decay factors must not be zero."] 
            },
            
            {
                new () 
                {
                    PropagationRateTracking = new()
                    {
                        GlobalHalflife = 0
                    }
                },
                ["Halflife values must not be zero."] 
            },
            {
                new () 
                {
                    PropagationRateTracking = new()
                    {
                        LocalHalflife = 0
                    }
                },
                ["Halflife values must not be zero."]
            },
            {
                new () 
                {
                    LiteralBlockDistanceTracking = new()
                    {
                        GlobalHalflife = 0
                    }
                },
                ["Halflife values must not be zero."]
            },
            {
                new () 
                {
                    LiteralBlockDistanceTracking = new()
                    {
                        LocalHalflife = 0
                    }
                },
                ["Halflife values must not be zero."] 
            },
            {
                new () 
                {
                    PropagationRateTracking = new()
                    {
                        GlobalHalflife = 0,
                        LocalHalflife = 0
                    },
                    LiteralBlockDistanceTracking = new()
                    {
                        GlobalHalflife = 0,
                        LocalHalflife = 0
                    }
                },
                ["Halflife values must not be zero."] 
            },
            {
                new ()
                {
                    ConstraintActivityDecayFactor = 0,
                    VariableActivityDecayFactor = 0,
                    PropagationRateTracking = new()
                    {
                        GlobalHalflife = 0,
                        LocalHalflife = 0
                    },
                    LiteralBlockDistanceTracking = new()
                    {
                        GlobalHalflife = 0,
                        LocalHalflife = 0
                    }
                },
                [
                    "Decay factors must not be zero.",
                    "Halflife values must not be zero."
                ]
            },
        };   
}
