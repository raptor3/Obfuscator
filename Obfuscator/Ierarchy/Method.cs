using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Obfuscator
{
	public class Method
	{
		List<MethodReference> references = new List<MethodReference>();

		public void Add(MethodReference methodRef)
		{
			references.Add(methodRef);
		}
	}
}
