namespace Revo.SatSolver.Processors;

interface IReduceLearnedConstraints 
{
    void ReduceLearnedConstraintsIfNecessary(int originalConstraintCount);
    void ReduceLearnedConstraints();
}