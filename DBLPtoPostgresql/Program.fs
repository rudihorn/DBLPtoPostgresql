open System
open System.Linq
open System.Xml
open System.IO
open System.Xml.Schema
open System.Xml.Resolvers
open Npgsql
open System.Xml.Linq

open Database
open SqlDumpStream

[<EntryPoint>]
let main argv =
    let settings = XmlReaderSettings()
    settings.DtdProcessing <- DtdProcessing.Parse
    settings.XmlResolver <- XmlUrlResolver()
    settings.ValidationType <- ValidationType.DTD
    let article_author = SqlDumpStream("article_author.sql", "article_author",
        [ Col.Key ("key", "article", ColType.String);
          Col.Key ("author", "author", ColType.String); ]
    )
    let proceedings_author = SqlDumpStream("proceedings_author.sql", "proceedings_author",
        [ Col.Key ("key", "proceeding", ColType.String);
          Col.Key ("author", "author", ColType.String); ]
    )
    let proceedings_article = SqlDumpStream("proceedings_article.sql", "proceedings_author",
        [ Col.Key ("proceedings", "proceeding", ColType.String);
          Col.Key ("key", "proceedings", ColType.String); ]
    )
    let proceedings = SqlDumpStream("proceedings.sql", "proceedings",
        [ Col.Key ("key", "proceeding", ColType.String);
          Col.CAs ("title", "proceedings_name", ColType.String);
          Col.CAs ("year", "proceedings_year", ColType.Int);
          Col.MC ("author", "author", ColType.String,
            fun str el -> proceedings_author.InsertMul str el) ]
    )
    let inproceedings_author = SqlDumpStream("inproceedings_author.sql", "inproceedings_author",
        [ Col.Key ("key", "inproceeding", ColType.String);
          Col.Key ("author", "author", ColType.String); ]
    )
    let inproceedings_crossref = SqlDumpStream("inproceedings_crossref.sql", "inproceedings_crossref",
        [ Col.Key ("key", "inproceeding", ColType.String);
          Col.CAs ("proceedings", "proceedings", ColType.String); ]
    )
    let inproceedings = SqlDumpStream("inproceedings.sql", "inproceedings",
        [ Col.Key ("key", "inproceeding", ColType.String);
          Col.C ("title", ColType.String);
          Col.C ("year", ColType.Int);
          Col.MC ("crossref", "crossref", ColType.String,
            fun str el -> inproceedings_crossref.InsertMul str el);
          Col.MC ("author", "author", ColType.String,
            fun str el -> inproceedings_author.InsertMul str el); ]
    )
    let article = SqlDumpStream("article.sql", "article",
        [ Col.Key ("key", "article", ColType.String);
          Col.C ("title", ColType.String);
          Col.C ("year", ColType.Int);
          Col.MC ("author", "author", ColType.String,
            fun str el -> article_author.InsertMul str el);
          Col.MC ("crossref", "crossref", ColType.String,
            fun str el -> proceedings_article.InsertMul str el); ]
    )
    let phdthesis = SqlDumpStream("phdthesis.sql", "phdthesis",
        [ Col.Key ("key", "phdthesis", ColType.String);
          Col.C ("title", ColType.String);
          Col.C ("year", ColType.Int);
          Col.C ("author", ColType.String) ]
    )
    let mscthesis = SqlDumpStream("mastersthesis.sql", "mastersthesis",
        [ Col.Key ("key", "mastersthesis", ColType.String);
          Col.C ("title", ColType.String);
          Col.C ("year", ColType.Int);
          Col.C ("author", ColType.String) ]
    )
    let tables = [
        "phdthesis", phdthesis;
        "mastersthesis", mscthesis;
        "article", article;
        "article_author", article_author;
        "proceedings", proceedings;
        "proceedings_article", proceedings_article;
        "proceedings_author", proceedings_author;
        "inproceedings", inproceedings;
        "inproceedings_crossref", inproceedings_crossref;
        "inproceedings_author", inproceedings_author; ]
    for (_, table) in tables do table.Header

    let r = XmlReader.Create("dblp.xml", settings)
    let li = r :> Object :?> IXmlLineInfo
    r.Read() |> ignore

    while not r.EOF
        do
            if li.LineNumber % 100000 = 0 then
                printf "\rAt line %.02f%%...     " <| float li.LineNumber / 551122.99
            match r.NodeType with
            | XmlNodeType.Whitespace
            | XmlNodeType.EndElement
            | XmlNodeType.DocumentType
            | XmlNodeType.XmlDeclaration-> r.Read() |> ignore
            | XmlNodeType.Element ->
                match r.Name with
                | "dblp" ->
                    // read into dblp node
                    r.Read() |> ignore
                | n ->
                    let node = XElement.ReadFrom(r) :?> XElement
                    let table = List.tryFind (fun (k,_) -> k = n) tables
                    match table with
                    | Some (name, table) ->
                        table.Insert(node)
                    | None -> ()
            | _ -> ()
        done

    for (_, table) in tables do
        table.Footer
        table.Close

    printfn "Finished..."
    0
