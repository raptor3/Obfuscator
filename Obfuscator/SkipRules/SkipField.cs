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
	public class SkipField : ISkipField
	{

		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		public bool IsFieldSkip(FieldReference field)
		{
			return Regex.IsMatch(field.DeclaringType.FullName, Type) && Regex.IsMatch(field.Name, Name);
		}
	}
}
