class: center, middle

# How we make things go fast #

---

## Recap

### Higher-order functions

```fsharp
let f a b = a + b
```

is equivalent to

```fsharp
let f a = fun b -> a + b
```

---

## A simple goal

&nbsp;

Write a declarative algebra that can outperform `float -> float` functions.

&nbsp;

--

Algebra : set of types of methods acting on them

Declarative : not an implementation, simply a description

---

## The idea

In Tyburn, and many other scenarios, we have two distinct phases

&nbsp;

--

### Startup

No particular performance requirements

Load data to construct things to run on...

--

### "Hot path"

Performance is key

No allocation allowed

??

think signals

---

## The model

A float -> float function for the hot path

&nbsp;

--

Parameterised by runtime values at startup

&nbsp;

--

We can accept slow start-up, but not slow hot-path

---

## Don't think, just do

```fsharp
let make a b c d (x : float) =
    (c - d) * (a + b + x) * (a + b + x)
```

A `float -> float` function, parameterised by 4 `float`s

--

There's no place to do slow up-front work

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
    let p1 = c - d
    let p2 = a + b
    fun x -> p1 * (p2 + x) * (p2 + x)
```

Do some hand-optimising!
--
 (because you hate yourself)

&nbsp;

--

What if the code changes?

--

Did you cover all the optimisations?

--

Were your refactorings correct?

--

Can you still understand the logic?

--

How far do you go?

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

Just stop

--

It's not wrong

--

But it is the wrong approach

---

## A better way

```fsharp
type Expr =
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
    | Const of float
    | Var
```

???

Var = input variable

--

Algebra : set of types of methods acting on them

Declarative : not an implementation, simply a description

---

## A better way

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

becomes...

--

```fsharp
let make a b c d : Expr =
  Mul (
    Mul (
      Sub (Const c, Const d),
      Add ((Add (Const a, Const b)), Var)
    ),
    Add ((Add (Const a, Const b)), Var)
  )
```

---

## A better way

```fsharp
let (+) a b = Add (a, b)
let (-) a b = Sub (a, b)
let (*) a b = Mul (a, b)
let (/) a b = Div (a, b)
```

Define some operators

--

```fsharp
let add1 : Expr = Var + (Const 1.0) // fun x -> x + 1.0

let times2 : Expr = Var * (Const 2.0) // fun x -> x * 2.0

let div3 : Expr = Var / (Const 3.0) // fun x -> x / 3.0
```

---

## A better way

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

becomes...

--

```fsharp
let make a b c d : Expr =
    let a = Const a
    let b = Const b
    let c = Const c
    let d = Const d
    let x = Var
    (c - d) * (a + b + x) * (a + b + x)
```

---

## What have we achieved?

Not much. Yet.

--

&nbsp;

The original function was opaque

We relied compiler's optimisations, but they only happen at JIT

It can't leverage the 2 phases

--

&nbsp;

The new expression is inspectable

It's just a description of what we want to do

What are we going to do with it?

---

## Inspectability

```fsharp
let rec print expr =
    match expr with
    | Add (a, b) -> sprintf "(%s + %s)" (impl a) (impl b)
    | Sub (a, b) -> sprintf "(%s - %s)" (impl a) (impl b)
    | Mul (a, b) -> sprintf "(%s * %s)" (impl a) (impl b)
    | Div (a, b) -> sprintf "(%s / %s)" (impl a) (impl b)
    | Const a    -> sprintf "%.1f" a
    | Var        -> "x"
```

A simple evaluator for our algebra

```
make 1.2 2.3 3.4 4.5 |> print
val it : string = "(((3.4 - 4.5) * ((1.2 + 2.3) + x)) * ((1.2 + 2.3) + x))"
```

---

## Performance

It turns out that `float -> float` functions are fast

Who knew?

--

```fsharp
let make a b c d : float -> float =
    fun x -> (c - d) * (a + b + x) * (a + b + x)
```

100,000,000 invocations takes ~300ms

--

&nbsp;

This will be our baseline

---

## Evaluation

```fsharp
let rec eval (expr : Expr) (x : float) : float =
    match expr with
    | Add (a, b) -> (eval a x) + (eval b x)
    | Sub (a, b) -> (eval a x) - (eval b x)
    | Mul (a, b) -> (eval a x) * (eval b x)
    | Div (a, b) -> (eval a x) / (eval b x)
    | Const a -> a
    | Var -> x
