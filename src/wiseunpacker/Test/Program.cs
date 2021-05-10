using System;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var unpacker = new WiseUnpacker.WiseUnpacker();

            Console.WriteLine("Extracting Wise installer");

            string input = Path.GetFullPath(args[0]);
            string outdir = Path.Combine(Path.GetDirectoryName(input), Path.GetFileNameWithoutExtension(input));
            if (args.Length > 1)
                outdir = Path.GetFullPath(args[1]);

            if (unpacker.ExtractTo(input, outdir))
                Console.WriteLine($"Extracted {input} to {outdir}");
            else
                Console.WriteLine($"Failed to extract {input}!");

            Console.WriteLine("Complete");
        }
    }
}
