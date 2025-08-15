using Revo.SatSolver.CDCL;
using Revo.SatSolver.DataStructures;
using SatSolverTests.Stubs;

namespace SatSolverTests.CDCL;

public sealed class LearnedConstraintCreatorTests
{
    [Fact]
    public void ConflictExample1()
    {
        /*
         * 
         * This test case is taken from the example 
         * shown here: https://users.aalto.fi/~tjunttil/2020-DP-AUT/notes-sat/cdcl.html
         * 
         */
      
        var variables = Enumerable.Range(0, 12).Select(i => new Variable(i)).ToArray();
        var trail = new VariableTrail(12, new TestCandidateHeap());
        var activityManager = new TestActivityManager();
        var constraintMinimizer = new TestConstraintMinimizer();
        
        var sut = new LearnedConstraintCreator(trail, activityManager, constraintMinimizer, variables);

        // introduce variables for easy comparison to the diagram
        var x1 = variables[0];
        var x2 = variables[1];
        var x3 = variables[2];
        var x4 = variables[3];
        var x5 = variables[4];
        var x6 = variables[5];  
        var x7 = variables[6];
        var x8 = variables[7];
        var x9 = variables[8];
        var x10 = variables[9];
        var x11 = variables[10];
        var x12 = variables[11];

        trail.Push();
        trail.Add(x1);
        x1.Sense = true;
        var c1 = new Constraint([
            x1.NegativeLiteral,
            x2.NegativeLiteral
            ]);
        x2.Sense = false;
        trail.Add(x2);
        x2.Reason = c1;

        var c2 = new Constraint([
            x1.NegativeLiteral,
            x3.PositiveLiteral
            ]);
        x3.Sense = true;
        trail.Add(x3);
        x3.Reason = c2;

        var c3 = new Constraint([
            x3.NegativeLiteral,
            x4.NegativeLiteral
            ]);
        x4.Sense = false;
        trail.Add(x4);
        x4.Reason = c3;

        var c4 = new Constraint([
            x2.PositiveLiteral,
            x4.PositiveLiteral,
            x5.PositiveLiteral,
            ]);
        x5.Sense = true;
        x5.Reason = c4;
        trail.Add(x5);

        trail.Push();
        x6.Sense = false;
        trail.Add(x6);

        var c5 = new Constraint([
            x5.NegativeLiteral,
            x6.PositiveLiteral,
            x7.NegativeLiteral,
            ]);
        x7.Sense = false;
        x7.Reason = c5;
        trail.Add(x7);

        var c6 = new Constraint([
            x2.PositiveLiteral,
            x7.PositiveLiteral,
            x8.PositiveLiteral,
            ]);
        x8.Sense = true;
        x8.Reason = c6;
        trail.Add(x8);

        var c7 = new Constraint([
            x8.NegativeLiteral,
            x9.NegativeLiteral,
            ]);
        x9.Sense = false;
        x9.Reason = c7;
        trail.Add(x9);

        var c8 = new Constraint([
            x8.NegativeLiteral,
            x10.PositiveLiteral,
            ]);
        x10.Sense = true;
        x10.Reason = c8;
        trail.Add(x10);

        var c9 = new Constraint([
            x9.PositiveLiteral,
            x10.NegativeLiteral,
            x11.PositiveLiteral
            ]);
        x11.Sense = true;
        x11.Reason = c9;
        trail.Add(x11);

        var c10 = new Constraint([
            x10.NegativeLiteral,
            x12.NegativeLiteral,
            ]);
        x12.Sense = false;
        x12.Reason = c10;
        trail.Add(x12);

        var conflictingConstraint = new Constraint([
            x11.NegativeLiteral,
            x12.PositiveLiteral
            ]);

        var learnedConstraint = sut.CreateLearnedConstraint(conflictingConstraint, out var uip, out var jumpBackLevel);

        Assert.Equal(0, jumpBackLevel);
        Assert.Equal(x8.NegativeLiteral, uip);
        Assert.Equal([x8.NegativeLiteral], learnedConstraint.Literals);
    }
}