```

Completely naive

--

&nbsp;

How does it fare?

--

**~12 times slower**

---

## Being smarter

--

We've not made use of the 2 phases

Let's take advantage of that

--

```fsharp
let rec impl (expr : Expr) : float -> float =
    match expr with
    | Add (a, b) ->
        let a = impl a
        let b = impl b
        fun x -> a x + b x
    | Sub (a, b) -> ...
    | Mul (a, b) -> ...
    | Div (a, b) -> ...
    | Const a ->
        fun _ -> a
    | Var -> fun x -> x
```

---

## Being smarter

--

```fsharp
// phase 1 - startup
let description : Expr = make 1.2 2.3 3.4 4.5
let implementation : float -> float = impl description

// phase 2 - hot path
for _ in 1..100_000_000 do
    ignore <| implementation 98.7
```

--

Not perfect

Each operation is a method call

--

&nbsp;

How does it fare?

--

**~6 times slower**

---

## What next?

Not immediately obvious how to make our implementation faster

--

&nbsp;

How else could we make this faster then?

--

Similar optimisations to those we hand-crafted

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
    | Sub (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const a, Const b -> Const (a - b)
        | a, b -> Sub (a, b)
    | Mul (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const a, Const b -> Const (a * b)
        | a, b -> Mul (a, b)
    | Div (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const a, Const b -> Const (a / b)
        | a, b -> Div (a, b)
    | Const a -> Const a
    | Var -> Var
```

---

## Implementation

```fsharp
let rec implement (expr : Expr) : float -> float =
    match expr with
    | Add (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x + b x
    | Sub (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x - b x
    | Mul (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x * b x
    | Div (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x / b x
    | Const a ->
        fun _ -> a
    | Var -> fun x -> x
```

---

## Recursion

There's a lot of boilerplate here

--

&nbsp;

Much of the logic is just in recursing

--

&nbsp;

What if we could commonise all of this recursion?

Doing it right once and for all

???

boilerplate leads to bugs

should make tail recusive

---

## Recursion

```fsharp
let rec go
    (expr : Expr)
    : 'a =
    match expr with
    | Add (a, b) ->
        failwith "What now?"
    | _ -> failwith ""
```

---

## Recursion

```fsharp
let rec go
    (expr : Expr)
    : 'a =
    match expr with
    | Add (a, b) ->
        let a' : 'a = go a
        let b' : 'a = go b
        failwith "What now?"
    | _ -> failwith ""
```

???

recurse for both

---

## Recursion

```fsharp
let rec go
    (add : 'a -> 'a -> 'a)
    (expr : Expr)
    : 'a =
    match expr with
    | Add (a, b) ->
        let a' : 'a = go add a
        let b' : 'a = go add b
        add a' b'
    | _ -> failwith ""
```

???

a new input for what to do

---

## Recursion

```fsharp
let rec go
    (add : 'a -> 'a -> 'a)
    (sub : 'a -> 'a -> 'a)
    (mul : 'a -> 'a -> 'a)
    (div : 'a -> 'a -> 'a)
    (const : float -> 'a)
    (var : 'a)
    (expr : Expr)
    : 'a =
    let recurse = go add sub mul div const var
    match expr with
    | Add (a, b) -> add (recurse a) (recurse b)
    | Sub (a, b) -> sub (recurse a) (recurse b)
    | Mul (a, b) -> mul (recurse a) (recurse b)
    | Div (a, b) -> div (recurse a) (recurse b)
    | Const v -> const v
    | Var -> var
```

---

## Recursion

```fsharp
type Cata<'a> =
    abstract Add : 'a -> 'a -> 'a
    abstract Sub : 'a -> 'a -> 'a
    abstract Mul : 'a -> 'a -> 'a
    abstract Div : 'a -> 'a -> 'a
    abstract Const : float -> 'a
    abstract Var : 'a
```

All those arguments are a bit unwieldy

Define a type to hold the transformations

---

## Recursion

