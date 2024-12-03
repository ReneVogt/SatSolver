namespace Revo.SatSolver.DPLL;

sealed record Variable(int Index)
{
    /// <summary>
    /// Indicates wether this variable is set
    /// to <c>true</c> or <c>false</c>.
    /// </summary>
    public bool Sense { get; set; }

    /// <summary>
    /// Indicates if this is an already fixed
    /// variable or if it is still free to test.
    /// </summary>
    public bool Fixed { get; set; }

    /// <summary>
    /// The constraints where this variable is contained
    /// as not-negated literal.
    /// </summary>
    public List<Constraint> Positives { get; } = [];

    /// <summary>
    /// The constraints where this variable is contained
    /// as negated literal.
    /// </summary>
    public List<Constraint> Negatives { get; } = [];

    /// <summary>
    /// The number of still active constraints containing
    /// this variable as not-negated.
    /// </summary>
    public int NumberOfActivePositives { get; set; }

    /// <summary>
    /// The number of still active constraints containing
    /// this variable as negated.
    /// </summary>
    public int NumberOfActiveNegatives { get; set; }
}
