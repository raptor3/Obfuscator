using System;

namespace TestProject
{
    public interface IClass1<T, R> where R : Classss
    {
        string DoSmth(R s);
        string DoSmth(T s);
    }

    public class GClass<TTT> where TTT : SomeClass2
    {
        public string HMMMM<Trrrrrrrr>(TTT s, Trrrrrrrr a) where Trrrrrrrr : SomeClass
        {
            return s.GetSt3() + a.GetSt() + s.GetSt2();
        }
    }

    public class SomeClass
    {
        public string GetSt()
        {
            return "SomeClass";
        }
    }

    public class SomeClass2
    {
        public string GetSt3()
        {
            return "SomeClass2.3";
        }

        public string GetSt2()
        {
            return "SomeClass2.2";
        }
    }

    public class Classss : IClass1<object, Classss>
    {
        public static void Main(params string[] args)
        {
            Console.WriteLine(new Classss().DoSmth(null));
            Console.WriteLine(new GClass<SomeClass2>().HMMMM<SomeClass>(new SomeClass2(), new SomeClass()));
        }

        public string DoSmth(Classss s)
        {
            return "classs";
        }

        public string DoSmth(object s)
        {
            return "object";
        }

        public string HMMMM<Trrrrrrrr>(Trrrrrrrr s)
        {
            return "generic";
        }
    }
}
