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
	public class SkipProperty : ISkipProperty
	{

		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }


		public bool IsPropertySkip(PropertyReference prop)
		{
			return Regex.IsMatch(prop.DeclaringType.FullName, Type) && Regex.IsMatch(prop.Name, Name);
		}

	}
}
