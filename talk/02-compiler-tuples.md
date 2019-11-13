
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

Becomes case statements (or jump tables if `int`)

No tuple is allocated*

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

Very different compiled code.

F\# performs around 10% better!

???

For floats the situation is reversed!

---

## Tuples - summary

Heavily optimised in a huge number of scenarios.

Don Syme has put in the effort here.

&nbsp;

As always with perf, it's not obvious.

Benchmark, benchmark, benchmark.

???

I was surprised by many things here!

---
