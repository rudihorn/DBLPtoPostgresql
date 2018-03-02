module Database

open System
open System.Xml.Linq

type ColType =
    | String
    | Int 
    member self.SQLType =
        match self with
        | Int -> "INT"
        | String -> "VARCHAR(2048)"

    member self.ToSqlString (str : string) =
        match self with
        | String -> sprintf "\'%s\'" <| str.Replace("'", "''") 
        | Int -> Int32.Parse(str).ToString()
        | _ -> failwithf "Unknown type {}" <| str.ToString()

type Col = 
    | Key of string * string * ColType
    | C of string * ColType
    | CAs of string * string * ColType
    | MC of string * string * ColType * (string -> XElement -> unit)
    member self.Hide =
        match self with 
        | MC _ -> true
        | _ -> false

    member self.IsKeyCol =
        match self with
        | Key _ -> true
        | _ -> false

    member self.Name = 
        match self with
        | Key (name, _, _) 
        | MC (name, _, _, _) 
        | C (name, _) -> name
        | CAs (name, _, _) -> name

    member self.Alias = 
        match self with
        | Key (_, alias, _) 
        | MC (_, alias, _, _)
        | C (alias, _) -> alias
        | CAs (_, alias, _) -> alias

    member self.Type =
        match self with
        | Key (_, _, t)
        | MC (_, _, t, _)
        | C (_, t) -> t
        | CAs (_, _, t) -> t