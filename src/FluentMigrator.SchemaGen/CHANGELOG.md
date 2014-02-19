18-Feb-2014
* ADD: Refactored to simplify code in FmDiffMigrationWriter.CreateUpdateTable() and split up code into smaller classes to prepare for unit testing.
  * Moved object specific rendering to Model/*Ext.cs classes.
  * Added CodeLines class to manage low level output rendering.
  * Added SqlFileWriter class to embed or link SQL files and folders.
* FIX: Now supports SchemaName in all object comparisions and emitted code.
* ADD: Improved layout.
* ADD: Index fill factor added to FM API and SchemaGen