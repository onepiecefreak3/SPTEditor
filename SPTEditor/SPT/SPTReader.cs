using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SPTEditor.IO;
using SPTEditor.SPT.Structs;

namespace SPTEditor.SPT
{
    public class SPTReader : IDisposable
    {
        private BinaryReaderY _reader;
        private SPTHeader _header;

        private int _headerSize = 0xc;

        private int _currentPartition = 0;
        private PartitionInfo _partitionInfo;
        private int _readCodes;

        private CodeInfo _codeInfo;

        public SPTReader(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} doesn't exist.");

            _reader = new BinaryReaderY(File.OpenRead(file));

            if (_reader.ReadString(4) != " TPS")
                throw new Exception($"File is not valid SPT.");

            _reader.BaseStream.Position = 0;
            _header = _reader.ReadStruct<SPTHeader>();
        }

        public bool ReadNextPartition()
        {
            if (_currentPartition >= _header.partitionCount)
            {
                _partitionInfo = null;
                return false;
            }

            _reader.BaseStream.Position = _headerSize + _currentPartition * 8;

            _partitionInfo = new PartitionInfo
            {
                absOffset = _reader.ReadInt16() * 2,
                codeCount = _reader.ReadInt16(),
                flags1 = _reader.ReadInt16(),
                flags2 = _reader.ReadInt16()
            };

            _readCodes = 0;
            _reader.BaseStream.Position = _partitionInfo.absOffset;

            _currentPartition++;

            return true;
        }

        public PartitionInfo GetCurrentPartitionInfo()
        {
            return _partitionInfo;
        }

        public int GetCurrentPartitionNumber()
        {
            return _currentPartition;
        }

        public bool ReadNextCode()
        {
            if (_readCodes >= _partitionInfo.codeCount)
            {
                _codeInfo = null;
                return false;
            }

            var code = _reader.ReadUInt16();
            if (Lists.controlCodes.ContainsKey(code))
            {
                //control code
                try
                {
                    _codeInfo = new CodeInfo
                    {
                        code = (ushort)(code ^ _header.XORKey),
                        arguments = _reader.ReadMultiple<ushort>(Lists.controlCodes[code].Item2).Select(m => (ushort)(m ^ _header.XORKey)).ToList()
                    };
                }
                catch
                {
                    return false;
                }

                _readCodes += Lists.controlCodes[code].Item2 + 1;
            }
            else
            {
                //utf16 letter
                _codeInfo = new CodeInfo
                {
                    code = code
                };

                _readCodes++;
            }

            return true;
        }

        public CodeInfo GetCurrentCodeInfo()
        {
            return _codeInfo;
        }

        public string GetCurrentCodeName()
        {
            if (Lists.controlCodes.ContainsKey(_codeInfo.code))
                return Lists.controlCodes[_codeInfo.code].Item1;

            return String.Empty;
        }

        public void Dispose()
        {
            _reader.Close();
        }
    }

    public class TXTReader : IDisposable
    {
        private StringReader _reader;

        private int _currentPartition;
        private PartitionInfo _partInfo;

        private CodeInfo _codeInfo;

        public TXTReader(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} doesn't exist.");

            _reader = new StringReader(File.ReadAllText(file));
        }

        public bool ReadNextPartition()
        {
            var check = _reader.ReadUntil('[');
            while (check != String.Empty && check.Last() == '\\')
            {
                _reader.Read();
                check = _reader.ReadUntil('[');
            }

            if (_reader.Read() == -1)
            {
                _partInfo = null;
                return false;
            }

            _currentPartition = Convert.ToInt32(_reader.ReadUntil(']'));
            _reader.Read();

            _reader.Read();
            var flags = _reader.ReadUntil(')').Split(' ');
            _partInfo = new PartitionInfo
            {
                flags1 = Convert.ToInt16(flags[0]),
                flags2 = Convert.ToInt16(flags[1])
            };
            _reader.Read();

            if (_reader.Peek() != -1 && _reader.Peek() == '\n')
                _reader.Read();

            return true;
        }

        public PartitionInfo GetCurrentPartitionInfo()
        {
            return _partInfo;
        }

        public bool ReadNextCode()
        {
            if (_reader.Peek() == -1)
            {
                _codeInfo = null;
                return false;
            }

            if (Convert.ToChar(_reader.Peek()) == '<')
            {
                //control code
                _reader.Read();
                var codeContent = _reader.ReadUntil('>');
                _reader.Read();

                if (!Lists.controlCodes.Contains(cc => cc.Item1 == codeContent.Split(':')[0]))
                {
                    _codeInfo = null;
                    return false;
                }

                _codeInfo = new CodeInfo
                {
                    code = Lists.controlCodes.Where(cc => cc.Value.Item1 == codeContent.Split(':')[0]).First().Key,
                    arguments = (codeContent.Split(':').Count() > 1) ? codeContent.Split(':')[1].Split(' ').Where(arg => arg != String.Empty).Select(arg => Convert.ToUInt16(arg)).ToList() : new List<ushort>()
                };
            }
            else if (Convert.ToChar(_reader.Peek()) == '\\')
            {
                //escaped char
                _reader.Read();
                _codeInfo = new CodeInfo
                {
                    code = Convert.ToChar(_reader.Read())
                };
            }
            else
            {
                //normal char
                if (Convert.ToChar(_reader.Peek()) == '[')
                {
                    _codeInfo = null;
                    return false;
                }
                _codeInfo = new CodeInfo
                {
                    code = Convert.ToChar(_reader.Read())
                };
            }

            return true;
        }

        public CodeInfo GetCurrentCodeInfo()
        {
            return _codeInfo;
        }

        public void Dispose()
        {
            _reader.Close();
        }
    }
}
