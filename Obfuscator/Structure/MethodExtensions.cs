using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator.Structure
{
	public static class MethodExtensions
	{
		public static bool Overrides(this MethodDefinition method, MethodReference overridden)
		{

			bool explicitIfaceImplementation = method.Overrides.Any(overrides => overrides.IsEqual(overridden));
			if (explicitIfaceImplementation)
			{
				return true;
			}

			if (IsImplicitInterfaceImplementation(method, overridden))
			{
				return true;
			}

			// new slot method cannot override any base classes' method by convention:
			if (method.IsNewSlot)
			{
				return false;
			}

			// check base-type overrides using Cecil's helper method GetOriginalBaseMethod()
			return method.GetOriginalBaseMethod().IsEqual(overridden);
		}

		/// <summary>
		/// Implicit interface implementations are based only on method's name and signature equivalence.
		/// </summary>
		private static bool IsImplicitInterfaceImplementation(this MethodDefinition method, MethodReference overridden)
		{
			// check that the 'overridden' method is iface method and the iface is implemented by method.DeclaringType
			if (overridden.DeclaringType.Resolve().IsInterface == false ||
				!method.DeclaringType.Interfaces.Any(i => i.IsEqual(overridden.DeclaringType)))
			{
				return false;
			}

			// check whether the type contains some other explicit implementation of the method
			if (method.DeclaringType.Methods.SelectMany(m => m.Overrides).Any(m => m.IsEqual(overridden)))
			{
				// explicit implementation -> no implicit implementation possible
				return false;
			}

			// now it is enough to just match the signatures and names:
			return method.Name == overridden.Name && method.SignatureMatches(overridden);
		}

		static bool IsEqual(this MethodReference method1, MethodReference method2)
		{
			return method1.Name == method2.Name && method1.DeclaringType.IsEqual(method2.DeclaringType);
		}

		static bool IsEqual(this TypeReference method1, TypeReference method2)
		{
			return method1.FullName == method2.FullName;
		}

		public static bool SignatureMatches(this IMethodSignature self, IMethodSignature signature)
		{
			if (self == null)
				return (signature == null);

			if (self.HasThis != signature.HasThis)
				return false;
			if (self.ExplicitThis != signature.ExplicitThis)
				return false;
			if (self.CallingConvention != signature.CallingConvention)
				return false;

			if (!self.ReturnType.IsEqual(signature.ReturnType))
				return false;

			bool h1 = self.HasParameters;
			bool h2 = signature.HasParameters;
			if (h1 != h2)
				return false;
			if (!h1 && !h2)
				return true;

			IList<ParameterDefinition> pdc1 = self.Parameters;
			IList<ParameterDefinition> pdc2 = signature.Parameters;
			int count = pdc1.Count;
			if (count != pdc2.Count)
				return false;

			for (int i = 0; i < count; ++i)
			{
				if (!pdc1[i].ParameterType.IsEqual(pdc2[i].ParameterType))
					return false;
			}
			return true;
		}
	}
}