
## Inlining

???

get for free in F#

--

Wikipedia\:

.quote[
> In computing, inline expansion, or inlining, is a manual or compiler optimization that replaces a function call site with the body of the called function.
]

--

- No function call
  - No need to pass arguments
  - Can improve locality of reference
- Allows for further optimisations
  - e.g. constant propagation / escape analysis

--

F\# is very aggressive with its inlining.

???

Even across assembly boundaries

---

## Inlining example

```fsharp
let f a = 2 * a
let g a = f (a + 1)
```

&nbsp;

What does the compiled code for `g` actually look like?

---

## Inlining example - no optimisations

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

```yaml
g:
IL_0000:  ldc.i4.2  # load 2              [1]
IL_0001:  ldarg.0   # load first argument [a;2]
IL_0002:  ldc.i4.1  # load 1              [1;a;2]
IL_0003:  add       # add                 [a+1;2]
IL_0004:  mul       # multiply            [2*(a+1)]
IL_0005:  ret
```

---

## Inlining example - analysis

Remember the original code:

```fsharp
let f a = 2 * a
let g a = f (a + 1)
```

The call site of `f` in `g` was replaced by the body `f`.

This leaves `g` looking like:

```fsharp
let g a = 2 * (a + 1)
```

And in fact, these compile to the exact same IL.

---
