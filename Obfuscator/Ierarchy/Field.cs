using System.Collections.Generic;
using Mono.Cecil;

namespace Obfuscator
{
	public class Field
	{
		List<FieldReference> references = new List<FieldReference>();

		public void Add(FieldReference fieldRef)
		{
			references.Add(fieldRef);
		}
	}
}
