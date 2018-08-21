using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Common;
using Thrift.Protocol;

namespace Codeex.Codecs.Protobuf.Message
{
    public class FrameThriftMessage : DefaultByteBufferHolder,IThriftMessage
    {
        protected const uint VERSION_MASK = 0xffff0000;
        protected const uint VERSION_1 = 0x80010000;

        public FrameThriftMessage(ThriftTransportType transportType, IByteBuffer buffer)
            :base(buffer)
        {
            this.StrictWrite = true;
            this.StrictRead = false;
            this.TransportType = transportType;
        }
        public IByteBuffer Payload { get; set; }

        public ThriftTransportType TransportType
        {
            get; private set;
        }
       
        public FrameThriftMessage CreateMessage()
        {
            return new FrameThriftMessage(this.TransportType, this.Copy().Content);
        }
       
        public bool IsOrderedResponsesRequired()
        {
            return true;
        }

        public long ProcessStartTimeMillis { get; set; }

        public bool StrictWrite { get; set; }
        public bool StrictRead { get; set; }
        public void WriteMessageBegin(TMessage message)
        {
            //保留真正的长度 缓存
            this.Content.SetWriterIndex(0);

            if (this.StrictWrite)
            {
                uint version = VERSION_1 | (uint)(message.Type);
                WriteI32((int)version);
                WriteString(message.Name);
                WriteI32(message.SeqID);
            }
            else
            {
                WriteString(message.Name);
                WriteByte((sbyte)message.Type);
                WriteI32(message.SeqID);
            }
        }

        public void WriteMessageEnd()
        {
        }

        public void WriteStructBegin(TStruct struc)
        {
        }

        public void WriteStructEnd()
        {
        }

        public void WriteFieldBegin(TField field)
        {
            WriteByte((sbyte)field.Type);
            WriteI16(field.ID);
        }

        public void WriteFieldEnd()
        {
        }

        public void WriteFieldStop()
        {
            WriteByte((sbyte)TType.Stop);
        }

        public void WriteMapBegin(TMap map)
        {
            WriteByte((sbyte)map.KeyType);
            WriteByte((sbyte)map.ValueType);
            WriteI32(map.Count);
        }

        public void WriteMapEnd()
        {
        }

        public void WriteListBegin(TList list)
        {
            WriteByte((sbyte)list.ElementType);
            WriteI32(list.Count);
        }

        public void WriteListEnd()
        {
        }

        public void WriteSetBegin(TSet set)
        {
            WriteByte((sbyte)set.ElementType);
            WriteI32(set.Count);
        }

        public void WriteSetEnd()
        {
        }

        public void WriteString(string s)
        {
            WriteBinary(Encoding.UTF8.GetBytes(s));
        }

        public void WriteBool(bool b)
        {
            WriteByte(b ? (sbyte)1 : (sbyte)0);
        }

        private byte[] bout = new byte[1];
        public void WriteByte(sbyte b)
        {
            bout[0] = (byte)b;
            this.Content.WriteBytes(bout, 0, 1);
        }

        private byte[] i16out = new byte[2];
        public  void WriteI16(short s)
        {
            i16out[0] = (byte)(0xff & (s >> 8));
            i16out[1] = (byte)(0xff & s);
            this.Content.WriteBytes(i16out, 0, 2);
        }

        private byte[] i32out = new byte[4];
        public void WriteI32(int i32)
        {
            i32out[0] = (byte)(0xff & (i32 >> 24));
            i32out[1] = (byte)(0xff & (i32 >> 16));
            i32out[2] = (byte)(0xff & (i32 >> 8));
            i32out[3] = (byte)(0xff & i32);
            this.Content.WriteBytes(i32out, 0, 4);
        }

        private byte[] i64out = new byte[8];
        public void WriteI64(long i64)
        {
            i64out[0] = (byte)(0xff & (i64 >> 56));
            i64out[1] = (byte)(0xff & (i64 >> 48));
            i64out[2] = (byte)(0xff & (i64 >> 40));
            i64out[3] = (byte)(0xff & (i64 >> 32));
            i64out[4] = (byte)(0xff & (i64 >> 24));
            i64out[5] = (byte)(0xff & (i64 >> 16));
            i64out[6] = (byte)(0xff & (i64 >> 8));
            i64out[7] = (byte)(0xff & i64);
            this.Content.WriteBytes(i64out, 0, 8);
        }

        public void WriteDouble(double d)
        {
#if !SILVERLIGHT
            WriteI64(BitConverter.DoubleToInt64Bits(d));
#else
            var bytes = BitConverter.GetBytes(d);
            WriteI64(BitConverter.ToInt64(bytes, 0));
#endif
        }

        public void WriteBinary(byte[] b)
        {
            WriteI32(b.Length);
            this.Content.WriteBytes(b, 0, b.Length);
        }


