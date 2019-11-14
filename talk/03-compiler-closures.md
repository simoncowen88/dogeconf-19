
class: center, middle

# Closures

---

## Closures - capture detection

```fsharp
let go () =
  let f a = a + 1
  ...
```

--

which is really...

```fsharp
let go () =
  let f = f_impl ()
  ...
```

--

_could_ be transformed safely to...

```fsharp
let f = f_impl ()
let go () = ...
```

???

but isn't

will allocate each time you call `go`

---

## Closures

If you find yourself desperately in need of allocating a closure up-front.

You can allocate at the scope at which all captured variables are known.

If there are none, then you can allocate statically.

---

## Closures

```fsharp
let add1 (xs : int list) = xs |> List.map (fun a -> a + 1)
```

becomes...

```fsharp
let mapper a = a + 1
let add1 xs = xs |> List.map mapper
```

--

Sweet. That was easy.

--

Wrong! This doesn't work.

--

&nbsp;

`mapper` here is a method, not a closure.

So F\# has to turn this into an object that it can pass around first - i.e. a closure!

???

it captures nothing - but f# can't detect that

---

## Closures

```fsharp
let mapper = fun a -> a + 1
let add1 xs = xs |> List.map mapper
```

--

Still no!

`mapper` here is a declared as a lambda, so you might assume it's already a closure.

For good or ill F\# has optimised this by converting this to a method for you!

So it's identical to our previous failure.

---

## Closures

```fsharp
let mapper = id <| fun a -> a + 1
let add1 xs = xs |> List.map mapper
```

Finally!

We *force* a closure to be created and stored, by passing our lambda through `id`.

Now F\# *can't work out* how to optimise this to a method.

---

## Closures - Was it worth it?

Some rough benchmarking suggests that for the above case, forcing the closure to be
allocated up front reduces about 4 nanoseconds of overhead per call.

Whether this is worth it depends on yur use case.

However, we'll soon see that this attempt at 'tricking' the compiler into doing what
we thought would be best can block other optimisations, actually hurting our performance
significantly.

Can be useful for writing allocation tests.

---
