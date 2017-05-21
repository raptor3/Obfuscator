using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Obfuscator.Structure
{
	public class MethodGroup
	{
		private List<Method> methods = new List<Method>();

		public List<Method> Methods { get { return methods; } }

		public MethodGroup(Method first, Method second)
		{
			if (second.Group == null)
			{
				this.Methods.Add(second);
			}
			else
			{
				this.Methods.AddRange(second.Group.Methods);
			}
			if (first.Group == null)
			{
				this.Methods.Add(first);
			}
			else
			{
				this.Methods.AddRange(first.Group.Methods);
			}
		}

		public MethodGroup(Method m)
		{
			Methods.Add(m);
		}
	}
}