```fsharp
let rec cata (c : Cata<'a>) (expr : Expr) : 'a =
    match expr with
    | Add (a, b) -> c.Add (cata c a) (cata c b)
    | Sub (a, b) -> c.Sub (cata c a) (cata c b)
    | Mul (a, b) -> c.Mul (cata c a) (cata c b)
    | Div (a, b) -> c.Div (cata c a) (cata c b)
    | Const a -> c.Const a
    | Var -> c.Var
```

This is called a catamorphism

Or a *cata* for short

???

work hard to ensure tail recursive &c

---

## Implement - revisited

Original

```fsharp
let rec implement (expr : Expr) : float -> float =
    match expr with
    | Add (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x + b x
    | Sub (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x - b x
    | Mul (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x * b x
    | Div (a, b) ->
        let a = implement a
        let b = implement b
        fun x -> a x / b x
    | Const a ->
        fun _ -> a
    | Var -> fun x -> x
```

---

## Implement - revisited

With catamorphism

```fsharp
let implCata =
    { new Cata<float -> float> with
        member __.Add a b = fun x -> a x + b x
        member __.Sub a b = fun x -> a x - b x
        member __.Mul a b = fun x -> a x * b x
        member __.Div a b = fun x -> a x / b x
        member __.Const a = fun x -> a
        member __.Var     = fun x -> x
    }

let impl expr = cata implCata expr
```

---

## Constant folding - revisited

Original

```fsharp
let rec foldConstants (expr : Expr) : Expr =
    match expr with
    | Add (a, b) ->
        let a = foldConstants a
        let b = foldConstants b
        match a, b with
        | Const a, Const b -> Const (a + b)
        | a, b -> Add (a, b)
    | ...
    | Const a -> Const a
    | Var -> Var
```

---

## Constant folding - revisited

With catamorphism

```fsharp
let foldConstantsCata =
    { new Cata<Expr> with
        member __.Add a b =
            match a,b with
            | Const a, Const b -> Const (a + b)
            | a, b -> Add (a, b)
        ...
        member __.Const a = Const a
        member __.Var     = Var
    }

let foldConstants expr = cata foldConstantsCata expr
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

Let's eyeball it

```
> make 1.2 2.3 3.4 4.5 |> print
val it : string = "(((3.4 - 4.5) * ((1.2 + 2.3) + x)) * ((1.2 + 2.3) + x))"
```

```
> make 1.2 2.3 3.4 4.5 |> foldConstants |> print
val it : string = "((-1.1 * (3.5 + x)) * (3.5 + x))"
```

---

## Constant folding

In reality we'd write PBTs to test that

```fsharp
expr |> eval === expr |> foldConstants |> eval
```

We can use the same technique to test our different evaluators

```fsharp
expr |> interpret === expr |> implement |> invoke
```

---

## Constant folding

```fsharp
// phase 1 - startup
let description : Expr = make 1.2 2.3 3.4 4.5
let optimised : Expr = foldConstants description
let implementation : float -> float = impl optimised

// phase 2 - hot path
for _ in 1..100_000_000 do
    ignore <| implementation 98.7
```

--

&nbsp;

How does it fare?

--

**~4 times slower**

---

## Other optimisations

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

### Do nothing - can be removed
`* 1.0`, `+ 0.0`

### Make the computation irrelavent
`* 0.0`, `x / x`

---

## Remove stupid operations

```fsharp
let removeStupidOpsCata =
    { new Cata<Expr> with
        member __.Add a b =
            match a,b with
            | Const 0.0, a | a, Const 0.0 -> a
            | a, b -> Add (a, b)
        member __.Sub a b = ...
        member __.Mul a b =
            match a,b with
            | Const 0.0, _ | _, Const 0.0 -> Const 0.0
            | Const 1.0, a | a, Const 1.0 -> a
            | a, b -> Mul (a, b)
        member __.Div a b =
            match a,b with
            | a, Const 1.0 -> a
            | a, b when a = b -> Const 1.0
            | a, b -> Div (a, b)
        member __.Const a = Const a
        member __.Var = Var
    }
let removeStupidOps expr = cata removeStupidOpsCata expr
```

---

## Combine optimisations

```fsharp
make 4.5 4.5 1.2 3.2 |> print
val it : string = "(((4.5 - 4.5) * ((1.2 + 3.2) + x)) * ((1.2 + 3.2) + x))"

