using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
namespace Obfuscator
{
	public class Type
	{
		List<TypeReference> references = new List<TypeReference>();
		TypeDefinition definition;
		public void Add(TypeReference typeRef)
		{
			references.Add(typeRef);
		}

		public void RegisterField(FieldDefinition field)
		{
			
		}
	}
}
