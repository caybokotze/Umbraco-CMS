﻿using System;
using System.IO;
using CSharpTest.Net.Serialization;

namespace Umbraco.Web.PublishedCache.NuCache.DataSource
{
    internal abstract class SerializerBase
    {
        private const char PrefixNull = 'N';
        private const char PrefixString = 'S';
        private const char PrefixInt32 = 'I';
        private const char PrefixUInt16 = 'H';
        private const char PrefixUInt32 = 'J';
        private const char PrefixLong = 'L';
        private const char PrefixFloat = 'F';
        private const char PrefixDouble = 'B';
        private const char PrefixDateTime = 'D';
        private const char PrefixByte = 'O';
        private const char PrefixByteArray = 'A';
        private const char PrefixCompressedStringByteArray = 'C';

        protected string ReadString(Stream stream) => PrimitiveSerializer.String.ReadFrom(stream);
        protected int ReadInt(Stream stream) => PrimitiveSerializer.Int32.ReadFrom(stream);
        protected long ReadLong(Stream stream) => PrimitiveSerializer.Int64.ReadFrom(stream);
        protected float ReadFloat(Stream stream) => PrimitiveSerializer.Float.ReadFrom(stream);
        protected double ReadDouble(Stream stream) => PrimitiveSerializer.Double.ReadFrom(stream);
        protected DateTime ReadDateTime(Stream stream) => PrimitiveSerializer.DateTime.ReadFrom(stream);
        protected byte[] ReadByteArray(Stream stream) => PrimitiveSerializer.Bytes.ReadFrom(stream);

        private T? ReadStruct<T>(Stream stream, char t, Func<Stream, T> read)
            where T : struct
        {
            var type = PrimitiveSerializer.Char.ReadFrom(stream);
            if (type == PrefixNull) return null;
            if (type != t)
                throw new NotSupportedException($"Cannot deserialize type '{type}', expected '{t}'.");
            return read(stream);
        }

        protected string ReadStringObject(Stream stream, bool intern = false) // required 'cos string is not a struct
        {
            var type = PrimitiveSerializer.Char.ReadFrom(stream);
            if (type == PrefixNull) return null;
            if (type != PrefixString)
                throw new NotSupportedException($"Cannot deserialize type '{type}', expected '{PrefixString}'.");
            return intern
                ? string.Intern(PrimitiveSerializer.String.ReadFrom(stream))
                : PrimitiveSerializer.String.ReadFrom(stream);
        }

        protected int? ReadIntObject(Stream stream) => ReadStruct(stream, PrefixInt32, ReadInt);
        protected long? ReadLongObject(Stream stream) => ReadStruct(stream, PrefixLong, ReadLong);
        protected float? ReadFloatObject(Stream stream) => ReadStruct(stream, PrefixFloat, ReadFloat);
        protected double? ReadDoubleObject(Stream stream) => ReadStruct(stream, PrefixDouble, ReadDouble);
        protected DateTime? ReadDateTimeObject(Stream stream) => ReadStruct(stream, PrefixDateTime, ReadDateTime);

        protected object ReadObject(Stream stream)
            => ReadObject(PrimitiveSerializer.Char.ReadFrom(stream), stream);

        protected object ReadObject(char type, Stream stream)
        {
            // NOTE: This method is only called when reading property data, some boxing may occur but all other reads for structs are
            // done with ReadStruct to reduce all boxing.

            switch (type)
            {
                case PrefixNull:
                    return null;
                case PrefixString:
                    return PrimitiveSerializer.String.ReadFrom(stream);
                case PrefixInt32:
                    return PrimitiveSerializer.Int32.ReadFrom(stream);
                case PrefixUInt16:
                    return PrimitiveSerializer.UInt16.ReadFrom(stream);
                case PrefixUInt32:
                    return PrimitiveSerializer.UInt32.ReadFrom(stream);
                case PrefixByte:
                    return PrimitiveSerializer.Byte.ReadFrom(stream);
                case PrefixLong:
                    return PrimitiveSerializer.Int64.ReadFrom(stream);
                case PrefixFloat:
                    return PrimitiveSerializer.Float.ReadFrom(stream);
                case PrefixDouble:
                    return PrimitiveSerializer.Double.ReadFrom(stream);
                case PrefixDateTime:
                    return PrimitiveSerializer.DateTime.ReadFrom(stream);
                case PrefixByteArray:
                    return PrimitiveSerializer.Bytes.ReadFrom(stream);
                case PrefixCompressedStringByteArray:
                    return new LazyCompressedString(PrimitiveSerializer.Bytes.ReadFrom(stream));
                default:
                    throw new NotSupportedException($"Cannot deserialize unknown type '{type}'.");
            }
        }

        protected void WriteObject(object value, Stream stream)
        {
            // NOTE: This method is only currently used to write 'string' information, all other writes are done directly with the PrimitiveSerializer
            // so no boxing occurs. Though potentially we should write everything via this class just like we do for reads.

            if (value == null)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixNull, stream);
            }
            else if (value is string stringValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixString, stream);
                PrimitiveSerializer.String.WriteTo(stringValue, stream);
            }
            else if (value is int intValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixInt32, stream);
                PrimitiveSerializer.Int32.WriteTo(intValue, stream);
            }
            else if (value is byte byteValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixByte, stream);
                PrimitiveSerializer.Byte.WriteTo(byteValue, stream);
            }
            else if (value is ushort ushortValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixUInt16, stream);
                PrimitiveSerializer.UInt16.WriteTo(ushortValue, stream);
            }
            else if (value is long longValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixLong, stream);
                PrimitiveSerializer.Int64.WriteTo(longValue, stream);
            }
            else if (value is float floatValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixFloat, stream);
                PrimitiveSerializer.Float.WriteTo(floatValue, stream);
            }
            else if (value is double doubleValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixDouble, stream);
                PrimitiveSerializer.Double.WriteTo(doubleValue, stream);
            }
            else if (value is DateTime dateValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixDateTime, stream);
                PrimitiveSerializer.DateTime.WriteTo(dateValue, stream);
            }
            else if (value is uint uInt32Value)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixUInt32, stream);
                PrimitiveSerializer.UInt32.WriteTo(uInt32Value, stream);
            }
            else if (value is byte[] byteArrayValue)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixByteArray, stream);
                PrimitiveSerializer.Bytes.WriteTo(byteArrayValue, stream);
            }
            else if (value is LazyCompressedString lazyCompressedString)
            {
                PrimitiveSerializer.Char.WriteTo(PrefixCompressedStringByteArray, stream);
                PrimitiveSerializer.Bytes.WriteTo(lazyCompressedString.GetBytes(), stream);
            }
            else
                throw new NotSupportedException("Value type " + value.GetType().FullName + " cannot be serialized.");
        }
    }
}
