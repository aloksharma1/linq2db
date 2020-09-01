﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class TableAttributeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(
				nameof(LinqExtensions.TableName),
				nameof(LinqExtensions.ServerName),
				nameof(LinqExtensions.DatabaseName),
				nameof(LinqExtensions.SchemaName),
				nameof(LinqExtensions.IsTemporary),
				nameof(LinqExtensions.TableOptions));
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = (TableBuilder.TableContext)sequence;
			var value    = methodCall.Arguments[1].EvaluateExpression();

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.TableName)    : table.SqlTable.PhysicalName = (string?)     value!; break;
				case nameof(LinqExtensions.ServerName)   : table.SqlTable.Server       = (string?)     value;  break;
				case nameof(LinqExtensions.DatabaseName) : table.SqlTable.Database     = (string?)     value;  break;
				case nameof(LinqExtensions.SchemaName)   : table.SqlTable.Schema       = (string?)     value;  break;
				case nameof(LinqExtensions.TableOptions) : table.SqlTable.TableOptions = (TableOptions)value!; break;
				case nameof(LinqExtensions.IsTemporary)  :
					if ((bool)value!)
						table.SqlTable.TableOptions |=  TableOptions.IsTemporary;
					else
						table.SqlTable.TableOptions &= ~TableOptions.IsTemporary;
					break;
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
