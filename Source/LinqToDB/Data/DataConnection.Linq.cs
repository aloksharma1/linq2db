﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Interceptors;

namespace LinqToDB.Data
{
	using System.Data.Common;
	using DataProvider;
	using Linq;
	using SqlProvider;
	using SqlQuery;

	public partial class DataConnection
	{
		// TODO: v4: remove both GetTable methods
		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to database table or view.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <returns>Queryable source.</returns>
		public ITable<T> GetTable<T>()
			where T : class
		{
			return new Table<T>(this);
		}

		/// <summary>
		/// Returns queryable source for specified mapping class for current connection, mapped to table expression or function.
		/// It could be used e.g. for queries to table-valued functions or to decorate queried table with hints.
		/// </summary>
		/// <typeparam name="T">Mapping class type.</typeparam>
		/// <param name="instance">Instance object for <paramref name="methodInfo"/> method or null for static method.</param>
		/// <param name="methodInfo">Method, decorated with expression attribute, based on <see cref="Sql.TableFunctionAttribute"/>.</param>
		/// <param name="parameters">Parameters for <paramref name="methodInfo"/> method.</param>
		/// <returns>Queryable source.</returns>
		public ITable<T> GetTable<T>(object instance, MethodInfo methodInfo, params object?[] parameters)
			where T : class
		{
			return DataExtensions.GetTable<T>(this, instance, methodInfo, parameters);
		}

		protected virtual SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
		{
			return statement;
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;
		Type             IDataContext.DataReaderType        => DataProvider.DataReaderType;

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			CheckAndThrowOnDisposed();

			if (forNestedQuery && _connection != null && IsMarsEnabled)
				return new DataConnection(DataProvider, _connection.Connection)
				{
					MappingSchema             = MappingSchema,
					TransactionAsync          = TransactionAsync,
					IsMarsEnabled             = IsMarsEnabled,
					ConnectionString          = ConnectionString,
					RetryPolicy               = RetryPolicy,
					CommandTimeout            = CommandTimeout,
					InlineParameters          = InlineParameters,
					ThrowOnDisposed           = ThrowOnDisposed,
					_queryHints               = _queryHints?.Count > 0 ? _queryHints.ToList() : null,
					OnTraceConnection         = OnTraceConnection,
					_commandInterceptor       = _commandInterceptor      .CloneAggregated(),
					_connectionInterceptor    = _connectionInterceptor   .CloneAggregated(),
					_dataContextInterceptor   = _dataContextInterceptor  .CloneAggregated(),
					_entityServiceInterceptor = _entityServiceInterceptor.CloneAggregated(),
				};

			return (DataConnection)Clone();
		}

		string IDataContext.ContextID => DataProvider.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider => () => DataProvider.CreateSqlBuilder(MappingSchema);

		static Func<ISqlOptimizer> GetGetSqlOptimizer(IDataProvider dp)
		{
			return dp.GetSqlOptimizer;
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer => GetGetSqlOptimizer(DataProvider);

		#endregion
	}
}
