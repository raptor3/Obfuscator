using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine(new Classss().DoSmth(null));
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
