using Mono.Cecil;

namespace Obfuscator.SkipRules
{
	public interface ISkipField
	{
		bool IsFieldSkip(FieldReference field);
	}
}
