
## Quick and dirty

Sometimes the answer is elegant, and sometimes you've got to go full Rilke...

.quote[
> Every normal man must be tempted, at times, to spit on his hands, hoist the black flag, and begin slitting throats.
]

--

&nbsp;

So let's talk about how the built-in `list` type is implemented.

---

## Lists

```fsharp
type List<'a> =
    private
    | Nil
    | Cons of 'a * List<'a>
```

How would you implement `List.map`?

Recursively, of course.

And if you went to Toby's talk, with CPS so you can handle long lists with S/O.

--

Is that what really happens?

--

Hell no!

---

## List.map

```fsharp
type List<'a> =
    private
    | Nil
    | Cons of 'a * List<'a>
```

First we need to realise that this isn't quite how the type looks.

---

## List.map

```fsharp
type List<'a> =
    private
    | Nil
    | Cons of 'a * mutable List<'a>
```

(Note that this doesn't actually compile.)

This is 'safe' as we've restricted access to the constructors.

---

## List.map

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

## Islands os insanity

More generally, this is a way in which we push F# preformance.

Judiciously applying mad crap like this, but always in hidden, well-contained, _extremely_ well-tested sections.

If the mutability leak at all, then you've fucked it.

---
