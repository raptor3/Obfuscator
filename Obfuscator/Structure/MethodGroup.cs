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
	}
}
