# Test organization

- **Unit tests** live alongside the existing component-oriented folders
  (`DataStructures`, `Processors`, `Tools`, `Parsing`) and in the root test classes.
  They test individual types, using `TestComponentStore` and mocks where needed.
- **Component tests** live under `ComponentTests/<feature>` with matching namespaces
  and `[Trait("TestLevel", "Component")]`. They exercise cooperating production
  components through the solver's public API, using small in-memory problems and
  no mocks. Group classes by behavior, for example `Solving/UnsatisfiableStateTests`,
  so incremental insertion, reset, and other regressions can grow independently.
- **Integration tests**, when added, should live under `IntegrationTests/<feature>`
  with `[Trait("TestLevel", "Integration")]`. Use these for complete workflows
  crossing boundaries such as CNF file loading/parsing and solving. Existing
  file-based solving tests remain in `SatSolverTests.Solving.cs` and use their
  `Category` traits (`Simple Cases`, `SAT`, `UNSAT`).

Keep small problem fixtures close to the tests that use them. Share helpers only
when multiple test classes need them; model validation is available through
`TestHelpers.SolutionValidator`.

From the repository root, run all component tests with:

```powershell
dotnet test src/SatSolver/SatSolverTests/SatSolverTests.csproj --filter "TestLevel=Component"
```

To run only the UNSAT lifecycle regressions, use
`--filter "FullyQualifiedName~ComponentTests.Solving.UnsatisfiableStateTests"`.
