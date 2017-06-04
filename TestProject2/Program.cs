using System;
using TestProject;

namespace TestProject2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(new GClass<SomeClass2>().HMMMM<SomeClass>(new SomeClass2(), new SomeClass()));
        }
    }

    public class GClass<TTT> where TTT : SomeClass2
    {
        public string HMMMM<Trrrrrrrr>(TTT s, Trrrrrrrr a) where Trrrrrrrr : SomeClass
        {
            return s.GetSt3() + a.GetSt() + s.GetSt2();
        }
    }
}
