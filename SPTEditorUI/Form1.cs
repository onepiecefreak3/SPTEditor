using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using SPTEditorUI.IO;

namespace SPTEditorUI
{
    public partial class Form1 : Form
    {
        private string _sptFileName = "";

        List<(SPT.Structs.PartitionInfo, string)> partitions = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void opensptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetFileToOpen();
            if (_sptFileName != String.Empty)
            {
                DeserializeSPT();
                LoadPartitionsToList();
            }
        }

        private void GetFileToOpen()
        {
            var fd = new OpenFileDialog();

            if (fd.ShowDialog() == DialogResult.OK)
            {
                if (!File.Exists(fd.FileName))
                {
                    MessageBox.Show($"File {fd.FileName} was not found. Choose another file.", "File not found", MessageBoxButtons.OK);
                    return;
                }
                if (new FileInfo(fd.FileName).Length <= 16)
                {
                    MessageBox.Show($"File is too small. Please choose another file.", "File too small", MessageBoxButtons.OK);
                    return;
                }
                _sptFileName = fd.FileName;
            }
        }

        private void DeserializeSPT()
        {
            partitions = new List<(SPT.Structs.PartitionInfo, string)>();

            using (var reader = new SPT.SPTReader(_sptFileName))
            {
                while (reader.ReadNextPartition())
                {
                    string text = "";

                    while (reader.ReadNextCode())
                    {
                        if (reader.GetCurrentCodeName() == String.Empty)
                        {
                            //utf16 char
                            text += (char)reader.GetCurrentCodeInfo().code;
                        }
                        else
                        {
                            //control code
                            text += $"<{reader.GetCurrentCodeName() + ((reader.GetCurrentCodeInfo().arguments.Count > 0) ? ":" : "") + reader.GetCurrentCodeInfo().arguments.Aggregate("", (o, i) => o += " " + i)}>";
                        }
                    }

                    var _curPart = reader.GetCurrentPartitionInfo();
                    partitions.Add(
                        (new SPT.Structs.PartitionInfo { absOffset = _curPart.absOffset, codeCount = _curPart.codeCount, flags1 = _curPart.flags1, flags2 = _curPart.flags2 },
                        text)
                        );
                }
            }
        }

        private void LoadPartitionsToList()
        {
            listBox1.SelectedIndexChanged -= listBox1_SelectedIndexChanged;

            if (partitions != null || partitions.Count > 0)
                listBox1.Items.Clear();

            for (int i = 0; i < partitions.Count; i++)
                listBox1.Items.Add("Partition " + i);

            listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1.Text = partitions[listBox1.SelectedIndex].Item2;
        }

        private void savesptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFile = GetFileToSave();
            if (saveFile == String.Empty)
                MessageBox.Show("Chosen file can't be used.", "Save file error", MessageBoxButtons.OK);

            SerializeSPT(saveFile);
        }

        private string GetFileToSave()
        {
            var sd = new SaveFileDialog();

            if (sd.ShowDialog() == DialogResult.OK)
            {
                return sd.FileName;
            }
            else
            {
                return String.Empty;
            }
        }

        private void SerializeSPT(string file)
        {
            string content = "";
            for (int i = 0; i < partitions.Count; i++)
            {
                content += $"[{i}]({partitions[i].Item1.flags1} {partitions[i].Item1.flags2})\n";
                content += partitions[i].Item2;
            }

            using (var bw = new BinaryWriterY(File.Create(file)))
            using (var reader = new SPT.TXTReader(content))
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
                        intBw.Write((short)0);
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
                    bw.Write((short)(partitions[i].Length / 2 - 1));
                    bw.Write((short)flags[i].Item1);
                    bw.Write((short)flags[i].Item2);

                    dataOffset += partitions[i].Length;
                }
                foreach (var part in partitions)
                    bw.Write(part);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (_sptFileName != String.Empty)
                partitions[listBox1.SelectedIndex] = (partitions[listBox1.SelectedIndex].Item1, textBox1.Text);
        }
    }
}
