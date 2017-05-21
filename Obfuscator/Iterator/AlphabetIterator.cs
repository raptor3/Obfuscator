namespace Obfuscator.Iterator
{
	public class AlphabetIterator : INameIterator
	{
		private static char defaultValue = 'a';

		private char current = defaultValue;

		public void Reset()
		{
			current = defaultValue;
		}

		public string Next()
		{
			string result = current.ToString();
			current++;
			return result;
		}
	}
}
