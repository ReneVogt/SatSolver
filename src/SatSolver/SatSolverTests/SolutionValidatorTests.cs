using Revo.SatSolver;
using TestHelpers;

namespace SatSolverTests;

public sealed class SolutionValidatorTests
{
    [Fact]
    public void Validate_CompleteUnorderedModel_AcceptsWithoutChangingInput()
    {
        var problem = new Problem(3, [new([1, 2]), new([-2])]);
        Literal[] solution = [-3, 1, -2];

        SolutionValidator.Validate(problem, solution);

        Assert.Equal<Literal>([-3, 1, -2], solution);
    }

    [Theory]
    [InlineData(new int[] { 1 })]
    [InlineData(new int[] { 1, 2, 3 })]
    public void Validate_WrongModelLength_Throws(int[] assignments)
    {
        var problem = new Problem(2, [new([1])]);
        Literal[] solution = [.. assignments.Select(id => (Literal)id)];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));

        Assert.Contains("Expected 2 assignments", exception.Message);
    }

    [Fact]
    public void Validate_MissingVariableDespiteSatisfiedClause_Throws()
    {
        var problem = new Problem(2, [new([1, 2])]);
        Literal[] solution = [1];

        Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));
    }

    [Fact]
    public void Validate_NoClausesStillRequiresAllVariables_Throws()
    {
        var problem = new Problem(2, []);
        Literal[] solution = [];

        Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void Validate_DuplicateVariableId_Throws(int secondAssignment)
    {
        var problem = new Problem(2, [new([1])]);
        Literal[] solution = [1, secondAssignment];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));

        Assert.Contains("Variable ID 1 is assigned more than once", exception.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-3)]
    public void Validate_OutOfRangeIdWithCorrectModelLength_Throws(int extraAssignment)
    {
        var problem = new Problem(2, [new([1])]);
        Literal[] solution = [1, extraAssignment];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));

        Assert.Contains("Variable ID 3 is outside 1..2", exception.Message);
    }

    [Fact]
    public void Validate_UnsatisfiedClause_Throws()
    {
        var problem = new Problem(2, [new([1]), new([-2])]);
        Literal[] solution = [1, 2];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));

        Assert.Contains("does not satisfy clause: -2 0", exception.Message);
    }

    [Fact]
    public void Validate_EmptyClause_Throws()
    {
        var problem = new Problem(0, [new([])]);
        Literal[] solution = [];

        Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));
    }

    [Fact]
    public void Validate_NoVariablesOrClauses_AcceptsEmptyModel()
    {
        var problem = new Problem(0, []);
        Literal[] solution = [];

        SolutionValidator.Validate(problem, solution);
        SolutionValidator.Validate(problem, new[] { solution });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidateModels_DuplicateModelRegardlessOfOrder_Throws(bool reverseOrder)
    {
        var problem = new Problem(2, [new([1, 2])]);
        Literal[] first = [1, -2];
        Literal[] duplicate = reverseOrder ? [-2, 1] : [1, -2];
        Literal[][] solutions = [first, [-1, 2], duplicate];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solutions));

        Assert.Contains("Duplicate model", exception.Message);
    }

    [Fact]
    public void ValidateModels_DifferentUnusedVariableValues_Accepts()
    {
        var problem = new Problem(2, [new([1])]);
        Literal[][] solutions = [[1, -2], [2, 1]];

        SolutionValidator.Validate(problem, solutions);
    }

    [Fact]
    public void ValidateModels_InvalidModelAfterValidModel_Throws()
    {
        var problem = new Problem(2, [new([1])]);
        Literal[][] solutions = [[1, -2], [-1, 2]];

        Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solutions));
    }

    [Fact]
    public void ValidateModels_RepeatedEmptyModel_Throws()
    {
        var problem = new Problem(0, []);
        Literal[][] solutions = [[], []];

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solutions));

        Assert.Contains("Duplicate model", exception.Message);
    }

    [Fact]
    public void ValidateModels_EmptySequence_DoesNotRequireUnsatisfiability()
    {
        var problem = new Problem(1, [new([1])]);
        Literal[][] solutions = [];

        SolutionValidator.Validate(problem, solutions);
    }

    [Fact]
    public void ValidateModels_LazySequence_EnumeratesOnceAndStopsAtDuplicate()
    {
        var problem = new Problem(1, []);
        var enumerationCount = 0;

        IEnumerable<Literal[]> Solutions()
        {
            Assert.Equal(1, ++enumerationCount);
            yield return [1];
            yield return [1];
            Assert.Fail("Validation should stop at the duplicate model.");
        }

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, Solutions()));

        Assert.Contains("Duplicate model", exception.Message);
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public void ValidateModels_ReusedArray_DetectsDuplicateUsingOriginalValues()
    {
        var problem = new Problem(1, []);
        var reachedThirdModel = false;

        IEnumerable<Literal[]> Solutions()
        {
            Literal[] solution = [1];
            yield return solution;
            solution[0] = -1;
            yield return solution;
            reachedThirdModel = true;
            solution[0] = 1;
            yield return solution;
        }

        var exception = Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, Solutions()));

        Assert.Contains("Duplicate model", exception.Message);
        Assert.True(reachedThirdModel);
    }

    [Fact]
    public void Validate_NullLiteral_Throws()
    {
        var problem = new Problem(1, []);
        Literal[] solution = [null!];

        Assert.Throws<ArgumentException>(() => SolutionValidator.Validate(problem, solution));
    }
}
