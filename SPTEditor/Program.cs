using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SPTEditor.IO;

namespace SPTEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() >= 1 && args[0] == "-h")
                Print.PrintHelp();

            if (args.Count() < 2)
                ErrorHandling.ThrowError("Not enough arguments.");
            if (args[0] != "-c" && args[0] != "-e")
                ErrorHandling.ThrowError("Unknown mode " + args[0]);
            if (!File.Exists(args[1]))
                ErrorHandling.ThrowError("File " + args[1] + " doesn't exist.");

            var mode = args[0];
            var file = args[1];

            if (mode == "-e")
            {
                var result = DeserializeSPT(file);
                var outputFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".txt");
                File.WriteAllText(outputFile, result);
            }
            else if (mode == "-c")
            {
                //ErrorHandling.ThrowError("Mode \"-c\" is not yet supprted.");
                var result = SerializeSPT(file);
                var outputFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".spt2");
                File.WriteAllBytes(outputFile, result);
            }
        }

        private static string DeserializeSPT(string file)
        {
            string result = "";

            using (var reader = new SPT.SPTReader(file))
                while (reader.ReadNextPartition())
                {
                    result += $"[{reader.GetCurrentPartitionNumber()}](" + reader.GetCurrentPartitionInfo().flags1 + " " + reader.GetCurrentPartitionInfo().flags2 + ")\n";

                    while (reader.ReadNextCode())
                    {
                        if (reader.GetCurrentCodeName() == String.Empty)
                        {
                            //utf16 char
                            result += (char)reader.GetCurrentCodeInfo().code;
                        }
                        else
                        {
                            //control code
                            result += $"<{reader.GetCurrentCodeName() + ((reader.GetCurrentCodeInfo().arguments.Count > 0) ? ":" : "") + reader.GetCurrentCodeInfo().arguments.Aggregate("", (o, i) => o += " " + i)}>";
                        }
                    }

                    result += "\n\n";
                }

            return result;
        }

        private static byte[] SerializeSPT(string file)
        {
            string result = "";

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterY(ms, true))
            using (var reader = new SPT.TXTReader(file))
            {
                var header = new SPT.Structs.SPTHeader();
                List<byte[]> partitions = new List<byte[]>();
                List<(int, int)> flags = new List<(int, int)>();

                while (reader.ReadNextPartition())
                {
                    flags.Add((reader.GetCurrentPartitionInfo().flags1, reader.GetCurrentPartitionInfo().flags2));

                    var intMs = new MemoryStream();

                    using (var intBw = new BinaryWriterY(intMs, true))
                    {
                        while (reader.ReadNextCode())
                        {
                            intBw.Write((ushort)(reader.GetCurrentCodeInfo().code ^ header.XORKey));
                            if (reader.GetCurrentCodeInfo().arguments.Count > 0)
                                foreach (var arg in reader.GetCurrentCodeInfo().arguments)
                                    intBw.Write((ushort)(arg ^ header.XORKey));
                        }
                    }

                    partitions.Add(intMs.ToArray());
                }

                header.biggestPartitionSize = (short)(partitions.Max(p => p.Length) / 2);
                header.partitionCount = (short)partitions.Count;

                bw.WriteStruct(header);
                var dataOffset = 0xc + partitions.Count * 8;
                for (int i = 0; i < partitions.Count; i++)
                {
                    bw.Write((short)(dataOffset / 2));
                    bw.Write((short)(partitions[i].Length / 2));
                    bw.Write((short)flags[i].Item1);
                    bw.Write((short)flags[i].Item2);

                    dataOffset += partitions[i].Length / 2;
                }
                foreach (var part in partitions)
                    bw.Write(part);
            }

            return ms.ToArray();
        }
    }
}
