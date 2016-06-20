using Mono.Cecil;

namespace Obfuscator.SkipRules
{
	public interface ISkipType
	{
		bool IsTypeSkip(TypeReference field);
	}
}
