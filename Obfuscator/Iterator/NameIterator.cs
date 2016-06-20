namespace Obfuscator.Iterator
{
	public class NameIterator : INameIterator
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
