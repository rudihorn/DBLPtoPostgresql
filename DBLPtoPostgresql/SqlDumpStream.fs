module SqlDumpStream

open System.IO
open System.Linq
open System.Xml.Linq

open Database
open Utility

type SqlDumpStream(file : string, table : string, fields : Col list) =
    let writer = new StreamWriter(file)
    let mutable first = true
    let mutable counter = 0

    member this.EscapeTable =
        table
    member this.InsertStatement =
        fprintfn writer "INSERT INTO %s (%s) VALUES" this.EscapeTable
            << Seq.fold cmbcomma ""
            <| seq { for c in fields do if not c.Hide then yield c.Alias }
        first <- true

    member this.Header =
        fprintfn writer "DROP TABLE IF EXISTS %s;" <| this.EscapeTable
        fprintfn writer "CREATE TABLE %s (" <| this.EscapeTable
        for c in fields do
            if not c.Hide then
                fprintfn writer "   %s %s NOT NULL," c.Alias <| c.Type.SQLType
        fprintfn writer "   PRIMARY KEY (%s)"
            << Seq.fold cmbcomma ""
            <| seq {
                for col in fields do
                    if col.IsKeyCol then
                        yield col.Alias
            }
        fprintfn writer ");"

        this.InsertStatement

    member this.PreInsert =
        counter <- counter + 1
        if counter % 10000 = 0 then
            fprintfn writer ";"
            this.InsertStatement

        // only write comma for second line and onwards
        if not first then
            fprintfn writer ","
        else
            first <- false

    member this.InsertMul (key : string) (el : XElement) =
        this.PreInsert
        fprintf writer "(%s, %s)" key <| ColType.String.ToSqlString(el.Value)

    member this.Insert(el : XElement) =
        if not <| el.Attribute(XName.Get("key")).Value.StartsWith "dblpnote/" then
            this.PreInsert

            fprintf writer "("
            List.iteri (fun i col ->
                match col with
                | MC (key, _, _, fn) ->
                    let subel = el.Elements(XName.Get(key)).ToArray()
                    let thkey = el.Attribute(XName.Get("key")).Value
                    let thkey = ColType.String.ToSqlString(thkey)
                    for e in subel do
                        fn thkey e
                | _ ->
                    let v =
                        if col.IsKeyCol then
                            el.Attribute(XName.Get(col.Name)).Value
                        else
                            let ch = el.Element(XName.Get(col.Name))
                            if isNull ch && col.Name.Equals("author") then "unknown author" else ch.Value
                    if i > 0 then fprintf writer ", "
                    fprintf writer "%s" << col.Type.ToSqlString <| v
            ) fields
            fprintf writer ")"
    member this.Footer =
        fprintfn writer ";"
    member this.Close =
        writer.Close()
