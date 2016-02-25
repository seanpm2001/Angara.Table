﻿(*** hide ***)
#I __SOURCE_DIRECTORY__
#load "load-project-debug.fsx"
(**
# Angara.Data.Table (F#)

A table is an immutable collection of named columns. 
[Angara.Data.Table](angara-data-table.html) exploits functional approach and allows to use succinct code to perform complex operations on tables.

*)

(**
## Creating and Initializing Tables

The `Table.Empty` property returns an empty table, i.e. a table that has no columns.
*)
open Angara.Data

let tableEmpty = Table.Empty

(**
To build a table from arrays (or other kinds of sequences) representing table columns, 
use `Table.Empty` and `Table.Add` functions. 
Note that all columns of a table must have same number of elements; otherwise, `Table.Add` fails.

The following example creates
a table with two columns `"x"` and `"y"` with data given as arrays of floats:
*)

let table =
    Table.Empty 
    |> Table.Add "x" [| 1; 2; 3 |]
    |> Table.Add "y" [| 2; 4; 6 |]


(**

A table can be initialized from a delimited text file, such as CSV file. To do that, you can use 
`Table.Read` function.

*)

let tableWheat = Table.Read DelimitedFile.ReadSettings.Default @"data\wheat.csv"

(**
Now, `tableWheat.ToString()` returns the following string:
*)    
(*** include-value: tableWheat ***)

(**
It means that `tableWheat` has three columns with names `Lon`, `Lat`, `wheat`; number of rows is 691.
Also, for each column several first elements are printed.
*)

(**
## Properties of Tables

To get a number of rows in a table, use `Table.Count` property; for example,
*)

tableWheat.Count

(** returns *)
(*** include-value: tableWheat.Count ***)

(** 
The properties `Table.Names` and `Table.Columns` return read-only lists of column names and the columns themselves.
The following code computes and prints average for each of the column of `tableWheat`:
*)



(**
# Samples

## Titanic survivor analysis

The following example computes the survival rates for the different passenger classes.
The original data is taken from https://www.kaggle.com/c/titanic.
*)

(** Having the table functions: *)

let GroupBy (colName : string) (projection : 'a -> 'b) (table : Table) : ('b * Table)[] =
    Table.ToArray<'a[]> colName table 
    |> Array.groupBy projection 
    |> Array.map(fun (key: 'b, _) ->
        key, table |> Table.Filter [colName] (fun (v:'a) -> projection v = key))

let OrderBy<'a,'b when 'b : comparison> (colName: string) (projection : 'a -> 'b) (table : Table) : Table =
    let order = 
        Table.ToArray<'a[]> colName table
        |> Array.mapi (fun i v -> (i, projection v)) 
        |> Array.sortBy snd |> Array.map fst
    let cols =
        table.Columns |> Seq.mapi(fun i c -> 
            table.Names.[i],            
            match Column.Type c with
            | t when t = typeof<float> -> Column.New(lazy(let arr:float[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<int> -> Column.New(lazy(let arr:int[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<string> -> Column.New(lazy(let arr:string[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<System.DateTime> -> Column.New(lazy(let arr:System.DateTime[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<bool> -> Column.New(lazy(let arr:bool[] = Column.ToArray c  in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | _ -> failwith "Unexpected column type")
    Table(cols)

let OfRows (columnNames : string seq) (rows : System.Array seq) : Table =
    let rows = rows |> Seq.toArray
    let cols =
        columnNames 
        |> Seq.mapi(fun iCol name ->
            name,
            match if rows.Length = 0 then typeof<float> else rows.[0].GetValue(iCol).GetType() with
            | t when t = typeof<float> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> float)))
            | t when t = typeof<int> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> int)))
            | t when t = typeof<string> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> string)))
            | t when t = typeof<System.DateTime> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> System.DateTime)))
            | t when t = typeof<bool> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> bool)))
            | _ -> failwith "Unexpected column type")
    Table(cols)

(** then *)

let survivors =         
    Table.Read { DelimitedFile.ReadSettings.Default with 
                     ColumnTypes = Some(fun (_,name) -> match name with "Survived" | "Pclass"-> Some typeof<int> | _ -> None) } 
               @"data\titanic.csv"
    |> GroupBy "Pclass" id 
    |> Seq.map(fun (pclass:int, table) -> 
        let stat = table |> Table.ToArray<int[]> "Survived" |> Array.countBy id |> Array.sortBy fst
        [| pclass; snd stat.[0]; snd stat.[1] |] :> System.Array)
    |> OfRows ["Pclass"; "Died"; "Survived"] 
    |> Table.MapToColumn ["Died"; "Survived"] "Died (%)" (fun (died:int) (survived:int) -> 100.0*(float died)/(float (died + survived)))
    |> Table.MapToColumn ["Died (%)"] "Survived (%)" (fun (died:float) -> 100.0 - died)
    |> Table.Remove ["Died"; "Survived"]
    |> OrderBy<int,int> "Pclass" id

(*** include-value: survivors ***)