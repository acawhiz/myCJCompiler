using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCJCompiler
{
    internal class SourceFile
    {
        byte[] b;

        public byte[] getSourceFile(string path)
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
            else
                return null;
        }

        public void ShowSourceFile(string path)
        {
            if (File.Exists(path))
            {
                int i = 0;
                b = File.ReadAllBytes(path);
                while (i < b.Length)
                {
                    Console.Write((char)b[i]);
                    i++;
                }

            }
     
        }

    }
}
