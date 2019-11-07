class: center, middle

# Writing performant code in F# #

???

Notes for me

---

## What to expect

Things this talk *isn't*:

- A flame war.
- An F\# vs C\# contest.
- A functional vs OOP deathmatch.

This talk *is* about F\# performance in isolation:

- How to make it perform well.
- Things to watch our for.
- How those of writing F\# at GR get the most out the language.

Disclaimer: I reserve the right to point out places where we're faster than C\#. :)

---

## Functional performance

Things we'll talk about about:

- Immutability
  - Parallelisation
  - Optimisations*
- Allocation
  - Closures
  - Tuples
  - Struct types
- Inlining
- Cheating
  - Case study: List.map
- Macro optimisations
  - Initial algebras

I also want to remind us all that sometimes absolute runtime performance is all that matter.
But most of the timme, it's just one part of the story.

Ask yourself, if I offered you a 10× development speedup for a 10% preformance loss,
would you take it? (12 month project done in under 6 weeks, 10ms perf becomes 11ms).

???

- F\#'s lack of purity reduces its ability to optimise perfectly here.
- Aggressive inlining - even across assembly boundaries.

---
