﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class ContextRefBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		static ProjectFlags GetRootProjectFlags(BuildInfo buildInfo)
		{
			return buildInfo.IsAggregation ? ProjectFlags.AggregtionRoot : ProjectFlags.Root;
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, GetRootProjectFlags(buildInfo));
			if (ExpressionEqualityComparer.Instance.Equals(root, buildInfo.Expression))
			{
				if (root is ContextRefExpression)
					return true;
				return false;
			}

			return builder.IsSequence(new BuildInfo(buildInfo, root));
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, GetRootProjectFlags(buildInfo));
			if (!ExpressionEqualityComparer.Instance.Equals(root, buildInfo.Expression) || root is not ContextRefExpression contextRef)
				return builder.BuildSequence(new BuildInfo(buildInfo, root));

			var context = contextRef.BuildContext;

			var elementContext = context.GetContext(buildInfo.Expression, 0, buildInfo);
			if (elementContext != null)
				return elementContext;

			return context;
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var root = builder.MakeExpression(buildInfo.Expression, GetRootProjectFlags(buildInfo));
			if (!ExpressionEqualityComparer.Instance.Equals(root, buildInfo.Expression))
				return builder.IsSequence(new BuildInfo(buildInfo, root));

			if (buildInfo.Expression is not ContextRefExpression contextRef)
				return false;

			if (buildInfo.IsAggregation)
				return contextRef.BuildContext is GroupByBuilder.GroupByContext;

			return true;
		}
	}
}
