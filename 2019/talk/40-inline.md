
### `inline` keyword

Stolen from StackOverflow:

> The most valuable application of the inline keyword in practice is inlining
higher-order functions to the call site where their function arguments are also
inlined in order to produce a singly fully-optimized piece of code.
>
> Note that this bears little resemblance to what inline does in most other languages.
You can achieve a similar effect using template metaprogramming in C++ but F# can
also inline between compiled assemblies because inline is conveyed via .NET metadata.
>
> For example, the inline in the following fold function makes it 5× faster:

```fsharp
let inline fold (f : 'acc -> 'a -> 'acc) (initial : 'acc) (xs : 'a array) : 'acc =
    let mutable acc = initial
    let len = xs.Length-1
    for i in 0..len do
        acc <- f acc xs.[i]
    acc
```

A wonderfully inefficient way to sum the first n integers.

```fsharp
let xs = Array.init 1000 id
let go () =
    fold (fun a b -> a + b) 0 xs
```

(perf: ~650 nanoseconds)

But ah-ha you think. This will allocate a closure!

So we do all of the previous faff to avoid this. But was it worth it?

No! It performs worse.

(perf: ~5000 nanoseconds)

---

### inline all the things

```fsharp
fold (fun a b -> a + b) 0 xs
```

If fold is marked inline, then this optimises to:

```fsharp
let mutable acc = 1
for i in 0..xs.Length-1 do
    acc <- acc + xs.[i]
acc
```

Rather than a function call for each element of the array, we simply have the operation inlined.
So no closure was allocated anyway!

In fact, our allocating of the closure up-front would have blocked the full inlining.
We would have ended up with:

```fsharp
let mutable acc = 1
for i in 0..xs.Length-1 do
    acc <- folder acc xs.[i]
acc
```

Which performs somewhere in the middle.

---