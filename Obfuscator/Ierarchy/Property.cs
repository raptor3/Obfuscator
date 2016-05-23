using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Obfuscator
{
	public class Property
	{
		List<PropertyReference> references = new List<PropertyReference>();

		public void Add(PropertyReference propRef)
		{
			references.Add(propRef);
		}
	}
}
