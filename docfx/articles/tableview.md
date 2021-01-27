# Table View

This control supports viewing and editing tabular data.  It provides a view of a [System.DataTable](https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable?view=net-5.0).

System.DataTable is a core class of .net standard and can be created very easily

## Csv Example

You can create a DataTable from a CSV file by creating a new instance and adding columns and rows as you read them.  For a robust solution however you might want to look into a CSV parser library that deals with escaping, multi line rows etc.

```csharp
var dt = new DataTable();
var lines = File.ReadAllLines(filename);
			
foreach(var h in lines[0].Split(',')){
	dt.Columns.Add(h);
}
				

foreach(var line in lines.Skip(1)) {
	lineNumber++;
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

