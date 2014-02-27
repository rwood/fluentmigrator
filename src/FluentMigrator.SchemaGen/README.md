Overview
--------
This app generates a set of C# Migration classes based on a SQL Server database using the Fluent Migrator API.
It can be used to generate migrations for a new database **install** OR an **upgrade** between two database versions.

Generated classes are intended to be added a C# project that outputs a DLL that is executed by a [Fluent Migration Runner](https://github.com/schambers/fluentmigrator/wiki/Migration-Runners) such as Migrate.exe, NAnt task or MSBuild tasks.

C# Project Template
-------------------

You should always use the provided **SchemaGenTemplate** C# project as a template for creating any new migration projects that will accepted generated migration classes.

The C# migration classes output by the code generator inherit from MigrationExt and AutoReversingMigrationExt that in turn inherit from the Migration and AutoReversingMigration migration base classes supplied by the FM API. The C# **SchemaGenTemplate** project includes these extension classes and additional helper classes that the generated classes depend on.
Classes included in this C# project template assume that the project root **namespace** is set to **Migrations**. 

There are also a number of other enhancements to the original Fluent Migrator API that this tool depends on.

Main Features:
==============

  * Code generation of classes that perform a **full** or **upgrade** schema migration (tables, indexes, foriegn keys) based on existing SQL Server 2008+ databases.
    * Generated schema can then be used to install / upgrade other database types supported by Fluent Migrator.
    * Can select included and excluded tables by name or pattern.
  * Generates a class per table ordered by FK dependency constraints. 
  * Migration class are number based a migration version: major.minor.patch.step 
    * You supply the major.minor.patch  (e.g. **3.1.2**)
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
  * Specify output directory, class namespace and newly added Fluent Migrator [Feature] attributes (replace [Tag]). 
    * Additional support classes are added in a generated project DLL.
    * Uses a MigrationVersion() class that defines the product version.
  * Source databases defined either using full connection string or just a localhost database name.
  * Includes several minor enhancements and fixes to Fluent Migrator API.
  * Emits IfNotDatabase("jet") conditions for indexes that match foriegn key constraints. 
    * Jet (MS-Access) auto adds FK indexes so this avoids adding a duplicate index.
    * Add IfNotDatabase() construct to FM API for this case.
  * Command line and MSBuild Task support.

Schema Upgrades:
===============

  * Generates Initial and Final migration classes that set Start / End migration version and step number. 
  * Generates a migration class per table that:
    * Adds new table columns, Removes old columns, Updates columns that change their type or properties
    * Deletes removed indexes, Creates new indexes, 
    * Deletes removed foriegn keys, Creates new foriegn keys, 
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

Fluent Migrator API Changes
---------------------------

* Added Create.Index().WithOptions.Fill(fill_factor) - Added fill factor support for SQL Server indexes.
* Added Execute.ScriptInDirectory() / Execute.ScriptsInNestedDirectories() - Executes directories of SQL scripts.  
  * Includes .WithTag()/.WithTags()/.WithPrefix() options to dynamically filter the set of SQL scripts executed.

SQL Script execution
--------------------
Excutes custom SQL Scripts for Table migration, Views, Stored Procedures, Functions, Data types, Seed Data, Demo Data, Test Data.

  * Adds migrations that run SQL scripts to perform Pre, Post or Per table data migrations.
  * Optionally loads SQL at run-time (best for development) OR embeds SQL scripts in C# code (best for deployment). 

SQL file Script feature tagging
-------------------------------

  * A simple tag based naming convention selects scripts to execute for each database type, database instance or component feature.
  * Allows reuse of SQL scripts that work in multiple database types or that need to be run in multiple database instances.
  * Flexible file/folder struture that allows related scripts to be either grouped together (same directory) or split into directory hierarchy.
  * Execution is ordered by full path name so dependency ordering can be controlled by file or folder naming.



