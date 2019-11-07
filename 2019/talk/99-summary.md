
## Summary

Unsurprisingly, there is a lot of nuance to writing performant F\#.

We've seen the compiler perform some fairly impressive optimisations,
occassionally rendering our naive attempts at optimising redundant - or even
making the situation worse.

As with all languages, don't rely on the optimisations you think the compiler
*should* make. Nor should you hand-craft *theoretically* performant - although
likely entirely unreadable - code.
**Use the profiler Luke**
