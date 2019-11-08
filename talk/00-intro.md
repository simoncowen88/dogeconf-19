class: center, middle

[doge]: images/doge.png
[red-cross]: images/red-cross.png

.w100img[![](images/dogeconf.png)]

# Writing performant code in F# #

???

Notes for me

---

class: split-50

## What to expect

.column[

### NO

- A flame war.
- An F\# vs C\# contest.
- A functional vs OOP deathmatch.

]

.column[

### YES

- F\# performance gotchas.
- Micro-level concerns.
- Macro-level techniques.

]

.post-columns[
&nbsp;
Disclaimer: I reserve the right to point out places where we're faster than C\#. :)
]

---

## Functional performance

Absolute runtime performance isn't always all that matters.
Most of the time, it's just one part of the story.

Ask yourself, if I offered you a 10× development speedup for a 10% preformance loss,
would you take it?

That's a 12 month project done in under 6 weeks, while 10ms perf becomes 11ms.

---
