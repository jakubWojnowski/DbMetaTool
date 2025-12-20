namespace DbMetaTool.Models;

public enum FirebirdFieldType
{
    SmallInt = 7,
    Integer = 8,
    Float = 10,
    Date = 12,
    Time = 13,
    Char = 14,
    BigInt = 16,
    DoublePrecision = 27,
    Timestamp = 35,
    VarChar = 37,
    Blob = 261
}

public enum FirebirdBlobSubType
{
    Binary = 0,
    Text = 1
}

