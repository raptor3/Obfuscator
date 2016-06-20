using Mono.Cecil;

namespace Obfuscator.SkipRules
{
	public interface ISkipMethod
	{
		bool IsMethodSkip(MethodReference method);
	}
}
