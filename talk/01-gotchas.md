
## Tuples

```
let thisIsATuple : int * string = (1, "foo")
```

Used extensively in F\#

???

Returning/passing multiple values
some replacement with anonymous records

--

### What is it?

Alias for `System.Tuple`

---

## Tuples

### Gotcha \#1

`System.Tuple` is a reference type.

So making a tuple allocates.

--

&nbsp;

F\# now has struct tuples.

```fsharp
let thisIsAStructTuple : (struct (int * string)) = struct (1, "foo")
```

---

## Tuples

### Gotcha \#2

```fsharp
type Foo =
    static member Bar : int * int -> int
```

--

```csharp
class Foo
{
    static int Bar(int a, int b)
    {
        // ...
    }
}
```

---

## Tuples

### Gotcha \#2

```fsharp
type Foo =
    static member Bar : int * int -> int
```

--

```fsharp
let x = Foo.Bar (2, 3) // doesn't allocate a tuple
```

--

```fsharp
let f : int * int -> int = Foo.Bar
```

--

```fsharp
let x = f (2, 3) // allocates a tuple!
```

---

## Option

```fsharp
let thisIsAnOption : int option = Some 3
```

Used extensively in F\#

???

for things that _might_ have a value

--

### What is it?

```fsharp
type 'a Option =
    | Some of 'a
    | None
```

---

## Option

### Gotcha \#1

`'a Option` is a reference type.

So making an option allocates.

--

&nbsp;

F\# now has struct options.

```fsharp
let thisIsAStructTuple : int ValueOption = ValueSome 3
```

---

## Closures

```fsharp
let f a b = a + b
let thisIsAClosure : int -> int = f 3
```

Used extensively in F\#

???

higher-order functions

function as a parameter

`List.map`, `Seq.fold`, `memoise`

--

### What is it?

--

Wait.

What actually _is_ a closure?

---

## Closures - behind the curtain

```fsharp
let f a b = a + b
let g = f 1
let x = g 3 // = 4
```

What's actually going on here?

--

&nbsp;

What *is* `g`?

--

&nbsp;

How does it have access to the `a` we passed in (as `1`)?

---

## Closures - behind the curtain


```fsharp
let f a b = a + b
let g = f 1
let x = g 3
```

is really...

```fsharp
type g_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b

let f a b = a + b
let g = new g_impl (a)
let x = g.Invoke 3
```

---

## Closures - behind the curtain


```fsharp
let f a b = a + b
let g = f 1
let x = g 3
```

```fsharp
type g_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b
```

```csharp
internal sealed class g_impl : FSharpFunc<int, int> {
    private int a;
    internal g_impl(int a) { this.a = a; }
    public override int Invoke(int b) { return a + b; }
}
```

???

now we know what's going on

---

## Closures

```fsharp
let f a b = a + b
let thisIsAClosure : int -> int = f 3
```

Used extensively in F\#

???

HOFs

--

```fsharp
type thisIsAClosure_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b
```

---

## Closures

### Gotcha \#1

Closures are reference types.

So making a closure allocates.

---

## Boxing

```fsharp
let thisIsABox : obj = box 3
```

```fsharp
let o : IComparable<DateTime> = upcast DateTime.Now
```

--

```csharp
object o = 3;
```

???

implicit in C\#

--

Value of unknown type.

List of things implementing dome interface.

---

## Boxing

```fsharp
let thisIsABox : obj = box 3
```

--

### What is it?

--

.quote[
  > To have a unified type system and allow value types to have a completely different representation of their underlying data from the way that reference types represent their underlying data (e.g., an int is just a bucket of thirty-two bits which is completely different than a reference type).
]

--

`obj` is a reference type.

We can't just upcast a value type in there.

We need some holder for it - that's a box.

---

## DateTime comparison

```
DateTime.Today = DateTime.Now
```

--

### What's the problem?

--

This allocates.

--

```fsharp
let o : IEquatable<DateTime> = upcast DateTime.Now
```

???

a box

---

## Map and Set

```fsharp
let thisIsAMap : Map<int, string> = [ 1, "one" ; 2, "two" ] |> Map.ofList
```

```fsharp
let thisIsASet : int Set = [ 1; 2; 3 ] |> Set.ofList
```

--

### What's the problem?

They are comparison-based (not hash-based).

--

Lookup and insert are `log n`.

--

Comparisons are often more expensive than hashes / equalities.

???

less able to short-circuit

--

&nbsp;

Map lookup allocates when `'key` is a value type.

---

## Enumerating

```fsharp
let xs = [1;2;3]
for x in xs do
    printfn "%d" x
```

--

### What's the problem?

--

This allocates.

--

Sometimes.

---

## Enumerating

How does it work?

--

```fsharp
type IEnumerator<'a> =
    abstract MoveNext : unit -> bool
    abstract Current : 'a

type IEnumerable<'a> =
    abstract GetEnumerator : unit -> IEnumerator<'a>
```

---

## Enumerating

How does it work?

```fsharp
for x in xs do
    printfn "%d" x
```

--

```fsharp
let enumerator : int IEnumerator = xs.GetEnumerator ()
while enumerator.MoveNext () do
    let x = enumerator.Current
    printfn "%d" x
```

---

## Enumerating

```fsharp
type IEnumerable<'a> =
    abstract GetEnumerator : unit -> IEnumerator<'a>
```

We can't help but create an object to return here.

???

surely it _always_ allocates?

--

Various core data types return enumerators that are structs.

But this doesn't help - we learned above that they get boxed.

.NET does some duck-typing here.

---

## Enumerating

Rules of thumb:

- Enumerating an `IEnumerable` or `seq` will allocate.
- Enumerating a core concrete type (e,g, `array`) will not allocate.

--

```fsharp
let go (a : int seq) =
    for i in a do
        ignore <| a
```

```fsharp
let go (a : int list) =
    for i in a do
        ignore <| a
```

```fsharp
let go (a : int array) =
    for i in a do
        ignore <| a
```

```fsharp
let go (a : int array) =
    let a : int seq = upcast a
    for i in a do
        ignore <| a
```

???

yes, no, no, yes

---

## Funcs

TODO - drop this?

`'a -> 'b` <-> `Func<'a, 'b>`

---

## Arrays


- Allocates a closure
- Fix is to use an intermediate variable

```fsharp
for i in 0..arr.Length-1 do
    arr.[i] ()
```

Allocates a closure.

```fsharp
for i in 0..arr.Length-1 do
    let f = arr.[i]
    f ()
```

Doesn't allocate a closure.

--

Why?

Some odd quirk of the compiler.

---
