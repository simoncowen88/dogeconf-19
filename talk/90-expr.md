class: center, middle

# The bigger picture

???

seen gotchas in the small

how do you win in the large?

---

## Recap

```fsharp
let f a b = a + b
```

is equivalent to

```fsharp
let f a = fun b -> a + b
```

is equivalent to

```fsharp
let f = fun a -> fun b -> a + b
```

---

## A simple goal

Write a declarative algebra that can outperform `float -> float` functions.

&nbsp;

--

Algebra : set of types and methods.

Declarative : not an implementation, simply a description.

---

## The idea

In Tyburn, and many other scenarios, we have two distinct phases:

&nbsp;

--

### Startup

No particular performance requirements.

Load data to construct things to run on...

&nbsp;

--

### "Hot path"

Performance is paramount.

No allocation allowed.

???

think signals

compiler doesn't know this

---

## The model

A `float -> float` function.

--

&nbsp;

Constructed / parameterised by runtime values at startup.

--

&nbsp;

For use on the hot path.

---

## Don't think, just do

```fsharp
let make a b c d (x : float) =
    (c - d) * (a + b + x) * (a + b + x)
```

A `float -> float` function, parameterised by 4 `float`s.

--

```fsharp
// phase 1 - startup
let f : float -> float = make 1.2 2.3 3.4 4.5

// phase 2 - hot path
let x = f 98.7
```

---

## Don't think, just do

```fsharp
let make a b c d (x : float) =
    (c - d) * (a + b + x) * (a + b + x)
```

&nbsp;

There's no place to do slow up-front work.

---

## Think a little bit

```fsharp
let make a b c d (x : float) =
    (c - d) * (a + b + x) * (a + b + x)
```

---

## Think a little bit

```fsharp
let make a b c d : float -> float =
    fun x ->
        (c - d) * (a + b + x) * (a + b + x)
```

---

## Think a little bit

```fsharp
let make a b c d : float -> float =
    // do all the work you like here
    fun x ->
        (c - d) * (a + b + x) * (a + b + x)
```

---

## Think a little bit

```fsharp
let make a b c d : float -> float =
    // do all the work you like here
    fun x ->
        // be as fast as possible here
        (c - d) * (a + b + x) * (a + b + x)
```

--

We've now given ourselves a place to do work at startup.

???

let's use it!

---

## Think a lot bit

```fsharp
let make a b c d : float -> float =
    // do all the work you like here
    fun x ->
        // be as fast as possible here
        (c - d) * (a + b + x) * (a + b + x)
```

???

Do some hand-optimising!

---

## Think a lot bit

```fsharp
let make a b c d : float -> float =
    let p1 = c - d
    fun x ->
        // be as fast as possible here
        p1 * (a + b + x) * (a + b + x)
```

???

the compiler can't constant-fold

