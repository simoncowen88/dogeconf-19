
## Tuples

Tuples are used extensively in F\#.

Often as a way of returning multiple values at once.

Mildly less prevalent since anonymous records.

Tuples in F\# are simply `System.Tuple` instances - and thus reference types.

(F\# has struct tuples as well)

But let's have a look at how bad tuples really are for performance.

---

## Tuples - optimisations

They are actually optimised extremely effectively in a variety of scenarios.

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

So clearly, a good deal of optmisation is happening around tuples.

---

## Tuples - pattern matching

Pattern matching on 'tuples' is also optimised efficiently.

```fsharp
let f a b =
    match a, b with
    | 0, 0 -> 0
    | 0, 1 -> 1
    | 1, 0 -> 2
    | 1, 1 -> 3
    | _ -> -1
```

This is tranformed into case statements (or jump tables if `int`).

Interestingly, this results in very different IL from the C# equivalent.
Yet more interestingly, the F\# IL performs around 10% better!

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

It _is_ possible to have a pattern match on a tuple allocate.

We've seen it in the wild, but I've been unable to reproduce it no matter how complicate I make things!


- **show the il differences / benchmark it?**

---

## Tuples - synthetic tuples for `out` parameters

For methods such as:

```csharp
bool Dictionary<TKey, TValue>.TryGetValue(TKey ket, out TValue value);
```

F\# has a syntax sugar to make this present as

```fsharp
Dictionary<'key, 'value>.TryGetValue : 'key -> bool * 'value
```

However, don'r be fooled. No tuple is allocated in either of these cases:

```fsharp
let go () =
    let b, v = dict.TryGetValue 1
    if b then v else -1
```

```fsharp
let go () =
    match dict.TryGetValue 1 with
    | true, v -> v
    | _ -> -1
```

???

As `out` parameters are inherantly mutation-focused.

---

## Tuples - DU fields

- Also, tuples on DUs are not really tuples

```fsharp
[<Struct>] type Foo = Foo of int * int
```

This defines a type with two fields.

```fsharp
let foo = Foo (1,2)
```

No tuples were allocated in the making of this `Foo`.

If you really wanted to be allocating tuples, then you need this:

```fsharp
[<Struct>] type Foo = Foo of (int * int)
```

This defines a type with a single field - that field is a tuple.

---
