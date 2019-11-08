
## Options

Does it cost when 'a is a ref type?
Yes - None vs Some null

Similar to tuples.

```fsharp
let f s =
    let a = Some s
    match a with
    | Some x -> 1
    | None -> 0
```

Doesn't allocated an option
---


ValueOption