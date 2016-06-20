using Mono.Cecil;

namespace Obfuscator.SkipRules
{
	public interface ISkipProperty
	{

		bool IsPropertySkip(PropertyReference prop);
	}
}
