using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeOpsCore.Utilities
{
    public class LipsumGenerator
    {
        internal string[] GenerateParagraphs(int num)
        {
            return Enumerable.Range(0, num)
                .Select(i => "this should be a random paragraph")
                .ToArray();
        }

        internal string[] GenerateSentences(int num)
        {
            return Enumerable.Range(0, num)
                .Select(i => "this should be a random string")
                .ToArray();
        }

        internal string[] GenerateWords(int num)
        {
            return Enumerable.Range(0, num)
                .Select(i => "randomword?")
                .ToArray();
        }
    }
}
