// SPDX-License-Identifier: LGPL-3.0-only

#define BR

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Hemati.DependencyInjection.Implementation.Core;
using Microsoft.Win32.SafeHandles;

namespace Hemati.DependencyInjection.Serialization;

#if BR
using ReaderImpl = BinaryReader;
#else
using ReaderImpl = ValueBinaryReader2;
#endif

public static class PrecomputedDataLoader
{
    public static PrecomputedServiceDescriptionData[] Load(string[] files)
    {
        List<BinaryServiceData> services = new(1000);
        foreach (string file in files)
        {
#if BR
            using FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            ReaderImpl reader = new(fileStream);
#else
            using SafeFileHandle fileHandle = File.OpenHandle(file);
            ReaderImpl reader = new(fileHandle);
#endif

            var count = reader.ReadInt32();
            var registrations = new BinaryServiceData[count];

            for (int i = 0; i < count; i++)
            {
                ReadOne(ref reader, ref registrations[i]);
            }

            services.AddRange(registrations);
        }

        return [];
    }

    private static void ReadOne(ref ReaderImpl reader, ref BinaryServiceData registration)
    {
        registration.ImplementationType = reader.ReadString();
        registration.KeyLikeContract = reader.ReadNullableString();
        registration.ContractType = reader.ReadNullableString();
        registration.CreationPolicy = (HbServiceLifetime)reader.ReadByte();
        registration.CustomAttributeType = reader.ReadNullableString();
        registration.CustomAttributeArgs = ReadManyValuesNullable(ref reader);
        registration.Metadata = ReadValueDictionaryNullable(ref reader);
    }

    private static object ReadPrimitive(ref ReaderImpl reader)
    {
        BinaryPrimitiveType type = (BinaryPrimitiveType)reader.ReadByte();
        return type switch
        {
            BinaryPrimitiveType.T_Bool => reader.ReadBoolean(),
            BinaryPrimitiveType.T_Byte => reader.ReadByte(),
            BinaryPrimitiveType.T_Char => reader.ReadChar(),
            BinaryPrimitiveType.T_Decimal => reader.ReadDecimal(),
            BinaryPrimitiveType.T_Double => reader.ReadDouble(),
            BinaryPrimitiveType.T_Short => reader.ReadInt16(),
            BinaryPrimitiveType.T_Int => reader.ReadInt32(),
            BinaryPrimitiveType.T_Long => reader.ReadInt64(),
            BinaryPrimitiveType.T_Sbyte => reader.ReadSByte(),
            BinaryPrimitiveType.T_Float => reader.ReadSingle(),
            BinaryPrimitiveType.T_String => reader.ReadString(),
            BinaryPrimitiveType.T_Ushort => reader.ReadUInt16(),
            BinaryPrimitiveType.T_Uint => reader.ReadUInt32(),
            BinaryPrimitiveType.T_Ulong => reader.ReadUInt64(),
            _ => throw new ArgumentOutOfRangeException($"Unknown type {type}")
        };
    }

    private static void ReadBinaryValueData(ref ReaderImpl reader, ref BinaryValueData data)
    {
        data.DataType = (BinaryValueDataType)reader.ReadByte();
        switch (data.DataType)
        {
            case BinaryValueDataType.TypeOf:
            {
                data.Value = reader.ReadString();
                break;
            }
            case BinaryValueDataType.Primitive:
            {
                data.Value = ReadPrimitive(ref reader);
                break;
            }
            case BinaryValueDataType.Enum:
            {
                string enumType = reader.ReadString();
                object enumValue = ReadPrimitive(ref reader);
                data.Value = (enumType, enumValue);
                break;
            }
            default: throw new IOException($"Unsupported data type {data.DataType}");
        }
    }

    private static Dictionary<string, BinaryValueData>? ReadValueDictionaryNullable(ref ReaderImpl reader)
    {
        bool notNull = reader.ReadBoolean();
        if (!notNull)
        {
            return null;
        }

        int count = reader.ReadInt32();
        Dictionary<string, BinaryValueData> data = new(count);
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            BinaryValueData value = new();
            ReadBinaryValueData(ref reader, ref value);
            data[key] = value;
        }

        return data;
    }

    private static BinaryValueData[]? ReadManyValuesNullable(ref ReaderImpl reader)
    {
        bool isNotNull = reader.ReadBoolean();
        if (!isNotNull)
        {
            return null;
        }

        int valuesCount = reader.ReadInt32();
        BinaryValueData[] values = new BinaryValueData[valuesCount];
        for (int i = 0; i < valuesCount; i++)
        {
            ReadBinaryValueData(ref reader, ref values[i]);
        }

        return values;
    }
}

file static class BinaryReaderExt
{
    public static string? ReadNullableString(this BinaryReader reader)
    {
        bool isNotNull = reader.ReadBoolean();
        if (!isNotNull)
        {
            return null;
        }

        return reader.ReadString();
    }
}

