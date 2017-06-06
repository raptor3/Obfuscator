using System;

namespace TestProject
{
	public interface IClass1<T, R> where R : Classss
	{
		string DoSmth(R s);
		string DoSmth(T s);
	}

	public class Classss : IClass1<object, Classss>
	{
		public static void Main(params string[] args)
		{
			//var s = 123;
			//switch (s)
			//{
			//	case 123:
			//		{
			//			Console.WriteLine("123");
			//			break;
			//		}
			//	case 124:
			//		{
			//			Console.WriteLine("124");
			//			break;
			//		}
			//	case 125:
			//		{
			//			Console.WriteLine("125");
			//			break;
			//		}
			//	case 126:
			//		{
			//			Console.WriteLine("126");
			//			break;
			//		}
			//	case 127:
			//		{
			//			Console.WriteLine("127");
			//			break;
			//		}
			//	case 128:
			//		{
			//			Console.WriteLine("128");
			//			break;
			//		}
			//	default:
			//		{
			//			Console.WriteLine("default");
			//			break;
			//		}
			//}

			switch (int.Parse("54"))
			{
				case 52:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC1" + c);
						break;
					}
				case 53:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC2" + c);
						break;
					}
				case 54:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC3" + c);
						break;
					}
				case 55:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC4" + c);
						break;
					}
				case 56:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC5" + c);
						break;
					}
				default:
					{
						var a = 3;
						var b = 5;
						var c = a + b;
						Console.WriteLine("ZBC1" + c);
						break;
					}
			}
		}

		public string DoSmth(Classss s)
		{
			return "classs";
		}

		public string DoSmth(object s)
		{
			return "object";
		}
	}
}
