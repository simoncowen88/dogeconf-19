
## Tuples

```
let a : int * string = (1, "foo")
```

Used extensively in F\#

???

Returning multiple values
Anonymous records

--

&nbsp;

Alias for `System.Tuple` - and thus reference types

(F\# does now also have struct tuples)

--

&nbsp;

How bad really are tuples for performance?

???

Allocation

---

## Tuples - well-optimisated

Optimised extremely effectively in a variety of scenarios.

```fsharp
let f a b =
    let tuple = a, b
    fst tuple + snd tuple
```

--

is optimised to:

```fsharp
let f a b = a + b
```

???

Don Syme has put in the effort here

---

## Tuples - pattern matching

Pattern matching on tuples:

```fsharp
let f a b =
    match a, b with
    | 0, 0 -> 0
    | 0, 1 -> 1
    | 1, 0 -> 2
    | 1, 1 -> 3
    | _ -> -1
```

???

extremely common use

--

Becomes case statements (or jump tables if `int`)

No tuple is allocated*

--

&nbsp;

It _is_ possible to have a pattern matching on a tuple allocate.

???

seen in the wild
unable to reproduce
even with very complex scenarios (nested dus &c)

---

## Tuples - pattern matching (vs C\#) 🔥🔥🔥

```csharp
int F (int a, int b)
{
    switch (a,b)
    {
        case (0,0): return 0;
        case (0,1): return 1;
        case (1,0): return 2;
        case (1,1): return 3;
        default: return -1;
    }
}
```

--

Very different IL between the languages.

F\# IL performs around 10% better!

???

For floats the situation is reversed!

---

## Tuples - 'synthetic' tuples

--

For methods such as:

```csharp
bool Dictionary<TKey, TValue>.TryGetValue(TKey ket, out TValue value);
```

--

F\# has a syntax sugar to make this present as

```fsharp
Dictionary<'key, 'value>.TryGetValue : 'key -> bool * 'value
```

--

i.e.

```fsharp
let b, v = dict.TryGetValue "foo"
```

???

`out` parameters are inherently mutation-focused

Would be very non-idiomatic in F\#

---

## Tuples - 'synthetic' tuples

No tuple is allocated in either of these cases:

--

```fsharp
let go (dict : Dictionary<string, int>) : int =
    let b, v = dict.TryGetValue "foo"
    if b then v else -1
```

```fsharp
let go (dict : Dictionary<string, int>) : int =
    match dict.TryGetValue "foo" with
    | true, v -> v
    | _ -> -1
```

???

`v` is secretly a local variable

Returning the tuple &c will force the allocation

---

## Tuples - DU fields

--

(Spoiler: they're not really tuples at all)

--

```fsharp
type Foo = Foo of int * int
```

This defines a type with two fields.

--

```fsharp
type Foo = Foo of alice: int * bob: int
```

More clear when you name them.

--

```fsharp
let foo = Foo (1,2)
```

No tuples were allocated in the making of this `Foo`.

---

## Tuples - DU fields

If you really wanted to be allocating tuples:

```fsharp
type Foo = Foo of (int * int)
```

This defines a type with a single field - that field is a tuple.

???

Very unusual!

---

## Tuples - summary

Heavily optimised in a huge number of scenarios.

Obviously putting them into a list &c will allocate them.

&nbsp;

As always with perf, it's not obvious.

Benchmark, benchmark, benchmark.

???

I was surprise by many things here!

---
