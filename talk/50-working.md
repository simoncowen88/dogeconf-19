
## Maps and Sets can be slow

- They are comparison-based
  - `log n` lookup / insert time instead of constant
  - Comparisons can often be more expensive than hashes + equalities
- Not hash-based
- We wrote HAMT for this reason

- Map lookup allocates

---

# Does iterating allocate?

seq,list,array
yes,no,no?

JamseG - ValueSeq

---

`'a -> 'b` <-> `Func<'a, 'b>`


---

# Whats going on with option?

Does it cost when 'a is a ref type?

---

Returning a pair where on element is an undsealed custom class

---

## min and max considered harmful

- Allocate for DateTimes
- Capital letters can block optimisation

---

## arrays of unit -> unit functions

- Allocates a closure
- Fix is to use an intermediate variable
