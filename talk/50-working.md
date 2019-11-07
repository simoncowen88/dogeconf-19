
## Maps and Sets can be slow

- They are comparison-based
  - `log n` lookup / insert time instead of constant
  - Comparisons can often be more expensive than hashes + equalities
- Not hash-based
- We wrote HAMT for this reason

- Map lookup allocates

---

## min and max considered harmful

- Allocate for DateTimes

---

## arrays of unit -> unit functions

- Allocates a closure
- Fix is to use an intermediate variable
