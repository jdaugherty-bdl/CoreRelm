using MySql.Data.MySqlClient;
using CoreRelm.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Models
{
    internal class DALPropertyType_MySQL
    {
        public string? ColumnName { get; set; }
        public string? PropertyName { get; set; }
        public PropertyInfo? PropertyTypeInformation { get; set; }

        public string? PropertyColumnType { get; private set; }
        public MySqlDbType PropertyMySqlDbType { get; private set; }
        public Type? PropertyType { get; private set; }
        public int DefaultColumnSize { get; private set; }

        public RelmColumn? ResolvableSettings { get; set; }

        // items first in list take precedence when converting from one tuple item to another - this list may look like it has duplicates, but it doesn't
        private static readonly IEnumerable<Tuple<string, MySqlDbType, Type, int>> MySqlTypeConverter =
        [
            new("bigint", MySqlDbType.Int64, typeof(long), 20),
            new("varchar", MySqlDbType.VarChar, typeof(string), 45),
            new("char", MySqlDbType.VarChar, typeof(string), 45),
            new("smallint", MySqlDbType.Int16, typeof(short), -1),
            new("int", MySqlDbType.Int32, typeof(int), -1),
            new("mediumint", MySqlDbType.Int24, typeof(int), -1),
            new("tinyint", MySqlDbType.Int16, typeof(short), 1),
            new("tinyint", MySqlDbType.Int16, typeof(bool), 1),
            new("tinyint", MySqlDbType.Int16, typeof(byte), 1),
            new("bit", MySqlDbType.Bit, typeof(byte), -1),
            new("timestamp", MySqlDbType.Timestamp, typeof(DateTime), -1),
            new("datetime", MySqlDbType.DateTime, typeof(DateTime), -1),
            new("blob", MySqlDbType.Blob, typeof(string), -1),
            new("binary", MySqlDbType.Binary, typeof(byte[]), -1),
            new("varbinary", MySqlDbType.VarBinary, typeof(byte[]), -1),
            new("tinyblob", MySqlDbType.TinyBlob, typeof(string), -1),
            new("mediumblob", MySqlDbType.MediumBlob, typeof(string), -1),
            new("longblob", MySqlDbType.LongBlob, typeof(string), -1),
            new("enum", MySqlDbType.Enum, typeof(string), -1),
            new("set", MySqlDbType.Set, typeof(string), -1),
            new("decimal", MySqlDbType.Decimal, typeof(decimal), -1),
            new("double", MySqlDbType.Double, typeof(double), -1),
            new("float", MySqlDbType.Float, typeof(float), -1),
            new("guid", MySqlDbType.Guid, typeof(Guid), -1),
            new("text", MySqlDbType.Text, typeof(string), -1),
            new("longtext", MySqlDbType.LongText, typeof(string), -1),
            new("time", MySqlDbType.Time, typeof(DateTime), -1),
            new("date", MySqlDbType.Date, typeof(DateTime), -1),
            new("varchar", MySqlDbType.VarChar, typeof(object), 45),
            new("json", MySqlDbType.JSON, typeof(object), -1),
            new("varchar", MySqlDbType.VarChar, typeof(TimeSpan), 45)
        ];

        public static implicit operator string?(DALPropertyType_MySQL Source) => MySqlTypeConverter.Where(x => x.Item1 == Source.PropertyColumnType).FirstOrDefault()?.Item1;
        public static implicit operator MySqlDbType?(DALPropertyType_MySQL Source) => MySqlTypeConverter.Where(x => x.Item2 == Source.PropertyMySqlDbType).FirstOrDefault()?.Item2;
        public static implicit operator Type?(DALPropertyType_MySQL Source) => MySqlTypeConverter.Where(x => x.Item3 == UnboxNullableType(Source.PropertyType)).FirstOrDefault()?.Item3;

        public static explicit operator DALPropertyType_MySQL(string Source) => new(Source);
        public static explicit operator DALPropertyType_MySQL(MySqlDbType Source) => new(Source);
        public static explicit operator DALPropertyType_MySQL(Type Source) => new(Source);

        public override string ToString() => $"[{PropertyColumnType} | {PropertyMySqlDbType} | {PropertyType}]";

        public override bool Equals(object? SourcePropertyColumnName)
        {
            if (SourcePropertyColumnName?.GetType() == typeof(string))
                return PropertyColumnType == (string)SourcePropertyColumnName;

            if (SourcePropertyColumnName?.GetType() == typeof(MySqlDbType))
                return PropertyMySqlDbType == (MySqlDbType)SourcePropertyColumnName;

            if (SourcePropertyColumnName?.GetType() == typeof(Type))
                return PropertyType == (Type)SourcePropertyColumnName;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public DALPropertyType_MySQL(string SourcePropertyColumnName)
        {
            PropertyColumnType = SourcePropertyColumnName;
            PropertyMySqlDbType = ColumnNameToColumnType(PropertyColumnType);
            PropertyType = ColumnNameToPropertyType(PropertyColumnType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnName);
        }

        public DALPropertyType_MySQL(MySqlDbType SourcePropertyColumnType)
        {
            PropertyMySqlDbType = SourcePropertyColumnType;
            PropertyColumnType = ColumnTypeToColumnName(PropertyMySqlDbType);
            PropertyType = ColumnTypeToPropertyType(PropertyMySqlDbType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyColumnType);
        }

        public DALPropertyType_MySQL(Type SourcePropertyType)
        {
            PropertyType = SourcePropertyType;

            if (SourcePropertyType.GetInterface(typeof(ICollection<>).Name) != null)
                PropertyType = typeof(string);

            PropertyColumnType = PropertyTypeToColumnName(PropertyType);
            PropertyMySqlDbType = PropertyTypeToColumnType(PropertyType);

            DefaultColumnSize = GetDefaultColumnSize(SourcePropertyType);
        }

        private static Type UnboxNullableType(Type SourceType) => Nullable.GetUnderlyingType(SourceType) ?? SourceType;

        public static MySqlDbType ColumnNameToColumnType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item2 ?? default;
        public static MySqlDbType PropertyTypeToColumnType(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == UnboxNullableType(PropertyType)).FirstOrDefault()?.Item2 ?? default;
        public static string? ColumnTypeToColumnName(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item1;
        public static string? PropertyTypeToColumnName(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == UnboxNullableType(PropertyType)).FirstOrDefault()?.Item1;
        public static Type? ColumnNameToPropertyType(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item3;
        public static Type? ColumnTypeToPropertyType(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item3;

        public static int GetDefaultColumnSize(string PropertyColumnName) => MySqlTypeConverter.Where(x => x.Item1 == PropertyColumnName).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(MySqlDbType PropertyColumnType) => MySqlTypeConverter.Where(x => x.Item2 == PropertyColumnType).FirstOrDefault()?.Item4 ?? default;
        public static int GetDefaultColumnSize(Type PropertyType) => MySqlTypeConverter.Where(x => x.Item3 == UnboxNullableType(PropertyType)).FirstOrDefault()?.Item4 ?? default;
    }
}
