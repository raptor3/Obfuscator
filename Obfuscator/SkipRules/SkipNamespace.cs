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

		[XmlAttribute("SkipTypes")]
		public bool SkipTypes { get; set; }

		public bool IsNamespaceSkip(string namespaceName)
		{
			return Regex.IsMatch(namespaceName, Name);
		}

		public bool IsTypeSkip(TypeReference type)
		{
			return Regex.IsMatch(type.Namespace, Name) && SkipTypes;
		}

		public bool IsMethodSkip(MethodReference method)
		{
			return Regex.IsMatch(method.DeclaringType.Namespace, Name) && SkipTypes;
		}

		public bool IsPropertySkip(PropertyReference prop)
		{
			return Regex.IsMatch(prop.DeclaringType.Namespace, Name) && SkipTypes;
		}

		public bool IsFieldSkip(FieldReference field)
		{
			return Regex.IsMatch(field.DeclaringType.Namespace, Name) && SkipTypes;
		}
	}
}
