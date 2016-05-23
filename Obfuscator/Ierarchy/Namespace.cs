using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Obfuscator
{
	public class Namespace
	{
		Dictionary<string, Type> types = new Dictionary<string, Type>();

		public Type RegisterType(TypeReference typeRef)
		{
			Type type;
			if (!types.TryGetValue(typeRef.FullName, out type))
			{
				type = new Type();
				type.Add(typeRef);
				types.Add(typeRef.FullName, type);
			}
			return type;
		}
	}
}
