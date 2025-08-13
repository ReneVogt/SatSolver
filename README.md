# SatSolver

Out of curiosity we started to implement a Satisfiability Solver.

CNFs for tests (SAT/UNSAT) taken from [here](https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html), collections uf250-1065 and uuf250-1065.

We use the DPLL algorithm with two-watched-literals scheme, VSIDS and CDCL, different restart strategies and an interval based clause deletion.

The CDCL parts are not yet working, slow and buggy... work is in progress.

---
René Vogt, Dresden 2025