// Cover HOFs
// let f a b = a + b === let f a = fun b -> a + b

// A float -> float function for the hot path
// Parameterised by runtime values
// We can accept slow start-up, but not slow hot-path

module Imperative =

    // no place to do slow up-front work
    let make a b c d (x : float) =
        (c - d) * (a + b + x) * (a + b + x)

    let make1' a b c d =
        // do all the work you like here
        fun (x : float) ->
            // be as fast as possible here
            (c - d) * (a + b + x) * (a + b + x)

    // You can now hand-optimise! (If you hate yourself)
    // What if the code changes?
    // Did you cover all the optimisations?
    // Were your refactorings correct?
    // Can you still understand the logic?
    let make1'' a b c d =
        let c1 = c - d
        let c2 = a + b
        fun (x : float) ->
            c1 * (c2 + x) * (c2 + x)

// A better way...

// A declarative approach

type Expr =
    | Add of Expr * Expr
    | Sub of Expr * Expr
    | Mul of Expr * Expr
    | Div of Expr * Expr
    | Const of float
    | Var

// open this to get the operators in scope
module Ops =
    let (+) a b = Add (a, b)
    let (-) a b = Sub (a, b)
    let (*) a b = Mul (a, b)
    let (/) a b = Div (a, b)

module Decalarative =
    open Ops

    let make a b c d =
        // we need to lift the floats to constants in our algebra
        let a = Const a
        let b = Const b
        let c = Const c
        let d = Const d
        let x = Var
        (c - d) * (a + b + x) * (a + b + x)

module Functions =
    open Ops

    let private x = Var

    let f = Imperative.make 1.2 3.2 4.5 4.5
    let g = Decalarative.make 1.2 3.2 4.5 4.5

    let add1 = x + (Const 1.0)

    let times2 = x * (Const 2.0)

    let div3 = x / (Const 3.0)


(*
f is inscrutable
You are relying on the compiler's optimisations
But they only happen at JIT - it doesn't know about the two phases

g is useless!
It's just a description of what we want to do
You can't actually evaluate it yet
*)

// Benchmark the imperative version

#time
for _ in 0..100_000_000 do
    ignore <| Functions.f 12.3
#time


// How to evaluate an expression?
// Let's be completely naive

module Interpret =

    let rec eval expr x =
        match expr with
        | Add (a, b) -> (eval a x) + (eval b x)
        | Sub (a, b) -> (eval a x) - (eval b x)
        | Mul (a, b) -> (eval a x) * (eval b x)
        | Div (a, b) -> (eval a x) / (eval b x)
        | Const a -> a
        | Var -> x

// ~12 times slower

#time
for _ in 0..100_000_000 do
    ignore <| Interpret.eval Functions.g 12.3
#time

// Can we be smarter about this?

// We said there were two phases, so let's take advantage of that

module Compile =

    let rec impl expr =
        match expr with
        | Add (a, b) ->
            // can be slow here
            let a = impl a
            let b = impl b
            fun x ->
                // must be fast here
                a x + b x
        | Sub (a, b) ->
            let a = impl a
            let b = impl b
            fun x -> a x - b x
        | Mul (a, b) ->
            let a = impl a
            let b = impl b
            fun x -> a x * b x
        | Div (a, b) ->
            let a = impl a
            let b = impl b
            fun x -> a x / b x
        | Const a ->
            fun _ -> a
        | Var -> fun x -> x

(*

fun x -> (a + b) * x
===
fun x ->
    (fun x ->
        (fun _ -> a) x
        +
        (fun _ -> b) x
    ) x
    *
    (fun x -> x) x
===
let getA x = a
let getB x = b
let getAB x = getA x + getB x
let getX x = x
fun x -> getAB x + getX x

Not perfect, lots of calls
How does it fare?
*)

// phase 1: compile
// we don't care how long this takes (within reason)
let compiled = Compile.impl Functions.g

// phase 2: execute
// this needs to be fast

// ~6 times slower

#time
for _ in 0..100_000_000 do
    ignore <| compiled 12.3
#time

// Let's park that for a moment, since it's not obvious how to implement something better
// (hint: we'll see later how to do exactly that)

// How else could we make this faster - knowing about the two phases?
// Well, we could do some optimisations that the JIT couldn't.

// e.g. constant folding (like we tried to do manually above)

module ConstantFold1 =

    let rec go (expr : Expr) : Expr =
        match expr with
        | Add (a, b) ->
            let a = go a
            let b = go b
            match a, b with
            | Const a, Const b -> Const (a + b)
            | a, b -> Add (a, b)
        | Sub (a, b) ->
            let a = go a
            let b = go b
            match a, b with
            | Const a, Const b -> Const (a - b)
            | a, b -> Sub (a, b)
        | Mul (a, b) ->
            let a = go a
            let b = go b
            match a, b with
            | Const a, Const b -> Const (a * b)
            | a, b -> Mul (a, b)
        | Div (a, b) ->
            let a = go a
            let b = go b
            match a, b with
            | Const a, Const b -> Const (a / b)
            | a, b -> Div (a, b)
        | Const a -> Const a
        | Var -> Var

