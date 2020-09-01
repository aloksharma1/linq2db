﻿using System;

namespace LinqToDB.Data
{
	/// <summary>
	/// Defines behavior of <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, BulkCopyOptions, System.Collections.Generic.IEnumerable{T})"/> method.
	/// </summary>
	public class BulkCopyOptions
	{
		public BulkCopyOptions()
		{
		}

		public BulkCopyOptions(BulkCopyOptions options)
		{
			MaxBatchSize           = options.MaxBatchSize;
			BulkCopyTimeout        = options.BulkCopyTimeout;
			BulkCopyType           = options.BulkCopyType;
			CheckConstraints       = options.CheckConstraints;
			KeepIdentity           = options.KeepIdentity;
			TableLock              = options.TableLock;
			KeepNulls              = options.KeepNulls;
			FireTriggers           = options.FireTriggers;
			UseInternalTransaction = options.UseInternalTransaction;
			ServerName             = options.ServerName;
			DatabaseName           = options.DatabaseName;
			SchemaName             = options.SchemaName;
			TableName              = options.TableName;
			TableOptions           = options.TableOptions;
			NotifyAfter            = options.NotifyAfter;
			RowsCopiedCallback     = options.RowsCopiedCallback;
		}

		/// <summary>Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</summary>
		/// <returns>The integer value of the <see cref="P:MaxBatchSize"></see> property, or zero if no value has been set.</returns>
		public int?         MaxBatchSize           { get; set; }
		public int?         BulkCopyTimeout        { get; set; }
		public BulkCopyType BulkCopyType           { get; set; }
		public bool?        CheckConstraints       { get; set; }

		/// <summary>
		/// If this option set to true, bulk copy will use values of columns, marked with IsIdentity flag.
		/// SkipOnInsert flag in this case will be ignored.
		/// Otherwise those columns will be skipped and values will be generated by server.
		/// Not compatible with <see cref="LinqToDB.Data.BulkCopyType.RowByRow"/> mode.
		/// </summary>
		public bool?        KeepIdentity           { get; set; }
		public bool?        TableLock              { get; set; }
		public bool?        KeepNulls              { get; set; }
		public bool?        FireTriggers           { get; set; }
		public bool?        UseInternalTransaction { get; set; }

		/// <summary>
		/// Gets or sets explicit name of target server instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.
		/// Also note that it is not supported by provider-specific insert method.
		/// </summary>
		public string?      ServerName             { get; set; }
		/// <summary>
		/// Gets or sets explicit name of target database instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string?      DatabaseName           { get; set; }
		/// <summary>
		/// Gets or sets explicit name of target schema/owner instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		public string?      SchemaName             { get; set; }
		/// <summary>
		/// Gets or sets explicit name of target table instead of one, configured for copied entity in mapping schema.
		/// </summary>
		public string?      TableName              { get; set; }
		/// <summary>
		/// Gets or sets explicit IsTemporary flag instead of one, configured for copied entity in mapping schema.
		/// See <see cref="LinqExtensions.IsTemporary{T}(ITable{T}, bool)"/> method for support information per provider.
		/// </summary>
		public TableOptions TableOptions           { get; set; }

		/// <summary>
		/// Gets or sets counter after how many copied records <see cref="RowsCopiedCallback"/> should be called.
		/// E.g. if you set it to 10, callback will be called after each 10 copied records.
		/// To disable callback, set this option to 0 (default value).
		/// </summary>
		public int          NotifyAfter            { get; set; }

		/// <summary>
		/// Gets or sets callback method that will be called by BulkCopy operation after each <see cref="NotifyAfter"/> rows copied.
		/// This callback will not be used if <see cref="NotifyAfter"/> set to 0.
		/// </summary>
		public Action<BulkCopyRowsCopied>? RowsCopiedCallback { get; set; }
	}
}
