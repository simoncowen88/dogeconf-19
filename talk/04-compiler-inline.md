
class: center, middle

# Inlining

---

## Inlining

> In computing, inline expansion, or inlining, is a manual or compiler optimization that replaces a function call site with the body of the called function.

--

No function call.

???

no args passing

no stackframe

locality of reference

--

Allows for further optimisations.

???

constant propagation

escape analysis

--

F\# is very aggressive with its inlining.

---

## Inlining example

```fsharp
let f a = 2 * a
let g a = f (a + 1)
```

What does the compiled code for `g` actually look like?

---

## Inlining example - no optimisations

```fsharp
let f a = 2 * a
let g a = f (a + 1)
```

```yaml
f:
IL_0000:  ldc.i4.2      # load 2                      [2]
IL_0001:  ldarg.0       # load first argument         [a;2]
IL_0002:  mul           # multiply top stack elements [2*a]
IL_0003:  ret

g:
IL_0000:  ldarg.0       # load first argument     [a]
IL_0001:  ldc.i4.1      # load 1                  [1;a]
IL_0002:  add           # add top stack elements  [a+1]
IL_0003:  call        f # invoke f                [f (a+1)]
IL_0008:  ret
```

???

Look at g first

---

## Inlining example - optimised

```fsharp
let f a = 2 * a
let g a = f (a + 1)
```

```yaml
g:
IL_0000:  ldc.i4.2  # load 2              [2]
IL_0001:  ldarg.0   # load first argument [a;2]
IL_0002:  ldc.i4.1  # load 1              [1;a;2]
IL_0003:  add       # add                 [a+1;2]
IL_0004:  mul       # multiply            [2*(a+1)]
IL_0005:  ret
```

--

```fsharp
let g a = 2 * (a + 1)
```

???

call site of `f` in `g` was replaced by the body of `f`

---

class: center, middle

# inline keyword

---

## inline keyword

Functions marked `inline` are integrated directly into the calling code.

&nbsp;

--

From StackOverflow...

> The most valuable application of the inline keyword in practice is inlining
higher-order functions to the call site where their function arguments are also
inlined in order to produce a singly fully-optimized piece of code.

---

## inline keyword

Let's take a function that folds over an array...

```fsharp
let inline fold (f : 'acc -> 'a -> 'acc) (initial : 'acc) (xs : 'a array) : 'acc =
    let mutable acc = initial
    let maxIndex = xs.Length - 1
    for i in 0..maxIndex do
        acc <- f acc xs.[i]
    acc
```

???

safe island of mutability

---

## inline keyword

```fsharp
let sum (xs : int array) = fold (fun a b -> a + b) 0 xs
```

This takes ~650 nanoseconds for 1000 elements.

--

&nbsp;

But this will allocate a closure.

--

So you force a closure to be allocated up-front.

--

```fsharp
let folder = id <| fun a b -> a + b
let sum (xs : int array) = fold folder 0 xs
```

--

This takes ~5000 nanoseconds for 1000 elements.

???

what went wrong?

---

class: center, middle

.w100img[![](images/not-sure-fry.jpg)]

---

## inline keyword

```fsharp
let sum (xs : int array) = fold (fun a b -> a + b) 0 xs
```

```fsharp
let inline fold (f : 'acc -> 'a -> 'acc) (initial : 'acc) (xs : 'a array) : 'acc =
    let mutable acc = initial
    let maxIndex = xs.Length - 1
    for i in 0..maxIndex do
        acc <- f acc xs.[i]
    acc
```

???

inline it!

--

```fsharp
let sum (xs : int array) =
    let mutable acc = 0
    let len = xs.Length - 1
    for i in 0..len do
        acc <- (fun a b -> a + b) acc xs.[i]
    acc
```

---

## inline keyword

```fsharp
let sum (xs : int array) =
    let mutable acc = 0
    let len = xs.Length - 1
    for i in 0..len do
        acc <- (fun a b -> a + b) acc xs.[i]
    acc
```

Now the compiler can perform further optimisations!

Here, that means inlining `(fun a b -> a + b)`

---

## inline keyword

```fsharp
let sum (xs : int array) =
    let mutable acc = 0
    let len = xs.Length - 1
    for i in 0..len do
        acc <- acc + xs.[i]
    acc
```

--

No closure was allocated anyway.

&nbsp;

Up-front closure allocation blocked the full inlining.

Performance loss was from all of the function calls.

???

virtcalls to closure

---
