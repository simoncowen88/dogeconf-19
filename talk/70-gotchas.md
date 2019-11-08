
## Maps and Sets

- They are comparison-based
  - `log n` lookup / insert time instead of constant
  - Comparisons can often be more expensive than hashes + equalities
- Not hash-based
- We wrote HAMT for this reason

- Map lookup allocates

---

## Boxing

```csharp
object o = 1;
```

I can't believe that compiles.

```fsharp
let o = box 1
```

To do the same thing.

--

This allocates an object / box.

--

.quote[
  > To have a unified type system and allow value types to have a completely different representation of their underlying data from the way that reference types represent their underlying data (e.g., an int is just a bucket of thirty-two bits which is completely different than a reference type).
]

--

As with all .NET, be extremely dubious of `object` / `obj`
Especially if it might contain a boxed value type

---

## Enumeration

Does iterating allocate?

seq,list,array
yes,no,no?

JamseG - ValueSeq

A sneaky form of boxing

Accessing a struct through an interface.
No other option when using the IEnumerable interface (i.e. `seq`)

Duck typing for a GetEnumerator method - iff the type is concrete.

This IL for these is so different!
Also a lot gets inlined.

```fsharp
let go_seq (a : int seq) =
    for i in a do
        ignore <| a

let go_list (a : int list) =
    for i in a do
        ignore <| a

let go_array (a : int array) =
    for i in a do
        ignore <| a

// same as seq
let go_array_as_seq (a : int array) =
    let a : int seq = upcast a
    for i in a do
        ignore <| a
```

---

## Funcs

`'a -> 'b` <-> `Func<'a, 'b>`

---

## min and max considered harmful

- Allocate for DateTimes - box
- Can be wrong for floats

---

## Capital letters

- Capital letters can block optimisation
- WTF Don Syme

---

## arrays of unit -> unit functions

- Allocates a closure
- Fix is to use an intermediate variable

```fsharp
for i in 0..arr.Length-1 do
    arr.[i] ()
```

Allocates a closure.

```fsharp
for i in 0..arr.Length-1 do
    let f = arr.[i]
    f ()
```

Doesn't allocate a closure.

--

Why?

Some odd quirk of the compiler.

---

## I can't repro this

Returning a pair where on element is an undsealed custom class

---
