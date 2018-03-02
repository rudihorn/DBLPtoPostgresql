# DBLPtoPostgresql
A small parser for the DBLP xml file

## Instructions
In order to use this, download the dblp.dtd and dblp.xml.gz files from http://dblp.uni-trier.de/xml/. Extract the dblp.xml.gz file to dblp.xml and run the F# program in the same directory. It should produce a set of sql files which can be imported into Postgresql. 

## Tables

The most important tables are:

proceedings (proceedings, proceedings_name, proceedings_year): A set of conference proceedings
inproceedings (inproceedings, title, year): A set of papers in conference proceedings
inproceedings_crossref (inproceedings, proceedings): A table crossreferencing conference inproceedings with proceedings
inproceedings_author (inproceedings, author): Authors of inproceedings