Example SQL Script Folder Structure
-----------------------------------

 * ```SQL/M1_Install/version/1_Pre/```
   * Run prior to verion install. e.g. Create databases, configure logins.
 * ```SQL/M2_Upgrade/<version>/1_Pre/```
   * Run prior to version upgrade. Perform validation checks. Perform updates required to ensure schema changes can succeed.
 * ```SQL/M1_Install/<version>/2_PerTable/<table>_<tags>.sql```  OR  ```M1_Install/<version>/2_PerTable/<tags>/<table>.sql``` 
   * Per table scripts containing table name. Run as each new table is being **created** on a new **database install**.
 * ```SQL/M2_Upgrade/<version>/2_PerTable/```
   * Per table script containing table name. Run as each table is being **created** or **updated** during a **database upgrade**.
   * To facilitate data migration during table updates, the table will contain both OLD and NEW columns.
 * ```SQL/M3_Post/<version>/```
   * Run after either Install of OR Upgrade to a given version.  
   * Typically used to drop/recreate Views, Stored Procedures, Functions, Data types. Just create subfolder for each.
   * Use tags to selectively delete/reinsert Seed Data, Demo Data, Test Data or Product Feature Data.
   * Run post install checks and optimizations.

 * Since scripts are executed in path name order we can create numbered subfolders to manage object dependencies:
   * ```<dir>/D0/*.sql``` = Objects that have NO dependencies
   * ```<dir>/D1/*.sql``` = Objects that depend on ```D0/*.sql``` 
   * ```<dir>/D2/*.sql``` = Objects that depend on ```D1/*.sql```  etc.

Once you have extracted SQL code for all of your stored procedures and viewss into folders, you can run this SQL Server code to generate a shell script to move them into the correct folder so that they run in dependency order:

```

	/* 
	 * Generates code to MOVE stored procs into subdirectories corresponding to their dependency order
	 * Replace 'P'  with 'V' to select views, or 'IF' to select functions.
	 * Just copy/paste the 1st column into a batch script file.
	 */

	WITH TablesCTE(ObjType, SchemaName, ObjectName, ObjectID, Ordinal) AS
	(
		SELECT  so.type AS ObjType, OBJECT_SCHEMA_NAME(so.object_id) AS SchemaName, OBJECT_NAME(so.object_id) AS ObjectName, so.object_id AS ObjectID, 
		0 AS Ordinal
		FROM sys.objects AS so
		WHERE so.type IN ( 'P'  ) AND  so.is_ms_Shipped = 0

		UNION ALL

		SELECT so.type AS ObjType, OBJECT_SCHEMA_NAME(so.object_id) AS SchemaName, OBJECT_NAME(so.object_id) AS ObjectName, so.object_id AS ObjectID,
		tt.Ordinal + 1 AS Ordinal
		FROM sys.objects AS so
		INNER JOIN sys.sql_expression_dependencies AS dep ON dep.referencing_id = so.object_id 
		INNER JOIN TablesCTE AS tt ON dep.referenced_id = tt.ObjectID
		WHERE so.type IN ( 'P' ) AND so.is_ms_Shipped = 0
	)

	SELECT DISTINCT 'move ' + t.ObjectName + '.sql D' + CAST(tt.Ordinal as VARCHAR), tt.Ordinal, t.ObjType, t.SchemaName, t.ObjectName, t.ObjectID
		FROM TablesCTE AS t
		INNER JOIN
		(
			SELECT itt.ObjType, itt.SchemaName AS SchemaName, itt.ObjectName, itt.ObjectID, 
			   Max(itt.Ordinal) AS Ordinal
			FROM TablesCTE AS itt
			GROUP BY itt.ObjType, itt.SchemaName, itt.ObjectName, itt.ObjectID
		) AS tt
		ON t.ObjectID = tt.ObjectID AND t.Ordinal = tt.Ordinal
	ORDER BY tt.Ordinal, t.ObjType, t.SchemaName, t.ObjectName, t.ObjectID


```  

Generated C# Classes
====================

   * ```<version>```    = MigrationVersion MSBuild options or --version command line argument.  Example: "4.0.1"
   * ```<fm-version>``` = Fluent Migrator version stored in VersionInfo table.   Example: "40000236"

