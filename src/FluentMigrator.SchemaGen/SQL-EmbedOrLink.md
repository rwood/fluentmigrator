A single migration DLL can now support the complete Install and Upgrade process including execution of your custom SQL scripts that can perform the following in correct dependency order: 

- Pre-install: Create an configure new databases
- Pre-upgrade: Checks and updates
- Per table updates that are run as each table is being migrated (both old and new fields are available to assist in data migration).
- Post-install/Post Upgrade migration updates and checks.
- Install Views, Store Procedures, Data Types
- Seed Data, Demo Data and Test Data
- 
An SQL script folder structure is documented that can support installing and upgrading to multiple database versions/instances/types from one migration DLL.

I've added a new Execute.SqlScriptDirectory() method to the FM API that can execute an entire directory of scripts (Example: your folder of views or stored procedure scripts).  Scripts are always executed in order of their full path name. You can use numbered sub-directories to enforce execution in dependency order (See README.md for more details). 

Tagging of SQL scripts.
-----------------------

While FluentMigrator reduces the duplication of code between different database types, we can still often end up managing duplicate or near duplicate csutomer SQL scripts for each database type or instance. Juggling the duplication of code and folder structures can be a nightmare. 

To address this, Execute.SqlScriptDirectory()  supports a simple naming convention that tags the set of SQL scripts that are run for a given database type, database instance, hostname or whatever rule you need. 

**Examples:**

In my case we had two MS-Access databases instances and one SQL Server database.  I create a simple mapping rule n C# to convert the connection string the database instance tags: "AC1", "AC2" and "SS".  I also needed tags to select test data and demo data. To be executed, an SQL script path need to contain all of the required tags (delimited by "_" or "\\" or "/").


- ```SQL\Pre\SS\create_db.sql```     - Run if "SS" tag is required but not for AC1 or AC2 database instances.
- ```SQL\Pre\demo_data_AC1_SS.sql``` - Runs for either SQL Server or the 1st MS-Access database.
- ```SQL\Pre\demo_data_AC2.sql```    - Only for 2nd MS-Access database.
- ```SQL\Post\AC1_AC2\*.sql```       - Runs for both MS-Access instances.

The main idea is that you can elect to either place related scripts in the same or different folders, whichever works best for this install/update release. 

Embed or Link your SQL scripts
------------------------------

The new FluentMigrator.SchemaGen code generator can now integrates your custom SQL scripts into the generated Migration classes. SQL scripts can be either Linked (best for development) or embedded (best for deployment). Embedding the scripts imports all the SQL code into generated classes so you only need to deploy you migration DLL  (no external SQL files).  

During development, you can set EmbedSql="false" which will generates code to execute your external SQL files and folders at run-time. This avoids having to re-run the code generator each time you update an SQL file. In this mode, the code generator will create a class for every table so it will run find and any matching "per table" SQL script you might write. When you switch to EmbedSql="true", it will only create a table migration class if the table schema changes OR it finds an SQL script file for the table.

There is one known issue that is not addressed by the current implementation.  Since executing each SQL directory maps to a single Migration class that is always performed as a single transaction that may fail due to transaction log limits. The workaround is to split up SQL scripts into subfolders each corresponding to a transaction and then run each subfolder in a separate Migration class with TransactionPerSession set to false. The Execute.ScriptDirectory() command includes the opton to not execute subfolders so we can just create subfolders for those scripts that require their own transaction. A future version of SchemaGen can implement this by optionally generating a class per script folder in it's own Migration class.  We could also perhaps introduce a "NEWTX" tag that runs a single script or folder in it's own Migration class.