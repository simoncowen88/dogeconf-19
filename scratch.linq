<Query Kind="FSharpProgram" />

open System.Runtime.CompilerServices
open System.Collections.Generic


//let sum a b = a + b
//
//[<MethodImpl(MethodImplOptions.NoInlining)>]
//let hof f = f 1
//
//let go a = hof (sum a)


//let add1 xs = xs |> List.map (fun a -> a + 1)

// 2 seconds for up-front allocation
// 6 seconds
//
//let mapper = fun a -> a + 1
//[<MethodImpl(MethodImplOptions.NoInlining)>]
//let hof f i = f i
//let add1 i = hof mapper i
//
//
//
//
//let s = System.Diagnostics.Stopwatch.StartNew ()
//s.Start ()
//s.Stop ()
//s.Start ()
//s.Stop ()
//s.Start ()
//s.Stop ()
//
//s.Restart ()
//let mutable ss = 0
//
//for i in 0..1_000_000_000 do
//    ss <- add1 i
//s.Stop ()
//
//s.Elapsed |> Dump
//    


//[<Struct>] type Foo = Foo of int * int
//
//let go () =
//    let f = Foo (1,3)
//    f


//let d = Dictionary<int, int> ()
//
//let go () =
//    match d.TryGetValue 1 with
//    | true, o -> o + 1
//    | _ -> 7
//    
//let go2 () =
//    let b,o = d.TryGetValue 1
//    match b,o with
//    | true, o -> o + 1
//    | _ -> 7
//    
//let go7 () =
//    let b, o = d.TryGetValue 1
//    if b then o else 7


let fold f a (xs: _ []) =
 let mutable a = a
 for i=0 to xs.Length-1 do
    a <- f a xs.[i]
 a

let inline fold_inline f a (xs: _ []) =
 let mutable a = a
 //for i=0 to xs.Length-1 do
 let l = xs.Length-1
 for i=0 to l do
    a <- f a xs.[i]
 a

//
//let xs = Array.init 1000 id
//[<MethodImpl (MethodImplOptions.NoInlining)>]
//let folder a b = a + b
//let folder' = id folder
//
//let go1 () =
//    fold_inline folder 0 xs
//
//let go1' () =
//    fold_inline (fun a b -> a + b) 0 xs
//
//let go2 () =
//    fold folder 0 xs
//
//let go3 () =
//    let mutable s = 0
//    for i in 0..10 do
//        s <- fold (fun a b -> a + b) 0 xs
//
//let go4 () =
//    let mutable s = 0
//    for i in 0..10 do
//        s <- fold folder 0 xs
//
//let go4' () =
//    let mutable s = 0
//    for i in 0..10 do
//        s <- fold folder' 0 xs
//
//// go1 becomes:
//let go1' () =
//    let mutable a = 0
//    for i=0 to xs.Length-1 do
//        a <- a + xs.[i]
//    a

    

let s = System.Diagnostics.Stopwatch.StartNew ()
s.Start ()
s.Stop ()
s.Start ()
s.Stop ()
s.Start ()
s.Stop ()

s.Restart ()
let mutable ss = 0
let xs = Array.init 1000 id

//[<MethodImpl (MethodImplOptions.NoInlining)>]
let folder' a b = a + b // this gets fully inlined
let folder = id folder' // this makes it worse! (the same as non-inlined) As it can't be inlined
// in fact, this appears to be the entire issue - getting this inlined.
for i in 0..1_000_000 do
    //ss <- fold (fun a b -> a + b) 0 xs
    
    ss <- fold_inline folder 0 xs
    
    // this is what actually gets compiled:
    //let mutable a = 0
    //for i=0 to xs.Length-1 do
    //    //a <- a + xs.[i]
    //    a <- folder a xs.[i]
    //ss <- a
s.Stop ()

s.Elapsed |> Dump
    
    
let f a = 2 * a 
let g a = f (a + 1)

let h a = 2 * (a + 1)