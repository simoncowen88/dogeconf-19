
## Quick and dirty

Sometimes the answer is elegant, and sometimes you've got to go full Rilke:

> Every normal man must be tempted, at times, to spit on his hands, hoist the black flag, and begin slitting throats.

So let's talk about how the built-in `list` type is implemented.

```fsharp
type List<'a> =
    private
    | Nil
    | Cons of 'a * List<'a>

module List =

    let empty = Nil

    let cons x xs = Cons (x, xs)

    let rec map1 f xs k =
        match xs with
        | Nil -> k Nil
        | Cons (x, xs) ->
            map1 f xs (fun ys -> k <| Cons ((f x), ys))

    let map (f : 'a -> 'b) (xs : List<'a>) : List<'b> =
        map1 f xs id

[<AutoOpen>]
module ListOperators =
    let (^) x xs = List.cons x xs

let a = 1 ^ 2 ^ 3 ^ List.empty

let b = a |> List.map ((+) 1)

(*
    What is k?
    - k0 = id
    - k1 = fun xs -> k0 (f 1 :: xs)
         = fun xs -> id (f 1 :: xs)
    - k2 = fun xs -> k1 (f 2 :: xs)
         = fun xs -> id (f 1 :: (f 2 :: xs))
    ...
    - k4 = fun xs -> id (f 1 :: (f 2 :: (f 3 :: (f 3 :: xs))))

    We hit the Nil case and invoke with Nil, giving:
    This is where all of the work happens, all we've been doing
    so far is building up the function that will do the work

    - k4 Nil = id (f 1 :: (f 2 :: (f 3 :: (f 3 :: Nil))))
             = 2 :: 3 :: 4 :: Nil
*)
```

The reality is more like:

```fsharp
type List<'a> =
    private
    | Nil
    | Cons of 'a * mutable List<'a>
```

Note that this doesn't actually compile.

What is happening?
We create the head of the list, then mutate the cons tail as we iterate through the input list.

- ret = f 1 :: Nil
- ret = f 1 :: f 2 :: Nil
- ret = f 1 :: f 2 :: f 3 :: Nil

A constructed list is never mutated in this way.
This is used only for internal construction of lists, thus retains
all visible safety of more-immutable, but slower implementations.

Moreover, this actually uses some feature that are only valid within FSharp.Core.
Don Syme doesn't trust enough to not fuck it up.

```fsharp
    // optimized mutation-based implementation. This code is only valid in fslib, where mutation of private
    // tail cons cells is permitted in carefully written library code.
    let inline setFreshConsTail cons t = cons.( :: ).1 <- t
    let inline freshConsNoTail h = h :: (# "ldnull" : 'T list #)
```

---
