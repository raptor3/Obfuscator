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
	public class SkipMethod : ISkipMethod
	{

		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		public bool IsMethodSkip(MethodReference method)
		{
			return Regex.IsMatch(method.DeclaringType.FullName, Type) && Regex.IsMatch(method.Name, Name);
		}
	}
}