(to it, these aren't constants)

---

## Think a lot bit

```fsharp
let make a b c d : float -> float =
    let p1 = c - d
    let p2 = a + b
    fun x ->
        p1 * (p2 + x) * (p2 + x)
```

--

How far can we go?

---

## Expanding brain meme

```fsharp
let make a b c d : float -> float =
    let p1 = c - d
    let p2 = a + b
    if p1 = 0.0 then
        fun x -> 0.0
    elif p2 = 0.0 then
        fun x -> p1 * x * x
    else
        fun x -> p1 * (p2 + x) * (p2 + x)
```

--

Look how far we've come...

```fsharp
let make a b c d (x : float) =
    (c - d) * (a + b + x) * (a + b + x)
```

---

## Expanding brain meme

What if the code changes?

--

Did you cover all optimisations?

--

Were your refactorings correct?

--

Can you still understand the logic?

--

How far do you go?

???

do you benefit from new optimisations?

--

&nbsp;

It _may_ not be wrong.

--

But it is the wrong approach.

---

class: center, middle

# A different approach

---

## The algebra

```fsharp
type Expr =
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
    | Const of float
    | Input
```

???

Input = input variable

--

Algebra : set of types and methods.

Declarative : not an implementation, simply a description.

---

## Translation

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

--

simply becomes...

--

```fsharp
let make a b c d : Expr =
  Mul (
    Mul (
      Sub (Const c, Const d),
      Add ((Add (Const a, Const b)), Input)
    ),
    Add ((Add (Const a, Const b)), Input)
  )
```

---

## Operators

```fsharp
let (+) a b = Add (a, b)
let (-) a b = Sub (a, b)
let (*) a b = Mul (a, b)
let (/) a b = Div (a, b)
```

--

```fsharp
// fun x -> x + 1.0
let add1 : Expr = Add (Input, (Const 1.0))
let add1 : Expr = Input + (Const 1.0)
```

--

```fsharp
// fun x -> x * 2.0
let times2 : Expr = Mult (Input, (Const 2.0))
let times2 : Expr = Input * (Const 2.0)
```

---

## Translation

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

--

simply becomes...

--

```fsharp
let make a b c d : Expr =
    let a = Const a
    let b = Const b
    let c = Const c
    let d = Const d
    let x = Input
    (c - d) * (a + b + x) * (a + b + x)
```

???

not perfect

we could probably do better with the constants

---

## What have we achieved?

Not much. Yet.

--

&nbsp;

Original function was opaque.

Relied on compiler's optimisations.

It can't leverage the 2 phases.

???

or our hand-crafted optimisations

--

&nbsp;

New expression is inspectable.

Just a description of what we want to do.

What are we going to do with it?

---

## Printing

```fsharp
let rec print expr =
    match expr with
    | Add (a, b) -> sprintf "(%s + %s)" (impl a) (impl b)
    | Sub (a, b) -> sprintf "(%s - %s)" (impl a) (impl b)
    | Mul (a, b) -> sprintf "(%s * %s)" (impl a) (impl b)
    | Div (a, b) -> sprintf "(%s / %s)" (impl a) (impl b)
    | Const a    -> sprintf "%.1f" a
    | Input        -> "x"
```

A simple evaluator for our algebra.

---

## Printing


```fsharp
let rec print expr =
    match expr with
    | Add (a, b) -> sprintf "(%s + %s)" (impl a) (impl b)
    | Sub (a, b) -> sprintf "(%s - %s)" (impl a) (impl b)
    | Mul (a, b) -> sprintf "(%s * %s)" (impl a) (impl b)
    | Div (a, b) -> sprintf "(%s / %s)" (impl a) (impl b)
    | Const a    -> sprintf "%.1f" a
    | Input        -> "x"
```

```fsharp
let make a b c d = ...
    (c - d) * (a + b + x) * (a + b + x)
```

--

```
make 1.2 2.3 3.4 4.5 |> print
"(((3.4 - 4.5) * ((1.2 + 2.3) + x)) * ((1.2 + 2.3) + x))"
```

--

```
make 0.0 2.3 1.0 1.0 |> print
"(((1.0 - 1.0) * ((0.0 + 2.3) + x)) * ((0.0 + 2.3) + x))"
```

---

## Baseline

It turns out that `float -> float` functions are fast

Who knew?

--

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

100,000,000 invocations takes ~300ms.

???

work cut out for us

---

## Evaluation

```fsharp
let rec evaluate (expr : Expr) (x : float) : float =
    match expr with
    | Add (a, b) -> (evaluate a x) + (evaluate b x)
    | Sub (a, b) -> (evaluate a x) - (evaluate b x)
    | Mul (a, b) -> (evaluate a x) * (evaluate b x)
    | Div (a, b) -> (evaluate a x) / (evaluate b x)
    | Const a -> a
    | Input -> x
```

???

completely naive

--

&nbsp;

How does it fare?

--

**~12 times slower**

???

better than I expected!

not used the 2 phases

---

## Implementation

```fsharp
let rec implement (expr : Expr) : float -> float =
    match expr with
    | Add (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x + b x
    | Sub (a, b) -> ...
    | Mul (a, b) -> ...
    | Div (a, b) -> ...
    | Const a -> fun x -> a
    | Input -> fun x -> x
```

---

## Implementation

```fsharp
// phase 1 - startup
let description : Expr = make 1.2 2.3 3.4 4.5
let implementation : float -> float = implement description

// phase 2 - hot path
let x = implementation 98.7
```

???

by hot path, expr is gone

--

Each operation incurs a method call.

```fsharp
// fun x -> x + 1.0
fun x -> ((fun x -> x) x) + ((fun x -> 1.0) x)
```

--

How does it fare?

--

**~6 times slower**

---

## What next?

Not immediately obvious how to make our implementation faster.

--

&nbsp;

How else could we make this faster?

--

Similar optimisations to those we hand-crafted.

--

```fsharp
let make a b c d : float -> float =
    let p1 = c - d
    let p2 = a + b
    if p1 = 0.0 then
        fun x -> 0.0
    elif p2 = 0.0 then
        fun x -> p1 * x * x
    else
        fun x -> p1 * (p2 + x) * (p2 + x)
```

???

'constant'-folding

mult 0, mult 1, add 0 &c

---

## Constant folding

```fsharp
let rec foldConstants (expr : Expr) : Expr =
    match expr with
    | Add (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const a, Const b -> Const (a + b)
        | a, b -> Add (a, b)
    | Sub (a, b) -> ...
    | Mul (a, b) -> ...
    | Div (a, b) -> ...
    | Const a -> Const a
    | Input -> Input
```

???

doesn't handle all things

e.g. `2 * 3 * x * 4` -> `6 * x * 4`

n-ary multiplication?

---

## Constant folding

Two questions arise:

1. Is this the transformation we wanted?
2. Is this a valid transformation?

--

Let's eyeball it...

```fsharp
make 1.2 2.3 3.4 4.5 |> print
"(((3.4 - 4.5) * ((1.2 + 2.3) + x)) * ((1.2 + 2.3) + x))"
```

```fsharp
make 1.2 2.3 3.4 4.5 |> foldConstants |> print
"((-1.1 * (3.5 + x)) * (3.5 + x))"
```

---

## Constant folding

In reality we'd write property-based-tests.

--

```fsharp
expr |> evaluate === expr |> foldConstants |> evaluate
```

--

We could also test our different evaluators.

--

```fsharp
expr |> evaluate === expr |> implement |> invoke
```

???

randomised expressions

randomised inputs

---

## Implementation

```fsharp
// phase 1 - startup
let description : Expr = make 1.2 2.3 3.4 4.5
let optimised : Expr = foldConstants description
let implementation : float -> float = implement optimised

// phase 2 - hot path
let x = implementation 98.7
```

--

&nbsp;

How does it fare?

--

**~4 times slower**

???

12 -> 6 -> 4

---

## Further optimisations

```fsharp
let removeStupidOps expr =
    match expr with
    | Add (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const 0.0, e | e, Const 0.0 -> e // (0 + e), (e + 0)
        | a, b -> Add (a, b)
    | Sub (a, b) -> ...
    | Mul (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a,b with
        | Const 0.0, _ | _, Const 0.0 -> Const 0.0 // (0 * e), (e * 0)
        | Const 1.0, e | e, Const 1.0 -> e // (1 * e), (e * 1)
        | a, b -> Mul (a, b)
    | Div (a, b) -> ...
    | Const a -> Const a
    | Input -> Input
```

---

## Combining optimisations

```fsharp
make 1.2 3.2 4.5 4.5 |> print
"(((4.5 - 4.5) * ((1.2 + 3.2) + x)) * ((1.2 + 3.2) + x))"
```

--

```fsharp
make 1.2 3.2 4.5 4.5 |> foldConstants |> print
"((0.0 * (4.4 + x)) * (4.4 + x))"
```

--

```fsharp
make 1.2 3.2 4.5 4.5 |> foldConstants |> removeStupidOps |> print
"0.0"
```

---

## Combining optimisations

```fsharp
let optimisations : (Expr -> Expr) list =
    [
        foldConstants
        removeStupidOps
        // ...
    ]
```

```fsharp
let implement' (expr : Expr) : (float -> float) =
    optimisations
    |> List.fold (fun expr opt -> opt expr) expr
    |> implement
```

???

common sub-expressions - variable extraction

--

How does it fare?

--

**~20% faster\***

???

FOR THIS CASE

still slower if optimisations don't reduce it

qualified success

---

class: center, middle

# Must go faster

---

class: center, middle

# Emitting IL

---

class: center, middle

.w100img[![](images/must-go-faster.gif)]

---

## IL operations

```fsharp
let f x = x + 1.0
```

--

Using LINQPad... (or ILSpy, or ildasm, or new Rider)

```yaml
IL_0000:  ldarg.0                             # [x]
IL_0001:  ldc.r8      00 00 00 00 00 00 F0 3F # [1.0; x]
IL_000A:  add                                 # [x + 1.0]
IL_000B:  ret
```

???

add consumes from the stack and pushes back on

---

## IL operations

```fsharp
type Expr =
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
    | Const of float
    | Input
```

--

```fsharp
type ILOp =
    | ILAdd
    | ILSub
    | ILMul
    | ILDiv
    | ILConst of float
    | ILInput
```

???

consume from stack

just need to ensure stack is correct

---

## Converting to IL

```fsharp
let rec opsToEmit (expr : Expr) : ILOp list =
    match expr with
    | Add (a, b) ->
        let a = opsToEmit a   // stack: [a]
        let b = opsToEmit b   // stack: [b; a]
        a @ b @ [ ILAdd ]     // stack: [a + b]
    | Sub (a, b) ->
        let a = opsToEmit a
        let b = opsToEmit b
        a @ b @ [ ILSub ]
    | Mul (a, b) -> ...
    | Div (a, b) -> ...
    | Const a -> [ ILConst a ]
    | Input -> [ ILInput ]
```

---

## Emitting IL

```fsharp
let emitOp (ilGen : ILGenerator) (op : ILOp) =
    match op with
    | ILAdd ->     ilGen.Emit OpCodes.Add
    | ILSub ->     ilGen.Emit OpCodes.Sub
    | ILMul ->     ilGen.Emit OpCodes.Mul
    | ILDiv ->     ilGen.Emit OpCodes.Div
    | ILConst a -> ilGen.Emit (OpCodes.Ldc_R8, a)
    | ILInput ->   ilGen.Emit OpCodes.Ldarg_0
```

---

## Putting it together

```fsharp
// IL emission requires a delegate type
type FloatFloat = delegate of float -> float

let compile (expr : Expr) : float -> float =
    // Make a new method to emit IL for
    let dm = DynamicMethod ("f", typeof<float>, [| typeof<float> |])
    let ilGenerator = dm.GetILGenerator ()

    // Get the IL to emit, and emit it
    expr |> opsToEmit |> Seq.iter (emitOp ilGenerator)

    // Add a return
    ilGenerator.Emit OpCodes.Ret

    // Make this bad boy!
    let f = dm.CreateDelegate(typeof<FloatFloat>) :?> FloatFloat
    f.Invoke
```

---

## The IL

```fsharp
let f x = x + 1.0
```

```yaml
IL_0000:  ldarg.0
IL_0001:  ldc.r8      00 00 00 00 00 00 F0 3F
IL_000A:  add
IL_000B:  ret
```

--

```fsharp
opsToEmit (Input + Const 1.0)
```

```fsharp
[
    ILInput
    ILConst 1.0
    ILAdd
]
```

---

## The IL

```fsharp
make 1.2 2.3 3.4 4.5 |> opsToEmit
```

```fsharp
[
    ILConst 3.4
    ILConst 4.5
    ILSub
    ILConst 1.2
    ILConst 2.3
    ILAdd
    ILVar
    ILAdd
    ILMul
    ILConst 1.2
    ILConst 2.3
    ILAdd
    ILVar
    ILAdd
    ILMul
]
```

---

## The IL

```fsharp
make 1.2 2.3 3.4 4.5 |> optimise |> opsToEmit
```

```fsharp
[
    ILConst 0.0
]
```

---

## Compilation

```fsharp
// phase 1 - startup
let description : Expr = make 1.2 2.3 3.4 4.5
let optimised : Expr = optimise description
let compiled : float -> float = compile optimised

// phase 2 - hot path
let x = compiled 98.7
```

--

&nbsp;

How does it fare?

--

**~10% faster with no optimisations**

???

potentially much faster _with_ optimisations

field loading from the closure vs il constants

---

class: center, middle

.w100img[![](images/i-am-speed.jpg)]

---


class: center, middle

# Summary

---

## Summary

Declarative algebras are great!

--

&nbsp;

They won't outperform imperative immediately.

--

They take advantage of startup vs hot-path.

--

Their inspectability allows for optimisations.

???

centralised optimisations - new ones benefit all

DA does this

---

class: center, middle

# Fin.