        public TMessage ReadMessageBegin()
        {
            TMessage message = new TMessage();
            int size = ReadI32();
            if (size < 0)
            {
                uint version = (uint)size & VERSION_MASK;
                if (version != VERSION_1)
                {
                    throw new TProtocolException(TProtocolException.BAD_VERSION, "Bad version in ReadMessageBegin: " + version);
                }
                message.Type = (Thrift.Protocol.TMessageType)(size & 0x000000ff);
                message.Name = ReadString();
                message.SeqID = ReadI32();
            }
            else
            {
                if (this.StrictRead)
                {
                    throw new TProtocolException(TProtocolException.BAD_VERSION, "Missing version in readMessageBegin, old client?");
                }
                message.Name = ReadStringBody(size);
                message.Type = (Thrift.Protocol.TMessageType)ReadByte();
                message.SeqID = ReadI32();
            }
            return message;
        }

        public void ReadMessageEnd()
        {
        }

        public TStruct ReadStructBegin()
        {
            return new TStruct();
        }

        public void ReadStructEnd()
        {
        }

        public  TField ReadFieldBegin()
        {
            TField field = new TField();
            field.Type = (TType)ReadByte();

            if (field.Type != TType.Stop)
            {
                field.ID = ReadI16();
            }

            return field;
        }

        public  void ReadFieldEnd()
        {
        }

        public TMap ReadMapBegin()
        {
            TMap map = new TMap();
            map.KeyType = (TType)ReadByte();
            map.ValueType = (TType)ReadByte();
            map.Count = ReadI32();

            return map;
        }

        public void ReadMapEnd()
        {
        }

        public TList ReadListBegin()
        {
            TList list = new TList();
            list.ElementType = (TType)ReadByte();
            list.Count = ReadI32();

            return list;
        }

        public void ReadListEnd()
        {
        }

        public TSet ReadSetBegin()
        {
            TSet set = new TSet();
            set.ElementType = (TType)ReadByte();
            set.Count = ReadI32();

            return set;
        }

        public void ReadSetEnd()
        {
        }

        public bool ReadBool()
        {
            return ReadByte() == 1;
        }

        private byte[] bin = new byte[1];
        public sbyte ReadByte()
        {
            ReadAll(bin, 0, 1);
            return (sbyte)bin[0];
        }

        private byte[] i16in = new byte[2];
        public short ReadI16()
        {
            ReadAll(i16in, 0, 2);
            return (short)(((i16in[0] & 0xff) << 8) | ((i16in[1] & 0xff)));
        }

        private byte[] i32in = new byte[4];
        public int ReadI32()
        {
            ReadAll(i32in, 0, 4);
            return (int)(((i32in[0] & 0xff) << 24) | ((i32in[1] & 0xff) << 16) | ((i32in[2] & 0xff) << 8) | ((i32in[3] & 0xff)));
        }

#pragma warning disable 675

        private byte[] i64in = new byte[8];
        public long ReadI64()
        {
            ReadAll(i64in, 0, 8);
            unchecked
            {
                return (long)(
                    ((long)(i64in[0] & 0xff) << 56) |
                    ((long)(i64in[1] & 0xff) << 48) |
                    ((long)(i64in[2] & 0xff) << 40) |
                    ((long)(i64in[3] & 0xff) << 32) |
                    ((long)(i64in[4] & 0xff) << 24) |
                    ((long)(i64in[5] & 0xff) << 16) |
                    ((long)(i64in[6] & 0xff) << 8) |
                    ((long)(i64in[7] & 0xff)));
            }
        }

#pragma warning restore 675

