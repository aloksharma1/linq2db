﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Tools;
	using Extensions;
	using SqlQuery;
	using SqlProvider;

	class SqlCeSqlOptimizer : BasicSqlOptimizer
	{
		public SqlCeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			// This function mutates statement which is allowed only in this place
			CorrectSkipAndColumns(statement);

			// This function mutates statement which is allowed only in this place
			CorrectInsertParameters(statement);

			CorrectFunctionParameters(statement);

			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement) statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement) statement);
					break;
			}


			// call fixer after CorrectSkipAndColumns for remaining cases
			base.FixEmptySelect(statement);

			return statement;
		}

		protected static string[] LikeSqlCeCharactersToEscape = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeSqlCeCharactersToEscape;

		void CorrectInsertParameters(SqlStatement statement)
		{
			//SlqCe do not support parameters in columns for insert
			//
			if (statement.IsInsert())
			{
				var query = statement.SelectQuery;
				if (query != null)
				{
					foreach (var column in query.Select.Columns)
					{
						if (column.Expression is SqlParameter parameter)
						{
							parameter.IsQueryParameter = false;
						}
					}
				}
			}
		}

		void CorrectSkipAndColumns(SqlStatement statement)
		{
			new QueryVisitor().Visit(statement, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlQuery:
						{
							var q = (SelectQuery)e;

							if (q.Select.SkipValue != null && q.OrderBy.IsEmpty)
							{
								if (q.Select.Columns.Count == 0)
								{
									var source = q.Select.From.Tables[0].Source;
									var keys   = source.GetKeys(true);

									foreach (var key in keys)
									{
										q.Select.AddNew(key);
									}
								}

								for (var i = 0; i < q.Select.Columns.Count; i++)
									q.OrderBy.ExprAsc(q.Select.Columns[i].Expression);

								if (q.OrderBy.IsEmpty)
								{
									throw new LinqToDBException("Order by required for Skip operation.");
								}
							}

							// looks like SqlCE do not allow '*' for grouped records
							if (!q.GroupBy.IsEmpty && q.Select.Columns.Count == 0)
							{
								q.Select.Add(new SqlValue(1));
							}

							break;
						}
				}
			});
		}

		void CorrectFunctionParameters(SqlStatement statement)
		{
			if (!SqlCeConfiguration.InlineFunctionParameters)
				return;

			new QueryVisitor().Visit(statement, e =>
			{
				if (e.ElementType == QueryElementType.SqlFunction)
				{
					var sqlFunction = (SqlFunction)e;
					foreach (var parameter in sqlFunction.Parameters)
					{
						if (parameter.ElementType == QueryElementType.SqlParameter &&
						    parameter is SqlParameter sqlParameter)
						{
							sqlParameter.IsQueryParameter = false;
						}
					}
				}
			});
		}

		protected override void FixEmptySelect(SqlStatement statement)
		{
			// already fixed by CorrectSkipAndColumns
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor,
			EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, visitor, context);

			switch (expr)
			{
				case SqlBinaryExpression be:
					switch (be.Operation)
					{
						case "%":
							return be.Expr1.SystemType!.IsIntegerType()?
								be :
								new SqlBinaryExpression(
									typeof(int),
									new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
									be.Operation,
									be.Expr2,
									be.Precedence);
						case "+":
						{
							if (GenerateDateAdd(be.Expr1, be.Expr2, false, context, out var generated))
								return generated;

							if (GenerateDateAdd(be.Expr2, be.Expr1, false, context, out generated))
								return generated;

							break;
						}
						case "-":
						{
							if (GenerateDateAdd(be.Expr1, be.Expr2, true, context, out var generated))
								return generated;

							break;
						}
					}

					break;

				case SqlFunction func:
					switch (func.Name)
					{
						case "Convert" :
							switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
							{
								case TypeCode.UInt64 :
									if (func.Parameters[1].SystemType!.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											false,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									break;

								case TypeCode.DateTime :
									var type1 = func.Parameters[1].SystemType!.ToUnderlying();

									if (IsTimeDataType(func.Parameters[0]))
									{
										if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
											return new SqlExpression(
												func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

										if (func.Parameters[1].SystemType == typeof(string))
											return func.Parameters[1];

										return new SqlExpression(
											func.SystemType, "Convert(NChar, {0}, 114)", Precedence.Primary, func.Parameters[1]);
									}

									if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
									{
										if (IsDateDataType(func.Parameters[0], "Datetime"))
											return new SqlExpression(
												func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
									}

									break;
							}

							break;
					}

					break;
			}

			return expr;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);
			return base.ConvertFunction(func);
		}

		public override bool HasSpecialTimeSpanProcessing => true;

		static bool GenerateDateAdd(ISqlExpression expr1, ISqlExpression expr2, bool isSubstraction, EvaluationContext context,
			[MaybeNullWhen(false)] out ISqlExpression generated)
		{
			var dbType1 = expr1.GetExpressionType();
			var dbType2 = expr2.GetExpressionType();

			if (dbType1.SystemType.ToNullableUnderlying().In(typeof(DateTime), typeof(DateTimeOffset))
			    && dbType2.SystemType.ToNullableUnderlying() == typeof(TimeSpan)
			    && expr2.TryEvaluateExpression(context, out var value))
			{
				var ts = value as TimeSpan?;
				var interval = "day";
				long? increment;

				if (ts == null)
				{
					generated = new SqlValue(dbType1, null);
					return true;
				}

				if (ts.Value.Milliseconds > 0)
				{
					increment = (long)ts.Value.TotalMilliseconds;
					interval = "millisecond";
				}
				else if (ts.Value.Seconds > 0)
				{
					increment = (long)ts.Value.TotalSeconds;
					interval = "second";
				}
				else if (ts.Value.Minutes > 0)
				{
					increment = (long)ts.Value.TotalMinutes;
					interval = "minute";
				}
				else if (ts.Value.Hours > 0)
				{
					increment = (long)ts.Value.TotalHours;
					interval = "hour";
				}
				else
				{
					increment = (long)ts.Value.TotalDays;
				}

				if (isSubstraction)
					increment = -increment;

				generated = new SqlFunction(
						dbType1.SystemType!,
						"DateAdd",
						false,
						true,
						new SqlExpression(typeof(string), interval, Precedence.Primary),
						CreateSqlValue(increment, new DbDataType(typeof(long)), expr2),
						expr1)
					{ CanBeNull = expr1.CanBeNull || expr2.CanBeNull };
				return true;
			}


			generated = null;
			return false;
		}


	}
}
