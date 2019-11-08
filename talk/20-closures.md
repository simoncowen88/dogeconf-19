
## Closures

Wikipedia says...

.quote[
> ... a technique for implementing lexically scoped name binding in a language with first-class functions
>
> &nbsp;
>
> Operationally, a closure is a record storing a function together with an environment.
>
> &nbsp;
>
> The environment is a mapping associating each free variable of the function (variables that are used locally, but defined in an enclosing scope) with the value or reference to which the name was bound when the closure was created. Unlike a plain function, a closure allows the function to access those captured variables through the closure's copies of their values or references, even when the function is invoked outside their scope.
]

---

## Closures - behind the curtain

```fsharp
let f a b = a + b
let g = f 1
g 3
```

What's actually going on here?

--

&nbsp;

What *is* `g`?

--

&nbsp;

How does it have access to `a`?

???

No `this` parameter like in C#

---

## Closures - behind the curtain


```fsharp
let f a b = a + b
let g = f 1
g 3
```

is really...

```fsharp
type g_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b
```

--

```csharp
internal sealed class g_impl : FSharpFunc<int, int> {
    private int a;
    internal g_impl(int a) { this.a = a; }
    public override int Invoke(int b) { return a + b; }
}
```

--

```fsharp
let f a b = a + b
let g = new g_impl (a)
g.Invoke 3
```

---

## Closures - closures everywhere

Functional programming leads to passing around a lot of functions due to the
prevalance of higher-order functions (e.g. List.map, Seq.fold, memoise).

(a higher-order function is anything with a functino as a parameter)

If these HOFs are being invoked on the hot path you may allocate closure
for each invocation.

F\# doesn't have closure detection - so even if your closure doesn't capture anything,
and thus could be allocataed statically, F\# doesn;t realise this.

---

## Closures - too clever for me

```fsharp
let sum a b = a + b

// HOF
let hof f = f 1

// HOT PATH
let go a = hof (sum a)
```

This actually optimises (due to inlining) to:

```fsharp
let go a = a + 1
```

Which is normally useful, but isn't what I'm trying to show here!

---

## Closures - behind the curtain

Let's stop any inlining.

```fsharp
let sum a b = a + b

[<MethodImpl(MethodImplOptions.NoInlining)>]
let hof f = f 1

let go a = hof (sum a)
```

Now, how does `go` compile?

```yaml
go:
IL_0000:  ldarg.0                       # load first argument          [a]
IL_0001:  newobj      Bork+go@6..ctor   # allocate a new go@6 (eh?!)   [?]
IL_0008:  call        Bork.f<Int32>     # invoke f                     [f ?]
IL_000D:  ret
```

So what is this `go@6` type?

(Named as it is declared inside `go` at line 6)

---

## Closures - `go@6` home, you're drunk

In IL:

```yaml
go@6.Invoke:
IL_0000:  ldarg.0
IL_0001:  ldfld       Bork+go@6.a
IL_0006:  ldarg.1
IL_0007:  add
IL_0008:  ret

go@6..ctor:
IL_0000:  ldarg.0
IL_0001:  call        Microsoft.FSharp.Core.FSharpFunc<System.Int32,System.Int32>..ctor
IL_0006:  ldarg.0
IL_0007:  ldarg.1
IL_0008:  stfld       Bork+go@6.a
IL_000D:  ret
```

---

## Closures - `go@6` go power rangers

In C# (thanks to ILSpy):

```csharp
[Serializable]
internal sealed class go@6 : FSharpFunc<int, int>
{
    public int a;

    [CompilerGenerated]
    [DebuggerNonUserCode]
    internal go@6(int a)
    {
        this.a = a;
    }

    public override int Invoke(int b)
    {
        return a + b;
    }
}
```

---

## Closures - we want to `go@6` faster

If you find yourself desperately in need of allocating a closure up-front.

You can allocate at the scope at which all capture variables are known.
If there are none, then you can allocate statically.

```fsharp
let add1 xs = xs |> List.map (fun a -> a + 1)
```

---

## Closures - Jason Statham, go-`go@6` dancer

```fsharp
let mapper a = a + 1
let add1 xs = xs |> List.map mapper
```

Nope!

`mapper` here is a method, not a closure.
So F\# has to turn this into an object that it can pass around first - i.e. a closure!

---

## Closures - Round and round we `go@6`

```fsharp
let mapper = fun a -> a + 1
let add1 xs = xs |> List.map mapper
```

Still no!

`mapper` here is a declared as a lambda, so you might assume it's already a closure.
For good or ill F\# has optimised this by converting this to a method for you!

---

## Closures - Witty title involving `go@6`

```fsharp
let mapper = id <| fun a -> a + 1
let add1 xs = xs |> List.map mapper
```

Finally!

We *force* a closure to be created and stored, by passing our lambda through `id`.
For good or ill F\# has *cant work out* how to optimise this!

---

## Closures - Was it worth it?

Some rough benchmarking suggests that for the above case, forcing the closure to be
allocated up front reduces about 4 nanoseconds of overhead per call.

Whether this is worth it depends on yur use case.

However, we'll soon see that this attempt at 'tricking' the compiler into doing what
we thought would be best can block other optimisations, actually hurting our performance
significantly.

---
