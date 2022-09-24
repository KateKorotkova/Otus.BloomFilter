using System;
using BloomFilter.Logic;

namespace BloomFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            int capacity = 2000000;
            var filter = new Filter<string>(capacity);
            filter.Add("content");

            Console.WriteLine(filter.Contains("content"));
            Console.WriteLine(filter.Contains("content2"));

            Console.ReadKey();
        }
    }
}
