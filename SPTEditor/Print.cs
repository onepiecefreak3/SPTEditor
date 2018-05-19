using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace SPTEditor
{
    public class Print
    {
        public static void PrintHelp()
        {
            Console.WriteLine("SPTReader by onepiecefreak\n" +
                "Internal structure based on own reverse engineering and the SPTEditor used by AAI2 French and English translations.\n\n" +
                "Usage:\n" +
                "\t" + Path.GetFileName(Assembly.GetExecutingAssembly().Location) + " <mode> <filePath>\n\n" +
                "Modes:\n" +
                "\t-e\tExtract a given SPT\n" +
                "\t-c\tCreates a given txt (not yet usable)");
            Environment.Exit(0);
        }
    }
}
