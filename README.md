# SatSolver

Out of curiosity I started to implement a Satisfiability Solver.

CNFs for tests (SAT/UNSAT) taken from [here](https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html), collections uf250-1065 and uuf250-1065.

So far a basic DPLL algorithm using two-watched-literals scheme has been implemented.

I use VSIDS with CDCL which seems to work correct, but does not (yet) perform very well,
because the are some important concepts missing.

So these are the things we'd like to add:
- restarts (to use learned constraints and activities/polarities to find better ways)
- clause minimization (by removing redundant/implicated literals from learned clauses)
- clause deletion by smarter criteria
- maybe an initial calculation for activities and polarities

---
René Vogt, Dresden 2025