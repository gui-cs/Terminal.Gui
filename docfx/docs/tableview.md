# Table View

**These are the v1 API docs**. The v2 API docs are [here](https://gui-cs.github.io/Terminal.GuiV2Docs/).

This control supports viewing and editing tabular data. It provides a view of a [System.DataTable](https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable?view=net-5.0).

System.DataTable is a core class of .net standard and can be created very easily

[TableView API Reference](~/api/Terminal.Gui.TableView.yml)

## Csv Example

You can create a DataTable from a CSV file by creating a new instance and adding columns and rows as you read them. For a robust solution however you might want to look into a CSV parser library that deals with escaping, multi line rows etc.

```csharp
var dt = new DataTable();
var lines = File.ReadAllLines(filename);

foreach(var h in lines[0].Split(',')){
   dt.Columns.Add(h);
}

foreach(var line in lines.Skip(1)) {
    dt.Rows.Add(line.Split(','));
}
```

## Database Example

All Ado.net database providers (Oracle, MySql, SqlServer etc) support reading data as DataTables for example:

```csharp
var dt = new DataTable();

using(var con = new SqlConnection("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;"))
{
    con.Open();
    var cmd = new SqlCommand("select * from myTable;",con);
    var adapter = new SqlDataAdapter(cmd);

    adapter.Fill(dt);
}
```

## Displaying the table

Once you have set up your data table set it in the view:

```csharp
tableView = new TableView () {
    X = 0,
    Y = 0,
    Width = 50,
    Height = 10,
};

tableView.Table = yourDataTable;
```

## Table Rendering
TableView supports any size of table (limited only by the RAM requirements of `System.DataTable`). You can have
thousands of columns and/or millions of rows if you want. Horizontal and vertical scrolling can be done using
the mouse or keyboard.

TableView uses `ColumnOffset` and `RowOffset` to determine the first visible cell of the `System.DataTable`.
Rendering then continues until the avaialble console space is exhausted. Updating the `ColumnOffset` and 
`RowOffset` changes which part of the table is rendered (scrolls the viewport).

This approach ensures that no matter how big the table, only a small number of columns/rows need to be
evaluated for rendering.
