﻿using System.Collections.Generic;
using System.Text.Json;
using LinqToDB.Naming;

namespace LinqToDB.CLI
{
	/// <summary>
	/// Code identifier normalization/generation options. Not supported in CLI (JSON only).
	/// </summary>
	/// <param name="Name">Option name (used as JSON property name).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Default">Optional default value, used when user didn't specified option explicitly.</param>
	internal sealed record NamingCliOption(
		string                Name,
		string                Help,
		string?               DetailedHelp,
		string[]?             JsonExamples,
		NormalizationOptions? Default)
		: CliOption(
			Name,
			null,
			OptionType.Naming,
			false,
			false,
			true,
			false,
			Help,
			DetailedHelp,
			null,
			JsonExamples)
	{
		public override object? ParseCLI(CliCommand command, string rawValue)
		{
			// not supported from CLI
			return null;
		}

		public override object? ParseJSON(JsonElement rawValue)
		{
			if (rawValue.ValueKind != JsonValueKind.Object)
				return null;

			var properties = new HashSet<string>();

			var options = new NormalizationOptions();

			foreach (var property in rawValue.EnumerateObject())
			{
				if (!properties.Add(property.Name))
					return null;

				switch (property.Name)
				{
					case "case"                            :
						if (property.Value.ValueKind != JsonValueKind.String)
							return null;

						switch (property.Value.GetString()!)
						{
							case "none"         : options.Casing = NameCasing.None                 ; break;
							case "camel_case"   : options.Casing = NameCasing.CamelCase            ; break;
							case "lower_case"   : options.Casing = NameCasing.LowerCase            ; break;
							case "pascal_case"  : options.Casing = NameCasing.Pascal               ; break;
							case "snake_case"   : options.Casing = NameCasing.SnakeCase            ; break;
							case "t4"           : options.Casing = NameCasing.T4CompatNonPluralized; break;
							case "t4_pluralized": options.Casing = NameCasing.T4CompatPluralized   ; break;
							case "upper_case"   : options.Casing = NameCasing.UpperCase            ; break;
							default             : return null;
						}

						break;
					case "pluralization"                   :
						if (property.Value.ValueKind != JsonValueKind.String)
							return null;

						switch (property.Value.GetString()!)
						{
							case "none"                      : options.Pluralization =Pluralization.None                 ; break;
							case "plural"                    : options.Pluralization =Pluralization.Plural               ; break;
							case "plural_multiple_characters": options.Pluralization =Pluralization.PluralIfLongerThanOne; break;
							case "singular"                  : options.Pluralization =Pluralization.Singular             ; break;
							default                          : return null;
						}
						
						break;
					case "prefix"                          :
						if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
							options.Prefix = null;
						else if (property.Value.ValueKind == JsonValueKind.String)
							options.Prefix = property.Value.GetString()!;
						else
							return null;
						break;
					case "suffix"                          :
						if (property.Value.ValueKind == JsonValueKind.Null || property.Value.ValueKind == JsonValueKind.Undefined)
							options.Suffix = null;
						else if (property.Value.ValueKind == JsonValueKind.String)
							options.Suffix = property.Value.GetString()!;
						else
							return null;
						break;
					case "transformation"                  :
						if (property.Value.ValueKind != JsonValueKind.String)
							return null;

						switch (property.Value.GetString()!)
						{
							case "split_by_underscore": options.Transformation = NameTransformation.SplitByUnderscore; break;
							case "t4"                 : options.Transformation = NameTransformation.T4Compat         ; break;
							default                   : return null;
						}
						
						break;
					case "pluralize_if_ends_with_word_only":
						if (property.Value.ValueKind == JsonValueKind.True)
							options.PluralizeOnlyIfLastWordIsText = true;
						else if (property.Value.ValueKind == JsonValueKind.False)
							options.PluralizeOnlyIfLastWordIsText = false;
						else
							return null;
						break;
					case "ignore_all_caps"                 :
						if (property.Value.ValueKind == JsonValueKind.True)
							options.DontCaseAllCaps = true;
						else if (property.Value.ValueKind == JsonValueKind.False)
							options.DontCaseAllCaps = false;
						else
							return null;
						break;
					default                                :
						return null;
				}
			}

			return options;
		}
	}
}
