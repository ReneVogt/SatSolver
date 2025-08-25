using Moq;
using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Processors;
using Revo.SatSolver.Tools;

namespace SatSolverTests.Processors;

public sealed class LearnedConstraintCreatorTests
{
    [Fact]
    public void FourStateSudoku_SettingAll1True()
    {
        var variables = Enumerable.Range(0, 16).Select(i => new Variable(i)).ToArray();
        var trail = new VariableTrail(new CandidateHeap(variables), 16);

        Variable v11 = variables[0], v12 = variables[1], v13 = variables[2], v14 = variables[3],
            v21 = variables[4], v22 = variables[5], v23 = variables[6], v24 = variables[7],
            v31 = variables[8], v32 = variables[9], v33 = variables[10], v34 = variables[11],
            v41 = variables[12], v42 = variables[13], v43 = variables[14], v44 = variables[15];

        var c1 = new Constraint([v11.PositiveLiteral, v12.PositiveLiteral, v13.PositiveLiteral, v14.PositiveLiteral]);
        var c112 = new Constraint([v11.NegativeLiteral, v12.NegativeLiteral]);
        var c113 = new Constraint([v11.NegativeLiteral, v13.NegativeLiteral]);
        var c114 = new Constraint([v11.NegativeLiteral, v14.NegativeLiteral]);
        var c123 = new Constraint([v12.NegativeLiteral, v13.NegativeLiteral]);
        var c124 = new Constraint([v12.NegativeLiteral, v14.NegativeLiteral]);
        var c134 = new Constraint([v13.NegativeLiteral, v14.NegativeLiteral]);

        var c2 = new Constraint([v21.PositiveLiteral, v22.PositiveLiteral, v23.PositiveLiteral, v24.PositiveLiteral]);
        var c212 = new Constraint([v21.NegativeLiteral, v22.NegativeLiteral]);
        var c213 = new Constraint([v21.NegativeLiteral, v23.NegativeLiteral]);
        var c214 = new Constraint([v21.NegativeLiteral, v24.NegativeLiteral]);
        var c223 = new Constraint([v22.NegativeLiteral, v23.NegativeLiteral]);
        var c224 = new Constraint([v22.NegativeLiteral, v24.NegativeLiteral]);
        var c234 = new Constraint([v23.NegativeLiteral, v24.NegativeLiteral]);

        var c3 = new Constraint([v31.PositiveLiteral, v32.PositiveLiteral, v33.PositiveLiteral, v34.PositiveLiteral]);
        var c312 = new Constraint([v31.NegativeLiteral, v32.NegativeLiteral]);
        var c313 = new Constraint([v31.NegativeLiteral, v33.NegativeLiteral]);
        var c314 = new Constraint([v31.NegativeLiteral, v34.NegativeLiteral]);
        var c323 = new Constraint([v32.NegativeLiteral, v33.NegativeLiteral]);
        var c324 = new Constraint([v32.NegativeLiteral, v34.NegativeLiteral]);
        var c334 = new Constraint([v33.NegativeLiteral, v34.NegativeLiteral]);

        var c4 = new Constraint([v41.PositiveLiteral, v42.PositiveLiteral, v43.PositiveLiteral, v44.PositiveLiteral]);
        var c411 = new Constraint([v41.NegativeLiteral, v42.NegativeLiteral]);
        var c412 = new Constraint([v41.NegativeLiteral, v43.NegativeLiteral]);
        var c413 = new Constraint([v41.NegativeLiteral, v44.NegativeLiteral]);
        var c423 = new Constraint([v42.NegativeLiteral, v43.NegativeLiteral]);
        var c424 = new Constraint([v42.NegativeLiteral, v44.NegativeLiteral]);
        var c434 = new Constraint([v43.NegativeLiteral, v44.NegativeLiteral]);

        var ca1 = new Constraint([v11.PositiveLiteral, v21.PositiveLiteral, v31.PositiveLiteral, v41.PositiveLiteral]);
        var ca2 = new Constraint([v12.PositiveLiteral, v22.PositiveLiteral, v32.PositiveLiteral, v42.PositiveLiteral]);
        var ca3 = new Constraint([v13.PositiveLiteral, v23.PositiveLiteral, v33.PositiveLiteral, v43.PositiveLiteral]);
        var ca4 = new Constraint([v14.PositiveLiteral, v24.PositiveLiteral, v34.PositiveLiteral, v44.PositiveLiteral]);

        // set first 1 to true
        trail.Push();
        v11.Sense = true;
        trail.Add(v11);

        // units
        v12.Sense = v13.Sense = v14.Sense = false;
        v12.Reason = c112; v13.Reason = c113; v14.Reason = c114;
        trail.Add(v12); trail.Add(v13); trail.Add(v14);

        // set second 1 to true
        trail.Push();
        v21.Sense = true;
        trail.Add(v21);

        // units
        v22.Sense = v23.Sense = v24.Sense = false;
        v22.Reason = c212; v23.Reason = c213; v24.Reason = c214;
        trail.Add(v22); trail.Add(v23); trail.Add(v24);

        // set third 1 to true
        trail.Push();
        v31.Sense = true;
        trail.Add(v31);

        // units
        v32.Sense = v33.Sense = v34.Sense = false;
        v32.Reason = c312; v33.Reason = c313; v34.Reason = c314;
        trail.Add(v32); trail.Add(v33); trail.Add(v34);

        // now ca2 states that v42 must be true
        v42.Sense = true;
        v42.Reason = ca2;
        trail.Add(v42);

        // this falsifies v41
        v41.Sense = false;
        v41.Reason = c412;
        trail.Add(v41);

        // and v43
        v43.Sense = false;
        v43.Reason = c423;
        trail.Add(v43);

        //
        // and this leads to a confict in ca3
        //

        // fill the array to verifiy it is cleared by the creator.
        var learnedLiterals = new StampArray();
        foreach(var variable in variables)
        {
            learnedLiterals.Add(variable.PositiveLiteral.StampIndex);
            learnedLiterals.Add(variable.NegativeLiteral.StampIndex);
        }

        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var seq = new MockSequence();
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c423, 1));
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(ca2, 1));
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c313, 1));
        var sut = new LearnedConstraintCreator(trail, activityManager.Object);

        sut.CreateLearnedConstraint(ca3, learnedLiterals);
        Assert.Equal([
            v12.PositiveLiteral.StampIndex,
            v13.PositiveLiteral.StampIndex,
            v22.PositiveLiteral.StampIndex,
            v23.PositiveLiteral.StampIndex,
            v31.NegativeLiteral.StampIndex
            ], learnedLiterals);

        activityManager.VerifyAll();
    }
    [Fact]
    public void MadeUpExample1()
    {
        var variables = Enumerable.Range(0, 13).Select(i => new Variable(i)).ToArray();
        var trail = new VariableTrail(new CandidateHeap(variables), 16);

        Variable v1 = variables[0], v2 = variables[1], v3 = variables[2], v4 = variables[3],
            v5 = variables[4], v6 = variables[5], v7 = variables[6], v8 = variables[7],
            v9 = variables[8], v10 = variables[9], v11 = variables[10], v12 = variables[11],
            v13 = variables[12];

        // decide v1 to true
        trail.Push();
        v1.Sense = true;
        trail.Add(v1);
        // units
        var c1 = new Constraint([v1.NegativeLiteral, v2.PositiveLiteral]);
        var c2 = new Constraint([v1.NegativeLiteral, v3.NegativeLiteral]);
        v2.Sense = true; v2.Reason = c1; trail.Add(v2);
        v3.Sense = false; v3.Reason = c2; trail.Add(v3);

        // decide v4 to true
        trail.Push();
        v4.Sense = true;
        trail.Add(v4);
        // units
        var c3 = new Constraint([v4.NegativeLiteral, v5.PositiveLiteral]);
        v5.Sense = true; v5.Reason = c3; trail.Add(v5);
        var c4 = new Constraint([v4.NegativeLiteral, v5.NegativeLiteral, v6.PositiveLiteral]);
        v6.Sense = true; v6.Reason = c4; trail.Add(v6);
        var c5 = new Constraint([v2.NegativeLiteral, v6.NegativeLiteral, v7.PositiveLiteral]);
        v7.Sense = true; v7.Reason = c5; trail.Add(v7);

        // decide v8 to true
        trail.Push();
        v8.Sense = true;
        trail.Add(v8);
        // units
        var c6 = new Constraint([v8.NegativeLiteral, v9.PositiveLiteral]);
        v9.Sense = true; v9.Reason = c6; trail.Add(v9);
        var c7 = new Constraint([v7.NegativeLiteral, v8.NegativeLiteral, v10.PositiveLiteral]);
        v10.Sense = true; v10.Reason = c7; trail.Add(v10);

        // decide v11 to true
        trail.Push();
        v11.Sense = true;
        trail.Add(v11);
        // units
        var c8 = new Constraint([v11.NegativeLiteral, v12.PositiveLiteral]);
        v12.Sense = true; v12.Reason = c8; trail.Add(v12);
        var c9 = new Constraint([v12.NegativeLiteral, v13.PositiveLiteral]);
        v13.Sense = true; v13.Reason = c9; trail.Add(v13);

        //
        // and this leads to a confict in this constraint:
        //
        var conflictingConstraint = new Constraint([v2.NegativeLiteral, v7.NegativeLiteral, v12.NegativeLiteral, v13.NegativeLiteral]);

        // fill the array to verifiy it is cleared by the creator.
        var learnedLiterals = new StampArray();
        foreach (var variable in variables)
        {
            learnedLiterals.Add(variable.PositiveLiteral.StampIndex);
            learnedLiterals.Add(variable.NegativeLiteral.StampIndex);
        }

        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var seq = new MockSequence();
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c9, 1));
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c8, 1));
        var sut = new LearnedConstraintCreator(trail, activityManager.Object);

        sut.CreateLearnedConstraint(conflictingConstraint, learnedLiterals);
        Assert.Equal([
            v2.NegativeLiteral.StampIndex,
            v7.NegativeLiteral.StampIndex,
            v12.NegativeLiteral.StampIndex
            ], learnedLiterals);

        activityManager.VerifyAll();
    }
    [Fact]
    public void MadeUpExample2()
    {
        var variables = Enumerable.Range(0, 14).Select(i => new Variable(i)).ToArray();
        var trail = new VariableTrail(new CandidateHeap(variables), 16);

        Variable v1 = variables[0], v2 = variables[1], v3 = variables[2], v4 = variables[3],
            v5 = variables[4], v6 = variables[5], v7 = variables[6], v8 = variables[7],
            v9 = variables[8], v10 = variables[9], v11 = variables[10], v12 = variables[11],
            v13 = variables[12], v14 = variables[13];

        // decide v1 to true
        trail.Push();
        v1.Sense = true;
        trail.Add(v1);
        // units
        var c1 = new Constraint([v1.NegativeLiteral, v2.PositiveLiteral]);
        var c2 = new Constraint([v1.NegativeLiteral, v3.NegativeLiteral]);
        v2.Sense = true; v2.Reason = c1; trail.Add(v2);
        v3.Sense = false; v3.Reason = c2; trail.Add(v3);

        // decide v4 to true
        trail.Push();
        v4.Sense = true;
        trail.Add(v4);
        // units
        var c3 = new Constraint([v4.NegativeLiteral, v5.PositiveLiteral]);
        v5.Sense = true; v5.Reason = c3; trail.Add(v5);
        var c4 = new Constraint([v4.NegativeLiteral, v5.NegativeLiteral, v6.PositiveLiteral]);
        v6.Sense = true; v6.Reason = c4; trail.Add(v6);
        var c5 = new Constraint([v2.NegativeLiteral, v6.NegativeLiteral, v7.PositiveLiteral]);
        v7.Sense = true; v7.Reason = c5; trail.Add(v7);

        // decide v8 to true
        trail.Push();
        v8.Sense = true;
        trail.Add(v8);
        // units
        var c6 = new Constraint([v8.NegativeLiteral, v9.PositiveLiteral]);
        v9.Sense = true; v9.Reason = c6; trail.Add(v9);
        var c7 = new Constraint([v7.NegativeLiteral, v8.NegativeLiteral, v10.PositiveLiteral]);
        v10.Sense = true; v10.Reason = c7; trail.Add(v10);

        // decide v11 to true
        trail.Push();
        v11.Sense = true;
        trail.Add(v11);
        // units
        var c8 = new Constraint([v11.NegativeLiteral, v12.PositiveLiteral]);
        v12.Sense = true; v12.Reason = c8; trail.Add(v12);
        var c9 = new Constraint([v2.NegativeLiteral, v7.NegativeLiteral, v12.NegativeLiteral, v13.PositiveLiteral]);
        v13.Sense = true; v13.Reason = c9; trail.Add(v13);
        var c10 = new Constraint([v13.NegativeLiteral, v14.PositiveLiteral]);
        v14.Sense = true; v14.Reason = c10; trail.Add(v14);

        //
        // and this leads to a confict in this constraint:
        //
        var conflictingConstraint = new Constraint([v11.NegativeLiteral, v14.NegativeLiteral]);

        // fill the array to verifiy it is cleared by the creator.
        var learnedLiterals = new StampArray();
        foreach (var variable in variables)
        {
            learnedLiterals.Add(variable.PositiveLiteral.StampIndex);
            learnedLiterals.Add(variable.NegativeLiteral.StampIndex);
        }

        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);
        var seq = new MockSequence();
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c10, 1));
        activityManager.InSequence(seq).Setup(am => am.IncreaseConstraintActivity(c9, 1));
        var sut = new LearnedConstraintCreator(trail, activityManager.Object);

        sut.CreateLearnedConstraint(conflictingConstraint, learnedLiterals);
        Assert.Equal([
            v2.NegativeLiteral.StampIndex,
            v7.NegativeLiteral.StampIndex,
            v11.NegativeLiteral.StampIndex
            ], learnedLiterals);

        activityManager.VerifyAll();
    }
    [Fact]
    public void Tommi_Junttila_Example()
    {
        /*
         * 
         * This test case is taken from the example 
         * shown here: https://users.aalto.fi/~tjunttil/2020-DP-AUT/notes-sat/cdcl.html
         * 
         */

        var candidateHeap = new Mock<ICandidateHeap>();
        var variables = Enumerable.Range(0, 12).Select(i => new Variable(i)).ToArray();
        var trail = new VariableTrail(candidateHeap.Object, variables.Length);
        var activityManager = new Mock<IManageActivities>(MockBehavior.Strict);

        var sut = new LearnedConstraintCreator(trail, activityManager.Object);

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

        activityManager.Setup(am => am.IncreaseConstraintActivity(c8, 1));
        activityManager.Setup(am => am.IncreaseConstraintActivity(c9, 1));
        activityManager.Setup(am => am.IncreaseConstraintActivity(c10, 1));

        var target = new StampArray();
        sut.CreateLearnedConstraint(conflictingConstraint, target);

        Assert.Equal([x8.NegativeLiteral.StampIndex], target);
        activityManager.VerifyAll();
    }
}
