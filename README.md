GenTools
========

---

Loads tables and stored procedures from a MS SQL Server database and generates ADO.NET code in C#.

Description
-----------

*GenTools* is a collection of 3 seperate tools that each handle one task each. A description and usage for each tool is listed below.

My hopes are to have these tools working with major database vendors, but testing is done exclusively on MS SQL Server 2012 for now. The tools are tested against the .NET framework v4.0.

### GenProc

Generator used for stored procedures. Each procedure is generated as a class with the procedure's parameters created as public properties on the class. Except for output parameters, each parameter is listed in the class' constructor with the appropriate types. Parameters with a default value will have that value carried over to C#.

The generated code is organized into a heirarchy that reflects the procedure's name, split on underscores (`_`). For instance, the following stored procedure `Name1_Name2_Proc` would generate C# code with the heirarchy `Name1.Name2.Proc`. Any `p_` or `sp_` at the beginning of the procedure name will be removed prior to this process.

A collision can happen when procedures have 'overlapping' names. For example, `p_Admin_User_GetAll` and `p_Admin_User` would collide on `User`. *GenProc* would try to create both a class and a namespace to represent these procedures. To get around this issue, *CollisionPrefix* is added to the class' name.

Each generated class inherits from `WrappedProcedure`, which provides an API for invoking the stored procedure. Documentation for this class can be found in [WrappedProcedure.cs](https://github.com/chris-cartwright/GenTools/blob/master/GenProc/Templates/Files/WrappedProcedure.cs).

### GenTable

Generator used for tables. Each table is generated as a class with the table's columns created as public properties on the class. The type of each column is carried over to C#. The generated classes all inherit from WrappedTable.cs. Documentation can be found [here](https://github.com/chris-cartwright/GenTools/blob/master/GenTable/Templates/Files/WrappedTable.cs).

*GenTable* also generates a few helper methods to make development easier. They are:

 * `Populate`: Takes a dictionary and returns a newly-constructed object from it. Maps the properties of the class to the keys of the dictionary.
 * `LoadFull`: Takes an ID of the appropriate type and returns the row who's identity column matches the given ID.
 * `SaveFull`: Updates a row in the table based on the passed-in object. Row matching is done based on the identity column.
 * `CreateFull`: Creates a new row in the table and returns the ID.

`LoadFull`, `SaveFull` and `CreateFull` are only generated if the table has an identity column.

### GenTypes

Generator used for tables with static data. Tables are created as classes with an underscore prefixed. Each value in the type table is added as a `public readonly` property. A `public static readonly` variable is created with the same name as the table and uses the prefixed class as the type. These variables contain the identity to value mapping.

The value column is chosen based on the identity column's name, without the ID on the end. Because of this check, the identity column must be suffixed with ID.

Only tables ending in `Type`, `Types`, `Category` or `Categories` are generated.

Usage
-----

These tools are (at the moment) designed to be ran from the command line during the pre-build of a assembly.

`WrappedTable` and `WrappedProcedure` both have a `ConnectionString` property that should be set appropriately. Example: 
```C#
public static readonly string ConnectionString = WebConfigurationManager.ConnectionStrings["SomeConnection"];
```

Each tool uses a .NET config file to load settings from. A standard set of options is available to each tool:

| Name             | Type   | Description |
|------------------|--------|---|
| LoggingLevel     | number | Sets the verbosity of the logger |
| MasterNamespace  | string | Namespace all generated classes/objects will be under |
| Name             | string | Name of configuration block. Multiple blocks are supported per config file |
| ConnectionString | string | Name of connection string to use |
| OutputFile       | string | Full path to output file that will contain the generated code |
|------------------|--------|---|

All tools require Common.dll to be deployed with them.

### GenProc

| Name            | Type   | Description |
|-----------------|--------|---|
| CollisionPrefix | string | Prefix added to classes if a collision is found |
| MiscClass       | string | Class to use for procedures without a namespace |
|-----------------|--------|---|

#### Requirements

 * `p_ListProcedures`
 * `GetParamDefault`
 * `WrappedProcedure`

### GenTable

| Name             | Type   | Description |
|------------------|--------|---|
| CollisionPostfix | string | Postfix added to classes if a collision is found |
|------------------|--------|---|

### Requirements

 * `WrappedTable.cs`
 * `p_ListTables`

### GenTypes

| Name     | Type   | Description |
|----------|--------|---|
| Language | string | Language templates to use when generating output |
|----------|--------|---|

### Requirements

 * `p_ListTypeTables`

Questions?
----------

Please feel free to ask questions using the *Issues* tab above.
