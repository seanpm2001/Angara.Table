﻿namespace Angara.Data

open System
open System.Collections.Generic
open System.Collections.Immutable

/// Represents a single value of a table column.
type DataValue =
    | IntValue      of int
    | RealValue     of float
    | StringValue   of string
    | DateValue     of DateTime
    | BooleanValue  of Boolean
    /// If this instance is IntValue, returns the integer value; otherwise, throws `InvalidCastException`.
    member AsInt     : int
    member AsReal    : float
    member AsString  : string
    member AsDate    : DateTime
    member AsBoolean : Boolean

/// Represents data values of a table column as an immutable array of one of the supported types which is computed on demand.
[<NoComparison>]
type ColumnValues =
    | IntColumn     of Lazy<ImmutableArray<int>>
    | RealColumn    of Lazy<ImmutableArray<float>>
    | StringColumn  of Lazy<ImmutableArray<string>>
    | DateColumn    of Lazy<ImmutableArray<DateTime>>
    | BooleanColumn of Lazy<ImmutableArray<Boolean>>
    /// If this instance is IntColumn, returns the immutable integer array; otherwise, throws `InvalidCastException`.
    /// If the column array has not been evalutated before, this function performs the execution of the Lazy instance.
    member AsInt     : ImmutableArray<int>
    member AsReal    : ImmutableArray<float>
    member AsString  : ImmutableArray<string>
    member AsDate    : ImmutableArray<DateTime>
    member AsBoolean : ImmutableArray<Boolean>
    /// Returns a column field at the specified row index.
    /// If the column array has not been evalutated before, this function performs the execution of the Lazy instance.
    member Item      : rowIndex:int -> DataValue

/// Represents a table column which is a pair of column name and an immutable array of one of the supported types.
[<Class>]
type Column =
    member Name : string with get
    member Rows : ColumnValues with get
    member Height : int with get 
    static member OfArray : name:string * rows:'a[] -> Column
    static member OfArray : name:string * rows:ImmutableArray<'a> -> Column
    static member OfArray : name:string * rows:System.Array -> Column
    static member OfLazyArray  : name:string * rows:Lazy<ImmutableArray<'a>> * count:int -> Column
    static member OfColumnValues : name:string * rows:ColumnValues * count:int -> Column


