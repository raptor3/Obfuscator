namespace Obfuscator.Iterator
{
    public class AlphabetIteratorFabric : INameIteratorFabric
	{
		public INameIterator GetIterator()
		{
			return new AlphabetIterator();
		}
	}
}
