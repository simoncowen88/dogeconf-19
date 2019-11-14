
class: center, middle

# Gotchas

???

unexpected pitfalls

---

class: center, middle

# Tuples

---

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

### Main gotcha

`System.Tuple` is a reference type.

So making a tuple allocates.

???

is this an issue?

.net is pretty quick with allocating / short-lived objects

GC pauses

--

&nbsp;

F\# now has struct tuples.

```fsharp
let thisIsAStructTuple : (struct (int * string)) = struct (1, "foo")
```

---

## Tuples

### Side gotcha

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

### Side gotcha

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

class: center, middle

# Option

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

### Main gotcha

`'a Option` is a reference type.

So making an option allocates.

--

&nbsp;

F\# now has struct options.

```fsharp
let thisIsAStructTuple : int ValueOption = ValueSome 3
```

---

class: center, middle

# Closures

---

## Closures

```fsharp
let f a b = a + b
let thisIsAClosure : int -> int = f 3
```

Used extensively in F\#

???

like Func

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

???

named as foo@46

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

### What is it?

--

```fsharp
type thisIsAClosure_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b
```

---

## Closures

### Main gotcha

Closures are reference types.

So making a closure allocates.

---

## Closures

### Side gotcha

--

```fsharp
type thisIsAClosure_impl (a : int) =
    inherit FSharpFunc<int, int>
    override __.Invoke (b : int) = a + b
```

???

notice override

--

Invoking a closure is a virtual call.

Virtual calls are a bit slower.

???

concrete type - just jump

interface / virtual - lookup table

---

class: center, middle

# Boxing

---

## Boxing

```fsharp
let thisIsABox : obj = box 3
```

```fsharp
let thisIsAlsoABox = DateTime.Now :> IComparable<DateTime>
```

--

In C\#

```csharp
object thisIsABox = 3;
```

```csharp
IComparable<DateTime> thisIsAlsoABox = DateTime.Now;
```

???

implicit in C\#


---

## Boxing

```fsharp
let thisIsABox : obj = box 3
```

```fsharp
let thisIsAlsoABox = DateTime.Now :> IComparable<DateTime>
```

--

Value-type treated as an obj.

Value-type treated as an interface.

---

## Boxing

```fsharp
let thisIsABox : obj = box 3
```

--

### What is it?

--

.quote[
> [Required to] have a unified type system and allow value types to have a completely different representation of their underlying data from the way that reference types represent their underlying data (e.g., an int is just a bucket of thirty-two bits which is completely different than a reference type).
]

--

`obj` is a reference type.

We can't just upcast a value type to `obj`.

We need some holder for it - that's a box.

---

## Boxing

### Main gotcha

```fsharp
let thisIsABox : obj = box 3
```

Boxing a value type allocates a box.

--

```fsharp
let thisIsAlsoABox = DateTime.Now :> IComparable<DateTime>
```

You may not realise you're even boxing.

???

c\# - (long)(int)o

---

class: center, middle

# DateTime

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

### Why?!

--

```fsharp
let (=) (a : DateTime) (b : DateTime) : bool =
    let o1 : obj = box a
    let o2 : obj = box b
    o1.Equals o2
```

---

class: center, middle

# Map and Set

---

## Map and Set

```fsharp
let thisIsAMap : Map<int, string> = [ 1, "one" ; 2, "two" ] |> Map.ofList
```

```fsharp
let thisIsASet : int Set = [ 1; 2; 3 ] |> Set.ofList
```

--

### Main gotcha

They are comparison-based (not hash-based).

???

Comparisons often more expensive than hashes / equalities


less able to short-circuit

--

Lookup and insert are `log n`.

---

## Map and Set

```fsharp
let thisIsAMap : Map<int, string> = [ 1, "one" ; 2, "two" ] |> Map.ofList
```

```fsharp
let thisIsASet : int Set = [ 1; 2; 3 ] |> Set.ofList
```

### Side gotcha

--

Map lookup allocates when `'key` is a value type.

--

### Why?!

--

```fsharp
let compare<'a when 'a : IComparable> (a : 'a) (b : 'a) : int =
    let o1 : IComparable = upcast a
    let o2 : obj = box a
    o1.Compare o2
```

---

class: center, middle

# Enumerating

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

What's an enumerator?

--

```fsharp
type IEnumerator<'a> =
    abstract MoveNext : unit -> bool
    abstract Current : 'a
    abstract Reset : unit -> unit
    abstract Dispose : unit -> unit

type IEnumerable<'a> =
    abstract GetEnumerator : unit -> IEnumerator<'a>
```

---

## Enumerating

```fsharp
type IEnumerable<'a> =
    abstract GetEnumerator : unit -> IEnumerator<'a>
```

We can't help but create an object to return here.

???

either ref type

or boxed value type

surely it _always_ allocates?

--

&nbsp;

.NET does some duck-typing here.

```fsharp
let enumerator = xs.GetEnumerator ()
while enumerator.MoveNext () do
    let x = enumerator.Current
    printfn "%d" x
```

???

core type enumerators are structs

---

## Enumerating

Rules of thumb:

- Enumerating an `IEnumerable` or `seq` will allocate.
- Enumerating a core concrete type (e,g, `array`) will not allocate.

---

class: center, middle

# Pop quiz

---

## Enumerating

```fsharp
let go (xs : int seq) =
    for x in xs do
        ...
```

Does it allocate?

--

# YES

---

## Enumerating

```fsharp
let go (xs : int list) =
    for x in xs do
        ...
```

Does it allocate?

--

# NO

---

## Enumerating

```fsharp
let go (xs : int array) =
    for x in xs do
        ...
```

Does it allocate?

--

# NO

---

## Enumerating

```fsharp
let go (xs : int array) =
    let xs' : int seq = upcast xs
    for x in xs' do
        ...
```

Does it allocate?

--

# YES

---

class: center, middle

# Arrays of functions

---

## Arrays of functions

```fsharp
for i in 0..arr.Length-1 do
    arr.[i] ()
```

--

### What's the problem?

--

This allocates a closure for every item.

???

quirk of the compiler

--

```fsharp
for i in 0..arr.Length-1 do
    let f = closure_impl ()
    f.Invoke (arr, i)
```

---

## Arrays of functions

```fsharp
for i in 0..arr.Length-1 do
    arr.[i] ()
```

### What's the fix?

--

```fsharp
for i in 0..arr.Length-1 do
    let f = arr.[i]
    f ()
```

---