/// Represents a table wich is an immutable list of named columns.
/// The type is thread safe.
[<Class>]
type Table = 
    interface IEnumerable<Column> 

    new : nameColumns : Column list -> Table

    /// Gets a count of the total number of columns in the table.
    member Count : int with get
    /// Gets a column by its index.
    member Item : index:int -> Column with get
    /// Gets a column by its name.
    /// If there are several columns with same name, returns the fist column having the name.
    member Item : name:string -> Column with get
    /// Tries to get a column by its index.
    member TryItem : index:int -> Column option with get
    /// Tries to get a column by its name.
    /// If there are several columns with same name, returns the fist column having the name.
    member TryItem : name:string -> Column option with get

    /// Gets a count of the total number rows in the table.
    member RowsCount : int with get

    /// Builds and returns rows of the table represented as a sequence of instances of certain type,
    /// so that one instance of `'r` corresponds to one row of the table with order respeced.
    /// Columns are mapped to public properties of `'r`.
    ///
    /// The method uses reflection to build instances of `'r` from the table columns:
    /// - If `'r` is F# record, then for each property of the type there must be a corresponding column of appropriate type
    /// - Otherwise, then for each public property of the type that has get and set accessors there must a corresponding column of appropriate type
    member ToRows<'r> : unit -> 'r seq

    /// Creates a new, empty table
    static member Empty : Table
    static member Add<'a> : column:Column -> table:Table -> Table
    static member Remove : columnNames:seq<string> -> table:Table -> Table

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     1 column, predicate should be predicate:('a->bool), where 'a is the type of the column, so 'b = bool
    ///     2 columns, predicate:('b>'c->bool), where 'b and 'c are the types of the columns, so 'b = 'c->bool
    ///     3 columns, predicate:('b->'c->'d->bool), where 'b, 'c and 'd are the types of the columns, so 'a = 'b->'c->'d
    ///     n...
    static member Filter : columnNames:seq<string> -> predicate:('a->'b) -> table:Table -> Table

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns and row index
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     0 column, predicate should be: `int->bool`, so `'a = bool`
    ///     1 column, predicate should be: `int->'b->bool`, where 'b is the type of the column, so `'a = 'b -> bool`
    ///     2 columns, predicate should be: `int->'b->'c->bool`, where 'b and 'c are the types of the columns, so 'a = 'b->'c->bool
    ///     n...
    static member Filteri : columnNames:seq<string> -> predicate:(int->'a) -> table:Table -> Table

    /// Builds a new sequence whose elements are the results of applying the given function 'map'
    /// to each of the rows of the given table columns.
    /// 
    /// The generic map function is only partially defined.
    /// If there are:
    ///
    /// - 0 columns, map should be `map:(unit->'c)`, where `'a` is the type of the column, so `'a = unit` and `'b = 'c`
    /// - 1 column, map should be `map:('a->'c)`, where `'a` is the type of the column, so `'b = 'c`
    /// - 2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    /// - 3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    /// - n...
    static member Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c seq

    /// <summary>Builds a new sequence whose elements are the results of applying the given function 'map'
    /// to each of the rows of the given table columns.
    /// The integer index passed to the function indicates the index of row being transformed.</summary>
    /// <remarks><p>The generic map function is only partially defined.
    /// If there are:
    ///     0 columns, map is called for each row of the table and should be map:(int->'c), so 'a = 'c
    ///     1 column, map should be map:(int->'d->'c), where 'd is the type of the column, so 'a = 'd->'c
    ///     2 columns, map:(int->'d->'e->'c), where 'd and 'e are the types of the columns, so 'a = 'd->'e->'c
    ///     n...
    /// </p></remarks>
    static member Mapi<'a,'c> : columnNames:seq<string> -> map:(int->'a) -> table:Table -> 'c seq

    /// Builds a new table that contains all columns of the given table and a new column or a replacement of an original table column;
    /// elements of the column are the results of applying the given function to each of the rows of the given table columns.
    ///
    /// The generic map function is only partially defined.
    /// If there are:
    /// 
    /// - 1 column, map should be map:('a->'c), where 'a is the type of the column, so 'b = 'c
    /// - 2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    /// - 3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    /// - n...
    /// 
    /// Ultimate result type of the map function must be either Int, Float, String, Bool or DateTime.
    /// </remarks>
    static member MapToColumn : columnNames:seq<string> -> newColumnName:string -> map:('a->'b) -> table:Table -> Table

    /// <summary>Builds a new table that contains all columns of the given table and a new column or a replacement of an original table column;
    /// elements of the column are the results of applying the given function to each of the rows of the given table columns.
    /// The integer index passed to the function indicates the index of row being transformed.</summary>
    /// <remarks><p>The generic map function is only partially defined.
    /// If there are:
    ///     0 columns, map is called for each row of the table and should be map:(int->'c), so 'a = 'c
    ///     1 column, map should be map:(int->'d->'c), where 'd is the type of the column, so 'a = 'd->'c
    ///     2 columns, map:(int->'d->'e->'c), where 'd and 'e are the types of the columns, so 'a = 'd->'e->'c
    ///     n...
    /// </p>
    /// <p>Ultimate result type of the map function must be either Int, Float, String, Bool or DateTime.</p>
    /// </remarks>
    static member MapiToColumn : columnNames:seq<string> -> newColumnName:string -> map:(int->'a) -> table:Table -> Table

        /// <summary>Applies the given function to the arrays of given table columns.</summary>
    /// <remarks>
    /// <p>The generic curried transform function is only partially defined.
    /// If there are:
    ///     1 column, transform should be transform:('a->'c) where 'a is an array corresponding to the column type, so 'b = 'c
    ///     2 columns, transform('a->'d->'c) where 'a, 'd are arrays corresponding to the columns types, so 'b = 'd->'c
    ///     3 columns, transform('a->'d->'e->'c) where 'a, 'd, 'e are arrays corresponding to the columns types, so 'b = 'd->'e->'c
    ///     n...</p>
    /// </remarks>
    static member Transform<'a,'b,'c> : columnNames:seq<string> -> transform:(ImmutableArray<'a>->'b) -> table:Table -> 'c

    /// Builds a new table that contains columns of both given tables. Duplicate column names are allowed.
    static member Append : table1:Table -> table2:Table -> Table

    /// <summary>Builds a new table that contains columns of the given table appended with columns of a table produced by the
    /// given function applied to the arrays of given table columns.</summary>
    /// <remarks>
    /// <p>The generic curried transform function is only partially defined.
    /// If there are:
    ///     1 column, transform should be transform:('a->Table) where 'a is an array corresponding to the column type, so 'b = Table
    ///     2 columns, transform('a->'d->Table) where 'a, 'd are arrays corresponding to the columns types, so 'b = 'd->Table
    ///     3 columns, transform('a->'d->'e->Table) where 'a, 'd, 'e are arrays corresponding to the columns types, so 'b = 'd->'e->Table
    ///     n...</p>
    /// <p>The transform function argument types may be one of: Column, T[], IRArray&lt;T> or Array.</p>
    /// </remarks>
    static member AppendTransform : columnNames:seq<string> -> transform:('a->'b) -> table:Table -> Table

    /// Reads table from a delimited text file.
    static member Read : settings:Angara.Data.DelimitedFile.ReadSettings -> path:string -> Table

    /// Reads table from a delimited text stream.
    static member ReadStream : settings:Angara.Data.DelimitedFile.ReadSettings -> stream:IO.Stream -> Table
    
    /// Writes a table to a stream as a delimited text.
    static member Write : settings:Angara.Data.DelimitedFile.WriteSettings -> path:string -> table:Table -> unit

    /// Writes a table to a stream as a delimited text.
    static member WriteStream : settings:Angara.Data.DelimitedFile.WriteSettings -> stream:IO.Stream -> table:Table -> unit


//[<Class>]
//type Table<'r> = 
//    inherit Table
//
//    new : rows : 'r seq -> Table<'r>
//
//    member Rows : ImmutableArray<'r>

