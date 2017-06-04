using System.Collections.Generic;

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
				if (!Methods.Contains(first))
				{
					Methods.Add(first);
				}
			}
			else
			{
				foreach (var m in first.Group.Methods)
				{
					if (!Methods.Contains(m))
					{
						Methods.Add(m);
					}
				}
			}
		}

		public MethodGroup(Method m)
		{
			Methods.Add(m);
		}
	}
}