Install
-------

   * ```M1_Install/<version>/M<fm-version>_Inital.cs```` 
     * Sets initial step number 
   * ```M1_Install/<version>/M<fm-version>_Pre.cs```
     * Runs ```SQL/M1_Install/<version>/1_Pre/```
   * ```M1_Install/<version>/M<fm-version>_Create_<table>.cs```
     * Runs ```SQL/M1_Install/<version>/2_PerTable/cr_<table>.sql```
   * ```M1_Install/<version>/M<fm-version>_Post.cs```
     * Runs ```SQL/M1_Install/<version>/3_Post/```
   * ```M1_Install/<version>/M<fm-version>_Final.cs```      
      * Sets final step number 

Upgrade
-------
   * ```M2_Upgrade/<version>/M<fm-version>_Inital.cs```
     * Sets initial step number 
   * ```M2_Upgrade/<version>/M<fm-version>_Create_<table>.cs```
     * ```SQL/M2_Upgrade/<version>/2_PerTable/cr_<table>.sql```
   * ```M2_Upgrade/<version>/M<fm-version>_Upgrade_<table>.cs```
     * Runs: ```SQL/M2_Upgrade/<version>/2_PerTable/up_<table>.sql```
   * ```M2_Upgrade/<version>/M<fm-version>_Post.cs```
     * Runs: ```SQL/M2_Upgrade/<version>/3_Post/```
   * ```M2_Upgrade/<version>/M<fm-version>_Final.cs```
      * Sets final step number 
   
   * ```M3_Post/<version>/<Profile>.cs```
     * Create your own named [Profile] classes here that run ```SQL/M3_Post/<Profile>/*.sql```
     * Project Template include **SqlScriptMigration** as a base class to simplify these classes.

Command Line Options 
====================

 ```FluentMigrator.SchemaGen.EXE <options>```

See the [SchemaGenOptions.cs](SchemaGenOptions.cs) for command line options and help documentation.
Runing ```FluentMigrator.SchemaGen.EXE --help``` will also display the command line options.

MSBuild Task
============
   
  * See the [FmCodeGen.cs](MSBuild/FmCodeGen.cs) for **<FmCodeGen\>** MSBuild Task .
    * MSBuild task options are documented in matching command options: [SchemaGenOptions.cs](SchemaGenOptions.cs) (implements same interface as FmCodeGen.cs).
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
 * SQL Server Include columns in indexes are implemented by SqlServerSchemaReader but not tested and not implemented by code gen.
 * When a field type is altered, we currently don't handle the case where this field is part of a foriegn key relation.
   * Requires one or more FKs to be dropped and two or more tables altered together before FKs are recreated.
 * Executing each SQL directory maps to a single Migration class that is always performed as a single transaction that may fail due to transaction log limits.
   * The workaround is to split up SQL scripts into subfolders each corresponding to a transaction and then run each subfolder in a separate Migration class with TransactionPerSession set to false.
   * The Execute.ScriptDirectory() command includes the opton to not execute subfolders so we can just create subfolders for those scripts that require their own transaction.
   * A future version of SchemaGen can implement this by optionally generating a class per script folder in it's own Migration class.
   * We could perhaps introduce a "NEWTX" tag that runs a single script or folder in it's own Migration class.
 * When dropping tables there is code (currently commented out) to also drop related FKs. 
   * In test runs I found that FKs were already deleted. Not yet sure why. Needs investigation.

To Do
-----

 * Example command line args and MSBuild scripts.
   * An MSBuild script that emits an enitire C# project and compiles it.
 * Unit Tests 
 * Need to check if a Primary Key is non-clustered and emit: 
   * Create.PrimaryKey("PK").OnTable("TestTable").Column("Id").NonClustered();

Future Ideas
------------

 * Can extract code for all SPs, Views, Functions, User defined types and put them into correct dependency order directories.
   * Generate code to deleted all removed SPs, Views, Functions and Type objects.
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
