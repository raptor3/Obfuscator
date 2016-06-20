using Mono.Cecil;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Obfuscator.SkipRules
{
	[Serializable()]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class SkipType : ISkipType, ISkipField, ISkipProperty, ISkipMethod
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("skipMethods")]
		public bool SkipMethods { get; set; }

		[XmlAttribute("skipFields")]
		public bool SkipFields { get; set; }

		[XmlAttribute("skipProperties")]
		public bool SkipProperties { get; set; }

		public bool IsTypeSkip(TypeReference type)
		{
			return Regex.IsMatch(type.Name, Name);
		}

		public bool IsMethodSkip(MethodReference method)
		{
			return Regex.IsMatch(method.DeclaringType.Name, Name) && SkipMethods;
		}

		public bool IsPropertySkip(PropertyReference prop)
		{
			return Regex.IsMatch(prop.DeclaringType.Name, Name) && SkipProperties;
		}

		public bool IsFieldSkip(FieldReference field)
		{
			return Regex.IsMatch(field.DeclaringType.Name, Name) && SkipFields;
		}
	}
}