make 4.5 4.5 1.2 3.2 |> foldConstants |> print
val it : string = "((0.0 * (4.4 + x)) * (4.4 + x))"

make 4.5 4.5 1.2 3.2 |> foldConstants |> removeStupidOps |> print
val it : string = "0.0"
```

---

// This ISN'T ACTUALLY VALID
// Consider NaN.

---

## Combine optimisations

```fsharp
let optimisations =
    [
        foldConstants
        removeStupidOps
        // ...
    ]
```

```fsharp
let optimise f =
    optimisations |> List.fold (fun expr opt -> opt expr) f
    //optimisations |> List.fold (|>) f

let optimiseAndImplement expr : (float -> float) = expr |> optimise |> implement
```

---

## More optimisations

The list is large


- Common sub-expression - variable extraction
- Mult (a, a) -> Pow (a, 2) (extend the algebra)
- Show adding an algebra case that's not publicly accessible?

---

// TODO: show a mega expression that reduces lots, but not completely

---

## Must go faster

TODO: insert Jeff Goldblum

---

## Must go faster

```fsharp
type ILOp =
    | ILAdd
    | ILSub
    | ILMul
    | ILDiv
    | ILConst of float
    | ILVar
```

These opcodes consume from the stack

We assume the inputs are the top elements on the stack

---

## Must go faster

```fsharp
let ilCata =
    { new Cata<ILOp list> with
        member __.Add a b = a @ b @ [ ILAdd ]
        member __.Sub a b = a @ b @ [ ILSub ]
        member __.Mul a b = a @ b @ [ ILMul ]
        member __.Div a b = a @ b @ [ ILDiv ]
        member __.Const a = [ ILConst a ]
        member __.Var = [ ILVar ]
    }

let opsToEmit = cata ilCata
```

---

## Must go faster

```fsharp
let commitOp (ilGen : ILGenerator) (op : ILOp) =
    match op with
    | ILAdd -> ilGen.Emit OpCodes.Add
    | ILSub -> ilGen.Emit OpCodes.Sub
    | ILMul -> ilGen.Emit OpCodes.Mul
    | ILDiv -> ilGen.Emit OpCodes.Div
    | ILConst a -> ilGen.Emit (OpCodes.Ldc_R8, a)
    | ILVar -> ilGen.Emit OpCodes.Ldarg_0
```

---

## Must go faster

```fsharp
// IL emission requires a 'proper' delegate type
type FloatFloat = delegate of float -> float

let compile (expr : Expr) : float -> float =
    // Make a new method to emit il for
    let dm = DynamicMethod ("f", typeof<float>, [| typeof<float> |])
    let ilGenerator = dm.GetILGenerator ()

    // Get the IL to emit, and emit it!
    expr |> opsToEmit |> Seq.iter (commitOp ilGenerator)

    // Add a return
    ilGenerator.Emit OpCodes.Ret

    // Make this bad boy!
    let f = dm.CreateDelegate(typeof<FloatFloat>) :?> FloatFloat
    f.Invoke
```

---

## Must go faster

```fsharp
fun x -> x + 1.0
```

--

```yaml
From linqpad:
IL_0000:  ldarg.0
IL_0001:  ldc.r8      00 00 00 00 00 00 F0 3F
IL_000A:  add
IL_000B:  ret
```

--

```fsharp
let add1 = Var + Const 1.0
opsToEmit add1
val it : ILOp list = [ILVar; ILConst 1.0; ILAdd]
```

---
```fsharp
// Check out some more
Functions.times2 |> IL.opsToEmit
Functions.div3 |> IL.opsToEmit

// Something more complicated
// It's 'identical' to the IL of the compiled one!
Functions.g |> IL.opsToEmit

let o = Functions.div3 |> IL.compile
o 6.0
```

---

```fsharp
let ilUnopt = IL.compile Functions.g

// We're actually _marginally_ faster than the original!
// Even with no optimisations.

#time
for _ in 0..100_000_000 do
    ignore <| ilUnopt 12.3
#time

---

// Why?
// I suspect field loading from the closure rather than constants.


let ilOpt = Functions.g |> megaOptimise |> IL.compile

#time
for _ in 0..100_000_000 do
    ignore <| ilOpt 12.3
#time
```