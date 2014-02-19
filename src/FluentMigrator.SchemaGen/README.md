Overview
--------
This app generates a set of C# Migration classes based on a SQL Server database using the Fluent Migrator API.
It can be used to generate migrations for a new database **install** OR an **upgrade** between two database versions.

Generated classes are intended to be added a C# project that outputs a DLL that is executed by a [Fluent Migration Runner](https://github.com/schambers/fluentmigrator/wiki/Migration-Runners) such as Migrate.exe, NAnt task or MSBuild tasks.

Features:
---------

  * Generates a **full** or **upgrade** schema migration (tables, indexes, foriegn keys) based on existing SQL Server 2008+ databases.
    * Generated schema can then be used to install / upgrade other database types supported by Fluent Migrator.
    * Can select included and excluded tables by name or pattern.
  * Generates a class per table ordered by FK dependency constraints. 
  * Migration class are number based a migration version: major.minor.patch.step 
    * You supply the major.minor.patch  (e.g. "3.1.2")
    * The step is generated and defines the execution order of the classes.
    * You can optionally start/end step number to support merging sets of generated classes.
    * Shows internal migration number as a comment. 
      * Useful when debugging to run a migration up to a previous migration number and then test the failing SQL.
  * Data types and default value mapping
    * Converts all SQL Server supported data types to FM API types
	* Optionally maps deprecated SQL Server types (TEXT, NTEXT, IMAGE) as custom types.
	* Converts common field default functions to [FM default constants](https://github.com/schambers/fluentmigrator/wiki/Use-inbuilt-database-functions-when-setting-the-default-value): 
	  * NewGuid, NewSequentialId, CurrentDateTime, CurrentUTCDateTime, CurrentUser
  * Indexes:
	* Rebuilds index when index columns change type
    * Supports Clustered/Nonclustered Indexes
	* Supports Index Fill Factor (Added WithOptions.Fill() method to FM API)
  * SQL Scripts for Views, Stored Procedures, Functions, Data types, Seed Data and Custom Data Migrations
    * Adds migrations that run SQL scripts to perform Pre, Post or Per table data migrations.
    * Optionally loads SQL at run-time (best for development) OR embeds SQL scripts in C# code (best for deployment). 
    * A simple tag based naming convention selects scripts to execute for each database, database type or software component.
    * Pre and Post processing SQL.
    * Post: Views, Stored Procedures, Functions, Data types
    * Post: Seed Data, Demo Data, Test Data.
    * Per table migration class: Data migration SQL scripts.
  * Specify output directory, class namespace, Fluent Migrator [Tag] attrtibutes.
    * Additional support classes are added in a generated project DLL.
    * Uses a MigrationVersion() class that defines the product version.
  * Source databases defined either using full connection string or just a localhost database name.
  * Includes several minor enhancements and fixes to Fluent Migrator API.
  * Emits IfNotDatabase("jet") conditions for indexes that match foriegn key constraints. 
    * Jet (MS-Access) auto adds FK indexes so we don't want to add a duplicate index.
    * Add IfNotDatabase() construct to FM API for this case.
  * Command line and MSBuild Task support.

Schema Upgrade Features:
-----------------------
  * Generates Initial and Final migration classes that set Start / End migration version and step number. 
  * Generates a migration class per table that:
    * Adds new table columns, Removes old columns, Updates columns that change their type or properties
    * Deletes removed indexes, Creates new indexes, 
    * Deletes removed foriegn keys, Created new foriegn keys, 
    * Recreates updated or renamed indexes (including indexes containing updated column types)
    * Recreates updated or renamed foriegn keys.
  * Optionally generates a "DropTable" migration.
    * Drops removed tables and related foriegn keys in reverse FK order
  * Optionally generates a "DropScript" migration
    * Drops removed scriptable objects: Views, Stored Procedures, Functions
  * Adds optional comments showing changes including: 
    * Renamed/Duplicate indexes and foreign keys (matching definition)
    * Previous definition of deleted and updated columns, indexes and foreign keys.
  * When a NULL-able table field becomes NOT NULL, optionally emits SQL to set NULL values to the column's DEFAULT value (if defined).
  * Imports or Embeds SQL scripts executed after new columns / indexes are added but before old columns are removed on each table.

C# Project Template
-------------------

You should always use the provided **SchemaGenTemplate** C# project as a template for creating any new migration projects that will accepted generated migration classes.

The C# migration classes output by the code generator inherit from MigrationExt and AutoReversingMigrationExt that in turn inherit from the Migration and AutoReversingMigration migration base classes supplied by the FM API. The C# **SchemaGenTemplate** project includes these extension classes and additional helper classes that the generated classes depend on.

Classes included in this C# project template assume that the project root **namespace** is set to **Migrations**. 

Command Line Options 
--------------------

 ```FluentMigrator.SchemaGen.EXE <options>```

See the [Options.cs](Options.cs) for command line options and help documentation.

MSBuild Task
------------
   
  * See the [FmCodeGen.cs](MSBuild/FmCodeGen.cs) for **FmCodeGen** task options.
    * MSBuild task options are documented in matching command options: [Options.cs](Options.cs).
  * Requires MSBuild.exe from .NET 3.5 or later:
    * ``` %WinDir%\Microsoft.NETFramework\v3.5\MSBuild.exe ``` 

**Example MSBuild script:**

```

	<UsingTask TaskName="FluentMigrator.SchemaGen.MSBuild.FmCodeGen" AssemblyFile=".\FluentMigrator.SchemaGen.exe" />

    <!-- Generates Install migration classes for schema v3.1.0 
         from local 'MyApp_030100' SQL Server 2008 database. -->

    <FmCodeGen
        Db="MyApp_030100"
        SqlDirectory=".\SQL"
        OutputDirectory=".\MyApp.Migrations\v03_01_00\Install"
        NameSpace="Migrations.v03_01_00.Install"
        MigrationVersion="3.1.0" StepStart="1" StepEnd="100"
        UseDeprecatedTypes="true"
        IncludeTables="tbl*" ExcludeTables="zz*;temp*" />

    <!-- Generates Upgrade migrations for v3.1.0 to v4.0.1 by comparing 
        local databases 'MyApp_030100' and 'MyApp_040001' -->

    <FmCodeGen
        Db1="MyApp_030100" Db2="MyApp_040001"
        SqlDirectory=".\SQL"
        OutputDirectory=".\MyApp.Migrations\v04_00_01\Upgrade"
        NameSpace="Migrations.v04_00_01.Upgrade"
        MigrationVersion="4.0.1" StepStart="1" StepEnd="100"
        UseDeprecatedTypes="true"
        IncludeTables="tbl*" ExcludeTables="zz*;temp*" />


```


Known Issues
------------
 
 * There are many complex cases that this generator is unlikely to ever cater for. 
   * The goal is to cover the most common cases. 
   * The rest invariably needs your knowledge of intended schema and data change! 
   * Example: Migrating recusive data relationships.
 * When a column or table is renamed, we currently emit add/remove or drop/create commands which you may may need to replace these with Rename.Table() or Rename.Column()
   * There is no way that SchemaGen can safely know that this is what was intended. 
 * SQL Server Include columns in indexes are implemented by SqlServerSchemaReader not yet tested.
 * When a field type is altered, we currently don't handle the case where this field is part of a foriegn key relation.
   * Requires one or more FKs to be dropped and two or more tables altered together before FKs are recreated.

To Do
-----
 * Example command line args and MSBuild scripts.
   * An MSBuild script that emits an enitire C# project and compiles it.
 * Unit Tests 
 * Need to check if a Primary Key is non-clustered and emit: 
   * Create.PrimaryKey("PK").OnTable("TestTable").Column("Id").NonClustered();
 * Support the option of emitting a single migration class (not hard to do with current implementation).
Refactoring Wish List:
 * We always know a 'better way' after the deed is done :) . If I get around to it this what needs doing:
 * Split up FmDiffMigrationWriter into smaller component classes.
 * Rewrite FmDiffMigrationWriter.UpdateTable() to use an improved data structure that should make it simpler and more readable.
 * Revise text output implementation. Used IEnumerable<string> an another project to emit lines to great effect. 

Future Ideas
------------
 * Might consider a two phase approach that emits differences as a data structure and then applies an ordering / grouping algorithm.  Groups become classes.
   * This should support more complex cases involving order complexity.
   * We're likely to find that some ordering contraints will depend on the database type.
 * Support selective differences so you can slice the changes into different phases.
   * Currently only support table include/exclude selection.
   * Object renaming only (Can then separate out Index/FK renaming changes from the 'real' schema changes).
   * Map set of tables (selected by pattern) to be assigned a FM Tag.
     * FKs between these tables can get multiple tags.

Required Libs
-------------
   PM> Install-Package CommandLineParser

Fluent Migrator API Refs:

  * https://github.com/schambers/fluentmigrator      - Source 
  * https://github.com/schambers/fluentmigrator/wiki - Docs

License:
-------
  * Copyright (C) 2014 Tony O'Hagan
  * [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0) 
