using Revo.SatSolver.DataStructures;
using Revo.SatSolver.Tools;

namespace Revo.SatSolver.Processors;

sealed class PreProcessor<TConstraintFactory>(SatSolverOptions _options, Problem _problem, UnitPropagationQueue _unitPropagationQueue, Variable[] _variables, IConstraintFactory constraintFactory) : IPreProcessor where TConstraintFactory : IConstraintFactory
{
    readonly TConstraintFactory _constraintFactory = (TConstraintFactory)constraintFactory;
    public int BuildConstraints()
    {
        var clauseCount = 0;
        var scores = new double[_variables.Length << 1];
        var literals = new HashSet<ConstraintLiteral>();
        var tautologyTest = new StampArray();

        foreach (var clause in _problem.Clauses)
        {
            literals.Clear();

            //
            // Map the literals in the clause to ConstraintLiterals
            // in the Constraint to generate.
            //
            foreach (var literal in clause.Literals)
                literals.Add(literal.Sense ? _variables[literal.Id-1].PositiveLiteral : _variables[literal.Id-1].NegativeLiteral);

            //
            // Check if the clause is a tautology (e.g. "a | !a")
            // to skip those immediatly fulfilled clauses.
            //
            tautologyTest.Clear();
            if (literals.Any(l => !tautologyTest.Add(l.Variable.Index))) continue;

            clauseCount++;

            //
            // Constraints with a single literal are
            // immediate unit propagations.
            //
            var constraint = _constraintFactory.CreateInitialConstraint(literals);
            if (constraint.Literals.Length == 1)
                _unitPropagationQueue.Enqueue((constraint.Watched1, constraint));

            //
            // Calculat Jeroslow-Wang heuristic for 
            // all litrals in the constraint.
            //
            foreach (var literal in literals)
            {
                var index = literal.Variable.Index << 1;
                if (!literal.Orientation) index+=1;
                scores[index] += Math.Pow(2, -constraint.Literals.Length);
            }
        }

        //
        // The sum of the two literals' Jeroslow-Wang score
        // is our initial activity for the variable. 
        //
        // The literal with the higher score determines the
        // polarity of the variable (note: the goal is to
        // fulfill as many constraints as possible, not to
        // generate many unit propagations).
        //
        var maxActivity = double.MinValue;
        for (var i = 0; i<_variables.Length; i++)
        {
            var ps = scores[i<<1];
            var ns = scores[(i<<1)+1];
            var activity = ps + ns;
            if (activity > maxActivity) maxActivity = activity;
            _variables[i].Activity = activity;
            _variables[i].Polarity = ps > ns;
        }
        maxActivity /= _options.VariableActivityDecayFactor;
        for (var i = 0; i<_variables.Length; i++)
            _variables[i].Activity /= maxActivity;

        return clauseCount;
    }
}
