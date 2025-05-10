# SatSolver

Out of curiosity we started to implement a Satisfiability Solver.

CNFs for tests (SAT/UNSAT) taken from [here](https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html), collections uf250-1065 and uuf250-1065.

We use the DPLL algorithm with two-watched-literals scheme, VSIDS and CDCL, different restart strategies and an interval based clause deletion.

The results are currently not very amazing. We keep researching improvements and experimenting with the `SatSolver.Options` to achieve better performance.

---
René Vogt, Dresden 2025