// Two questions arise:
// 1. Is this the transformation we wanted?
// 2. Is this a valid transformation?

// This look a lot like our compile function.
// Most of the logic is just in recursing.
// Boilerplate sucks. Potential for introducing bugs.
// We should probably make these tail recursive to avoid stack overflows.
// How boring.

// What if we could commonise all of this recursion?
// And do it right once and for all.

module Recursion =

    let rec go1 (expr : Expr) : 'a =
        match expr with
        | Add (a, b) ->
            failwith "What now?"
        | _ -> failwith ""

    let rec go2 (expr : Expr) : 'a =
        match expr with
        | Add (a, b) ->
            // recurse for both
            let a' = go2 a
            let b' = go2 b
            failwith "What now?"
        | _ -> failwith ""

    let rec go3
        (add : 'a -> 'a -> 'a)
        (expr : Expr)
        : 'a
        =
        match expr with
        | Add (a, b) ->
            // recurse for both
            // take a new input for what to do
            // pass inputs along
            let a' = go3 add a
            let b' = go3 add b
            add a' b'
        | _ -> failwith ""

    let rec go4
        (add : 'a -> 'a -> 'a)
        (sub : 'a -> 'a -> 'a)
        (expr : Expr)
        : 'a
        =
        let recurse = go4 add sub
        match expr with
        | Add (a, b) -> add (recurse a) (recurse b)
        | Sub (a, b) -> sub (recurse a) (recurse b)
        // ...
        | _ -> failwith ""

    // add, sub, mul, div are all : 'a -> 'a -> 'a
    // const : float -> 'a
    // var : 'a

// This is called a catamorphism. Or Cata for short.
// Rather than passing in the functions, we make a type for them.
// Just more sane that passing loads of args
// Written as an interface, but here could use a record of functions

type Cata<'a> =
    abstract Add : 'a -> 'a -> 'a
    abstract Sub : 'a -> 'a -> 'a
    abstract Mul : 'a -> 'a -> 'a
    abstract Div : 'a -> 'a -> 'a
    abstract Const : float -> 'a
    abstract Var : 'a

let rec cata (c : Cata<'a>) (expr : Expr) : 'a =
    // factored out recursion to here
    // can work hard to e.g. make tail recursive &c
    // and all uses share that work!
    match expr with
    | Add (a, b) -> c.Add (cata c a) (cata c b)
    | Sub (a, b) -> c.Sub (cata c a) (cata c b)
    | Mul (a, b) -> c.Mul (cata c a) (cata c b)
    | Div (a, b) -> c.Div (cata c a) (cata c b)
    | Const a -> c.Const a
    | Var -> c.Var


module CataCompile =

    // Compare to the previous compiling code:

    // let rec impl expr =
    //     match expr with
    //     | Add (a, b) ->
    //         let a = impl a
    //         let b = impl b
    //         fun x -> a x + b x
    //     | Sub (a, b) ->
    //         let a = impl a
    //         let b = impl b
    //         fun x -> a x - b x
    //     | Mul (a, b) ->
    //         let a = impl a
    //         let b = impl b
    //         fun x -> a x * b x
    //     | Div (a, b) ->
    //         let a = impl a
    //         let b = impl b
    //         fun x -> a x / b x
    //     | Const a -> fun _ -> a
    //     | Var     -> fun x -> x

    let implCata =
        { new Cata<float -> float> with
            member __.Add a b = fun x -> a x + b x
            member __.Sub a b = fun x -> a x - b x
            member __.Mul a b = fun x -> a x * b x
            member __.Div a b = fun x -> a x / b x
            member __.Const a = fun x -> a
            member __.Var     = fun x -> x
        }

    let compile expr = cata implCata expr

let h' = CataCompile.compile Functions.g

// identical speed to non-cata version (as they're identical!)

#time
for _ in 0..100_000_000 do
    ignore <| h' 12.3
#time


// We said earlier:

// Two questions arise:
// 1. Is this the transformation we wanted?
// 2. Is this a valid transformation?


// Eye-ball it.

module Print =

    let printCata =
        { new Cata<string> with
            member __.Add a b = sprintf "(%s + %s)" a b
            member __.Sub a b = sprintf "(%s - %s)" a b
            member __.Mul a b = sprintf "(%s * %s)" a b
            member __.Div a b = sprintf "(%s / %s)" a b
            member __.Const a = sprintf "%.1f" a
            member __.Var     = "x"
        }

    let print expr = cata printCata expr

Print.print Functions.g

Decalarative.make 1.2 2.3 3.4 4.5 |> Print.print

// This does a decent job of the first point,
// but is hardly a long-term solution for the second.

// We can test that:
// expr |> eval === expr |> optimise |> eval

// We can use the same technique to test our compiler:
// expr |> interpret === expr |> compile |> invoke


module ConstantFold =

    // doesn't really handle all things (e.g. 2 * 3 * x * 4 -> 6 * x * 4)
    // n-ary multiplication &c would make this easier
    let constFoldCata =
        { new Cata<Expr> with
            member __.Add a b =
                match a,b with
                | Const a, Const b -> Const (a + b)
                | a, b -> Add (a, b)
            member __.Sub a b =
                match a,b with
                | Const a, Const b -> Const (a - b)
                | a, b -> Sub (a, b)
            member __.Mul a b =
                match a,b with
                | Const a, Const b -> Const (a * b)
                | a, b -> Mul (a, b)
            member __.Div a b =
                match a,b with
                | Const a, Const b -> Const (a / b)
                | a, b -> Div (a, b)
            member __.Const a = Const a
            member __.Var     = Var
        }

    let optimise expr = cata constFoldCata expr

Decalarative.make 1.2 2.3 3.4 4.5 |> Print.print
Decalarative.make 1.2 2.3 3.4 4.5 |> optimise |> Print.print

Functions.g |> Print.print
Functions.g |> cata ConstantFold.constFoldCata |> Print.print

let i = Functions.g |> ConstantFold.optimise |> CataCompile.compile

// ~4 times slower

#time
for _ in 0..100_000_000 do
    ignore <| i 12.3
#time


// It's a bit silly that we're multiplying the whole expression by zero!

module PointlessOperation =
    // bad for nan obvs
    let goCata =
        { new Cata<Expr> with
            member __.Add a b =
                match a,b with
                | Const 0.0, a
                | a, Const 0.0 -> a
                | a, b -> Add (a, b)
            member __.Sub a b =
                match a,b with
                | a, Const 0.0 -> a
                | a, b when a = b -> Const 0.0
                | a, b -> Sub (a, b)
            member __.Mul a b =
                match a,b with
                | Const 0.0, _
                | _, Const 0.0
                    -> Const 0.0
                | Const 1.0, a
                | a, Const 1.0
                    -> a
                | a, b -> Mul (a, b)
            member __.Div a b =
                match a,b with
                | a, Const 1.0 -> a
                | a, b when a = b -> Const 1.0
                | a, b -> Div (a, b)
            member __.Const a = Const a
            member __.Var = Var
        }

    let optimise expr = cata goCata expr

let optimisations =
    [
        ConstantFold.optimise
        PointlessOperation.optimise
        // ...
    ]

// look at the optimisations
Functions.g |> Print.print
Functions.g |> ConstantFold.optimise |> Print.print
Functions.g |> ConstantFold.optimise |> PointlessOperation.optimise |> Print.print
Functions.g |> PointlessOperation.optimise |> Print.print

// This ISN'T VALID
// Consider NaN.

let megaOptimise f =
    optimisations |> List.fold (fun expr opt -> opt expr) f
    //optimisations |> List.fold (|>) f

let j = Functions.g |> megaOptimise |> CataCompile.compile

#time
for _ in 0..10_000_000 do
    ignore <| j 12.3
#time

// More optimisations:

// Common sub-expression - variable extraction
// Mult (a, a) -> Pow (a, 2)
// Show adding an algebra case that's not publicly accessible?


// TODO: show a mega expression that reduces lots, but not completely


// IL emiting

open System.Reflection.Emit

type ILOp =
    | ILAdd
    | ILSub
    | ILMul
    | ILDiv
    | ILConst of float
    | ILVar

module IL =

    // These opcodes consume from the stack
    // We assume the inputs are the top elements on the stack

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

    let commitOp (ilGen : ILGenerator) (op : ILOp) =
        match op with
        | ILAdd -> ilGen.Emit OpCodes.Add
        | ILSub -> ilGen.Emit OpCodes.Sub
        | ILMul -> ilGen.Emit OpCodes.Mul
        | ILDiv -> ilGen.Emit OpCodes.Div
        | ILConst a -> ilGen.Emit (OpCodes.Ldc_R8, a)
        | ILVar -> ilGen.Emit OpCodes.Ldarg_0

    // IL emission requires a 'proper' delegate type
    type private FloatFloat = delegate of float -> float

    let compile expr =
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

// Let's see the opcodes
Functions.add1 |> IL.opsToEmit

(*
From linqpad:
IL_0000:  nop
IL_0001:  ldarg.0
IL_0002:  ldc.r8      00 00 00 00 00 00 F0 3F
IL_000B:  add
IL_000C:  ret
*)

// Check out some more
Functions.times2 |> IL.opsToEmit
Functions.div3 |> IL.opsToEmit

// Something more complicated
// It's 'identical' to the IL of the compiled one!
Functions.g |> IL.opsToEmit

let o = Functions.div3 |> IL.compile
o 6.0


let ilUnopt = IL.compile Functions.g

// We're actually _marginally_ faster than the original!
// Even with no optimisations.

#time
for _ in 0..100_000_000 do
    ignore <| ilUnopt 12.3
#time

// Why?
// I suspect field loading from the closure rather than constants.


let ilOpt = Functions.g |> megaOptimise |> IL.compile

#time
for _ in 0..100_000_000 do
    ignore <| ilOpt 12.3
#time