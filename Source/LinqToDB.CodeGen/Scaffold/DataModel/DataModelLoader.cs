﻿using System.Collections.Generic;
using LinqToDB.CodeGen.CodeGeneration;
using LinqToDB.CodeGen.ContextModel;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.Naming;
using LinqToDB.Schema;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// Implements database schema load and conversion to data model.
	/// </summary>
	public sealed partial class DataModelLoader
	{
		private record TableWithEntity(TableLikeObject TableOrView, EntityModel Entity);

		// language-specific naming services for initial normalization of identifiers in data model
		// (generation of valid identifiers without name conflicts resolution)
		private readonly NamingServices         _namingServices;
		/// language services provider
		private readonly ILanguageProvider      _languageProvider;
		// database schema provider
		private readonly ISchemaProvider        _schemaProvider;
		// database to .net type mapping provider
		private readonly ITypeMappingProvider   _typeMappingsProvider;

		// TODO: remove/refactor
		private readonly CodeGenerationSettings _codegenSettings;
		private readonly ContextModelSettings   _contextSettings;
		private readonly SchemaSettings         _schemaSettings;

		// lookups for created data model objects:
		// entity model lookup by schema table/view object (e.g. for conversion of foreign keys to associations)
		private readonly Dictionary<ObjectName, TableWithEntity>   _entities = new ();
		// column model lookup
		private readonly Dictionary<EntityModel, Dictionary<string, ColumnModel>> _columns  = new ();

		public DataModelLoader(
			NamingServices         namingServices,
			ILanguageProvider      languageProvider,
			CodeGenerationSettings codegenSettings,
			ISchemaProvider        schemaProvider,
			ContextModelSettings   contextSettings,
			SchemaSettings         schemaSettings,
			ITypeMappingProvider   typeMappingsProvider)
		{
			_namingServices       = namingServices;
			_languageProvider     = languageProvider;
			_codegenSettings      = codegenSettings;
			_schemaProvider       = schemaProvider;
			_contextSettings      = contextSettings;
			_schemaSettings       = schemaSettings;
			_typeMappingsProvider = typeMappingsProvider;
		}

		/// <summary>
		/// Loads database schema into <see cref="DatabaseModel"/> object.
		/// </summary>
		/// <returns>Loaded database model instance.</returns>
		public DatabaseModel LoadSchema()
		{
			// create empty data model and set initial options
			var dataContext                    = BuildDataContext();
			var model                          = new DatabaseModel(dataContext);
			model.NRTEnabled                   = _languageProvider.NRTSupported && _codegenSettings.NullableReferenceTypes;
			model.DisableXmlDocWarnings        = _languageProvider.MissingXmlCommentWarnCodes.Length > 0 && _codegenSettings.SuppressMissingXmlCommentWarnings;
			model.OrderFindParametersByOrdinal = _codegenSettings.OrderFindParametersByColumnOrdinal;

			if (_codegenSettings.MarkAsAutoGenerated)
			{
				// default header
				model.AutoGeneratedHeader = @"This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.";
			}

			// base type for entities (if specified)
			IType? baseEntityType = null;
			if (_contextSettings.BaseEntityClass != null)
				baseEntityType = _languageProvider.TypeParser.Parse(_contextSettings.BaseEntityClass, false);

			// list of default database schemas (objects in those schemas will be added to main data context)
			var defaultSchemas = _schemaProvider.GetDefaultSchemas();
			// load enabled database objects and convert them to data model

			// load tables as entities
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.Table))
			{
				foreach (var table in _schemaProvider.GetTables())
					BuildEntity(dataContext, table, table.PrimaryKey, table.Identity, defaultSchemas, baseEntityType);
			}

			// load views as entities
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.View))
			{
				foreach (var view in _schemaProvider.GetViews())
					BuildEntity(dataContext, view, null, view.Identity, defaultSchemas, baseEntityType);
			}

			// load foreign keys as associations
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.ForeignKey))
			{
				foreach (var fk in _schemaProvider.GetForeignKeys())
				{
					var association = BuildAssociations(fk, defaultSchemas);
					if (association != null)
						dataContext.Associations.Add(association);
				}
			}

			// load stored procedures
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.StoredProcedure))
			{
				foreach (var proc in _schemaProvider.GetProcedures(_schemaSettings.LoadProceduresSchema, _schemaSettings.UseSafeSchemaLoad))
					BuildStoredProcedure(dataContext, proc, defaultSchemas);
			}

			// load table functions
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.TableFunction))
			{
				foreach (var func in _schemaProvider.GetTableFunctions(_schemaSettings.LoadTableFunctionsSchema, _schemaSettings.UseSafeSchemaLoad))
					BuildTableFunction(dataContext, func, defaultSchemas);
			}

			// load scalar functions
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.ScalarFunction))
			{
				foreach (var func in _schemaProvider.GetScalarFunctions())
					BuildScalarFunction(dataContext, func, defaultSchemas);
			}

			// load aggregate functions
			if (_schemaSettings.Objects.HasFlag(SchemaObjects.AggregateFunction))
			{
				foreach (var func in _schemaProvider.GetAggregateFunctions())
					BuildAggregateFunction(dataContext, func, defaultSchemas);
			}

			return model;
		}
	}
}
