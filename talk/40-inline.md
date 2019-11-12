
## inline keyword

&nbsp;

Functions marked `inline` are integrated directly into the calling code.

&nbsp;

--

From StackOverflow...

.quote[
> The most valuable application of the inline keyword in practice is inlining
higher-order functions to the call site where their function arguments are also
inlined in order to produce a singly fully-optimized piece of code.
>
> &nbsp;
>
> Note that this bears little resemblance to what inline does in most other languages.
You can achieve a similar effect using template metaprogramming in C++ but F# can
also inline between compiled assemblies because inline is conveyed via .NET metadata.
]

---

## inline keyword

Let's take a function that folds over an array...

```fsharp
let inline fold (f : 'acc -> 'a -> 'acc) (initial : 'acc) (xs : 'a array) : 'acc =
    let mutable acc = initial
    let len = xs.Length-1
    for i in 0..len do
        acc <- f acc xs.[i]
    acc
```

???

safe island of mutability

---

## inline keyword

We can write ourselves a wonderfully inefficient way to sum the first n integers.

```fsharp
let xs = Array.init 1000 id
let go () = fold (fun a b -> a + b) 0 xs
```

--

Perf: ~650 nanoseconds

--

But ah-ha you think. This will allocate a closure!

So we do all of the previous faff to avoid this.

--

&nbsp;

Perf: ~5000 nanoseconds

???

what went wrong?

---

## inline all the things

```fsharp
let go () =
    fold (fun a b -> a + b) 0 xs
```

--

But `fold` is marked inline, so we just blat the body in, replacing args...

---

## inline all the things

```fsharp
let go () =
    let mutable acc = 0
    let len = xs.Length-1
    for i in 0..len do
        acc <- (fun a b -> a + b) acc xs.[i]
    acc
```

???

initial = 0
f = (fun a b -> a+b)

--

But now F\# can perform further optimisations!

Here, that means inlining `(fun a b -> a + b)`

---

## inline all the things

```fsharp
let go () =
  let mutable acc = 1
  let len = xs.Length-1
  for i in 0..len do
      acc <- acc + xs.[i]
  acc
```

--

So no closure was allocated anyway!

In fact, our allocating of the closure up-front  blocked the full inlining.
And given how cheap everything was, the extra perf was simply from all of the function calls.

---
