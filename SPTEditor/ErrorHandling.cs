using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTEditor
{
    public class ErrorHandling
    {
        public static void ThrowError(string message)
        {
            Console.WriteLine("Error: " + message);
            Environment.Exit(999);
        }
    }
}