class ValueBinaryReader2
{
    private readonly SafeFileHandle _fileHandle;
    private long _offset;
    private byte[]? _stringBuffer;
    private int _currentReadCount;

    public ValueBinaryReader2(SafeFileHandle fileHandle)
    {
        _fileHandle = fileHandle;
    }

    public string? ReadNullableString()
    {
        bool isNotNull = ReadBoolean();
        if (!isNotNull)
        {
            return null;
        }

        return ReadString();
    }

    public string ReadString()
    {
        // Length of the string in bytes, not chars
        int stringLength = Read7BitEncodedInt();
        if (stringLength < 0)
        {
            throw new IOException($"Corrupted string size {stringLength}");
        }

        if (stringLength == 0)
        {
            return string.Empty;
        }

        if (_stringBuffer is null || _stringBuffer.Length < stringLength)
        {
            _stringBuffer = new byte[Math.Max(128, stringLength)];
        }

        Span<byte> stringBytes = _stringBuffer.AsSpan(0, stringLength);
        ReadExactly(stringBytes);
        return Encoding.UTF8.GetString(stringBytes);
    }

    // From dotnet sources
    public int Read7BitEncodedInt()
    {
        // Unlike writing, we can't delegate to the 64-bit read on
        // 64-bit platforms. The reason for this is that we want to
        // stop consuming bytes if we encounter an integer overflow.

        uint result = 0;
        byte byteReadJustNow;

        // Read the integer 7 bits at a time. The high bit
        // of the byte when on means to continue reading more bytes.
        //
        // There are two failure cases: we've read more than 5 bytes,
        // or the fifth byte is about to cause integer overflow.
        // This means that we can read the first 4 bytes without
        // worrying about integer overflow.

        const int MaxBytesWithoutOverflow = 4;
        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            // ReadByte handles end of stream cases for us.
            byteReadJustNow = ReadByte();
            result |= (byteReadJustNow & 0x7Fu) << shift;

            if (byteReadJustNow <= 0x7Fu)
            {
                return (int)result; // early exit
            }
        }

        // Read the 5th byte. Since we already read 28 bits,
        // the value of this byte must fit within 4 bits (32 - 28),
        // and it must not have the high bit set.

        byteReadJustNow = ReadByte();
        if (byteReadJustNow > 0b_1111u)
        {
            throw new FormatException("Bad 7bit integer encountered");
        }

        result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    public bool ReadBoolean()
    {
        return ReadByte() != 0;
    }

    public char ReadChar()
    {
        var decoder = Encoding.UTF8.GetDecoder();

        char res = '\0';
        Span<char> resSpan = new(ref res);

        int charsWritten;
        do
        {
            byte readByte = ReadByte();
            charsWritten = decoder.GetChars(new ReadOnlySpan<byte>(ref readByte), resSpan, false);
        } while (charsWritten == 0);

        return res;
    }

    public byte ReadByte()
    {
        byte res = 0;
        ReadExactly(new Span<byte>(ref res));
        return res;
    }

    public int ReadInt32()
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    private void ReadExactly(Span<byte> buffer)
    {
        _currentReadCount = 0;
        while (_currentReadCount < buffer.Length)
        {
            int thisRead = RandomAccess.Read(_fileHandle, buffer[_currentReadCount..], _offset);
            if (thisRead == 0)
            {
                throw new EndOfStreamException();
            }

            _currentReadCount += thisRead;
            _offset += thisRead;
        }
    }

    public decimal ReadDecimal()
    {
        throw new Exception("for some reason, decoding decimals is gatekeeped");
    }

    public object ReadDouble()
    {
        Span<byte> span = stackalloc byte[sizeof(double)];
        ReadExactly(span);
        return BinaryPrimitives.ReadDoubleLittleEndian(span);
    }

    public short ReadInt16()
    {
        Span<byte> span = stackalloc byte[sizeof(short)];
        ReadExactly(span);
        return BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public long ReadInt64()
    {
        Span<byte> span = stackalloc byte[sizeof(long)];
        ReadExactly(span);
        return BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    public float ReadSingle()
    {
        Span<byte> span = stackalloc byte[sizeof(float)];
        ReadExactly(span);
        return BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    public ushort ReadUInt16()
    {
        Span<byte> span = stackalloc byte[sizeof(ushort)];
        ReadExactly(span);
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public uint ReadUInt32()
    {
        Span<byte> span = stackalloc byte[sizeof(uint)];
        ReadExactly(span);
        return BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public ulong ReadUInt64()
    {
        Span<byte> span = stackalloc byte[sizeof(ulong)];
        ReadExactly(span);
        return BinaryPrimitives.ReadUInt64LittleEndian(span);
    }
}