        public double ReadDouble()
        {
#if !SILVERLIGHT
            return BitConverter.Int64BitsToDouble(ReadI64());
#else
            var value = ReadI64();
            var bytes = BitConverter.GetBytes(value);
            return BitConverter.ToDouble(bytes, 0);
#endif
        }
        public string ReadString()
        {
            var buf = ReadBinary();
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        public byte[] ReadBinary()
        {
            int size = ReadI32();
            byte[] buf = new byte[size];
            this.ReadAll(buf, 0, size);
            return buf;
        }
        private string ReadStringBody(int size)
        {
            byte[] buf = new byte[size];
            this.ReadAll(buf, 0, size);
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }

        private int ReadAll(byte[] buf, int off, int len)
        {
            this.Content.ReadBytes(buf, off, len);
            return this.Content.ReaderIndex;
        }


        public void Skip(TType type)
        {
            try
            {
                switch (type)
                {
                    case TType.Bool:
                        this.ReadBool();
                        break;
                    case TType.Byte:
                        int num1 = (int)this.ReadByte();
                        break;
                    case TType.Double:
                        this.ReadDouble();
                        break;
                    case TType.I16:
                        int num2 = (int)this.ReadI16();
                        break;
                    case TType.I32:
                        this.ReadI32();
                        break;
                    case TType.I64:
                        this.ReadI64();
                        break;
                    case TType.String:
                        this.ReadBinary();
                        break;
                    case TType.Struct:
                        this.ReadStructBegin();
                        while (true)
                        {
                            TField tfield = this.ReadFieldBegin();
                            if (tfield.Type != TType.Stop)
                            {
                                Skip(tfield.Type);
                                this.ReadFieldEnd();
                            }
                            else
                                break;
                        }
                        this.ReadStructEnd();
                        break;
                    case TType.Map:
                        TMap tmap = this.ReadMapBegin();
                        for (int index = 0; index < tmap.Count; ++index)
                        {
                            Skip(tmap.KeyType);
                            Skip(tmap.ValueType);
                        }
                        this.ReadMapEnd();
                        break;
                    case TType.Set:
                        TSet tset = this.ReadSetBegin();
                        for (int index = 0; index < tset.Count; ++index)
                            Skip(tset.ElementType);
                        this.ReadSetEnd();
                        break;
                    case TType.List:
                        TList tlist = this.ReadListBegin();
                        for (int index = 0; index < tlist.Count; ++index)
                            Skip(tlist.ElementType);
                        this.ReadListEnd();
                        break;
                }
            }
            finally
            {
                //prot.DecrementRecursionDepth();
            }
        }

        /// <summary>
        /// 测试类型的长度，如果不完整则返回 -1
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetTypeSize(TType type)
        {
            int nTypeSize = 0;
            var sz = 0;
            var t = TType.Stop;
            var szType = 0;

            switch (type)
            {
                case TType.Bool:
                case TType.Byte:
                    nTypeSize = 1;
                    if (this.Content.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }
                    break;
                case TType.Double:
                case TType.I64:
                    nTypeSize = 8;
                    if (this.Content.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }
                    break;
                case TType.I16:
                    nTypeSize = 2;
                    if (this.Content.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }
                    break;
                case TType.I32:
                    nTypeSize = 4;
                    if (this.Content.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }
                    break;
                case TType.String:
                    nTypeSize = 4;
                    if (this.Content.ReadableBytes < nTypeSize)
                    {
                        return -1; //不完整报文
                    }

                    sz = this.Content.ReadInt();
                    if (this.Content.ReadableBytes < sz)
                    {
                        return -1;
                    }
                    nTypeSize += sz;
                    break;
                case TType.Struct:
                    this.Content.MarkReaderIndex();
                    while (true)
                    {
                        if (this.Content.ReadableBytes < 1)
                        {
                            return -1; //不完整报文
                        }

                        nTypeSize += 1;
                        t = (TType)ReadByte();
                        if (t == TType.Stop)
                        {
                            break;
                        }
                        else
                        {
                            if (this.Content.ReadableBytes < 2)
                            {
                                return -1; //不完整报文
                            }
                            nTypeSize += 2;
                            this.Content.SkipBytes(2);
                            sz = GetTypeSize(t);
                            if (sz < 0 || this.Content.ReadableBytes < sz)
                            {
                                return -1;
                            }

                            nTypeSize += sz;
                            this.Content.SkipBytes(sz);
                        }
                        
                    }
                    
                    this.Content.ResetReaderIndex();
                    break;
                case TType.Map:
                    if (this.Content.ReadableBytes < 6)
                    {
                        return -1; //不完整报文
                    }

                    this.Content.MarkReaderIndex();
                    var kt = this.Content.ReadByte();
                    var kv = this.Content.ReadByte();
                    var size = this.Content.ReadInt();
                    nTypeSize = 6;
                    var szKt = GetTypeSize((TType) kt);
                    var szKv = GetTypeSize((TType) kv);
                    if (szKt < 0 || szKv < 0)
                    {
                        return -1;
                    }

                    nTypeSize += size * (szKt + szKv);
                    if (this.Content.ReadableBytes < size * (szKt + szKv))
                    {
                        return -1;
                    }
                    this.Content.ResetReaderIndex();
                    break;
                case TType.Set:
                case TType.List:
                    if (this.Content.ReadableBytes < 5)
                    {
                        return -1; //不完整报文
                    }
                    this.Content.MarkReaderIndex();
                    nTypeSize += 5;
                    t = (TType) this.Content.ReadByte();
                    sz = this.Content.ReadInt();
                    szType = GetTypeSize(t);
                    if (szType < 0)
                    {
                        return -1;
                    }
                    nTypeSize += sz * szType;
                    if (this.Content.ReadableBytes < sz * szType)
                    {
                        return -1;
                    }
                    this.Content.ResetReaderIndex();
                    break;
            }

            return nTypeSize;

        }

    }
}
