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
	public class SkipNamespace : ISkipNamespace, ISkipType, ISkipField, ISkipProperty, ISkipMethod
	{

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("skipTypes")]
		public bool SkipTypes { get; set; }

		public bool IsNamespaceSkip(string namespaceName)
		{
			return Regex.IsMatch(namespaceName, Name);
		}

		public bool IsTypeSkip(TypeReference type)
		{
			return SkipTypes && Regex.IsMatch(type.Namespace, Name);
		}

		public bool IsMethodSkip(MethodReference method)
		{
			return SkipTypes && Regex.IsMatch(method.DeclaringType.Namespace, Name);
		}

		public bool IsPropertySkip(PropertyReference prop)
		{
			return SkipTypes && Regex.IsMatch(prop.DeclaringType.Namespace, Name);
		}

		public bool IsFieldSkip(FieldReference field)
		{
			return SkipTypes && Regex.IsMatch(field.DeclaringType.Namespace, Name);
		}
	}
}
