
class: center, middle

.w100img[![](images/this-is-fine.jpg)]

---

class: center, middle

.w100img[![](images/this-is-fire.jpg)]

---

class: center, middle

# Compiler optimisations

???

it's not so bad after all

---

class: center, middle

# Tuples

---

## Tuples

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

a bit noddy

---

## Tuples - pattern matching

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

Becomes case statements (or jump tables if `int`).

No tuple is allocated.*

???

seen in the wild
unable to reproduce
even with very complex scenarios (nested dus &c)

---

class: split-50

## Tuples - pattern matching (vs C\#) 🔥🔥🔥

.column[

```fsharp
let f a b =
    match a, b with
    | 0, 0 -> 0
    | 0, 1 -> 1
    | 1, 0 -> 2
    | 1, 1 -> 3
    | _ -> -1
```

]

.column[

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
]

--

.post-columns[

Very different compiled code.

F\# performs around 10% better.

]

--

For floats the situation is reversed.

???


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

--

No tuple is allocated in either of these cases.

???

`v` is secretly a local variable

Returning the tuple &c will force the allocation

---

## Tuples - summary

Heavily optimised in a large number of scenarios.

Don Syme has put in the effort here.

&nbsp;

As always with performance, it's not obvious.

Benchmark, benchmark, benchmark.

???

I was surprised by many things here!

---
