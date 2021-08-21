using System;

public partial class Common
{
    public enum enumHandshakeType
    {
        C0C1,
        S0S1S2,
        C2
    }

    public enum enumAMF0ObjectType
    {
        Number = 0,
        Boolean = 1,
        String = 2,
        Object = 3,
        Null = 5,
        EMCAArray = 8,
        StrictArray = 10
    }

    public const int VID_SUPPORT_UNUSED = 1;
    public const int VID_SUPPORT_JPEG = 2;
    public const int VID_SUPPORT_SORENSON = 4;
    public const int VID_SUPPORT_HOMEBREW = 8;
    public const int VID_SUPPORT_VP6 = 0x10;
    public const int VID_SUPPORT_VP6ALPHA = 0x20;
    public const int VID_SUPPORT_HOMEBREWV = 0x40;
    public const int VID_SUPPORT_H264 = 0x80;
    public const int VID_SUPPORT_ALL = 0xFF;
    public const int SND_SUPPORT_NONE = 1;
    public const int SND_SUPPORT_ADPCM = 2;
    public const int SND_SUPPORT_MP3 = 4;
    public const int SND_SUPPORT_INTEL = 8;
    public const int SND_SUPPORT_UNUSED = 0x10;
    public const int SND_SUPPORT_NELLY8 = 0x20;
    public const int SND_SUPPORT_NELLY = 0x40;
    public const int SND_SUPPORT_G711A = 0x80;
    public const int SND_SUPPORT_G711U = 0x100;
    public const int SND_SUPPORT_NELLY16 = 0x200;
    public const int SND_SUPPORT_AAC = 0x400;
    public const int SND_SUPPORT_SPEEX = 0x800;
    public const int SND_SUPPORT_ALL = 0xFFF;
    public const int VID_SUPPORT_CLIENT_SEEK = 1;
    public const int AMF0 = 0;
    public const int AMF3 = 3;

    // rtmp need property:
    // app
    // flashver
    // swfurl
    // tcurl
    // fpad
    // audioCodecs
    // videoCodecs
    // videoFunction
    // pageUrl
    // objectEncoding

    public static RTMPLibrary.AMF0Objects.AMF0InheritsBase ParsingAMF0(byte[] Value, int OffsetIndex, ref int ParsingLength)
    {
        enumAMF0ObjectType AMF0Type;
        RTMPLibrary.AMF0Objects.AMF0InheritsBase RetValue = null;

        AMF0Type = (enumAMF0ObjectType)Value[OffsetIndex];
        switch (AMF0Type)
        {
            case enumAMF0ObjectType.String:
                int StrLength = Value[OffsetIndex + 1] * 256 + Value[OffsetIndex + 2];

                RetValue = new RTMPLibrary.AMF0Objects.AMF0String(Value, OffsetIndex);
                ParsingLength = 3 + StrLength;

                break;
            case enumAMF0ObjectType.Number:
                RetValue = new RTMPLibrary.AMF0Objects.AMF0Number(Value, OffsetIndex);
                ParsingLength = 9;

                break;
            case enumAMF0ObjectType.Boolean:
                RetValue = new RTMPLibrary.AMF0Objects.AMF0Boolean(Value, OffsetIndex);
                ParsingLength = 2;

                break;
            case enumAMF0ObjectType.Object:
            case enumAMF0ObjectType.Null:
                int NullLength = 0;

                RetValue = new RTMPLibrary.AMF0Objects.AMF0Object(Value, OffsetIndex, ref NullLength);
                ParsingLength = NullLength;

                break;
            case enumAMF0ObjectType.EMCAArray:
            case enumAMF0ObjectType.StrictArray:
                // find 0 0 9
                // byte: 1, 2, 3, 4: EMCA array length, ignore
                int SubOffsetIndex = 5;

                while (true) {
                    if (Value[OffsetIndex + SubOffsetIndex + 0] == 0 & Value[OffsetIndex + SubOffsetIndex + 1] == 0 & Value[OffsetIndex + SubOffsetIndex + 2] == 9)
                    {
                        ParsingLength = SubOffsetIndex + 3;
                        break;
                    }
                    else
                    {
                        RTMPLibrary.AMF0Objects.AMF0Object.ObjectProperty OP = null;
                        int pLength = 0;

                        OP = RTMPLibrary.AMF0Objects.AMF0Object.ObjectProperty.ParseFromArray(Value, OffsetIndex + SubOffsetIndex, ref pLength);
                        SubOffsetIndex += pLength;
                    }
                }

                break;
            default:
                // unknow
                ParsingLength = 1;
                break;
        }

        return RetValue;
    }

    public static uint GetUInt(byte[] arr, int offset) { 
        return Convert.ToUInt32(arr[offset] * Math.Pow(256, 3) + 
                arr[offset + 1] * Math.Pow(256, 2) + 
                arr[offset + 2] * 256 + 
                arr[offset + 3]);
    }
}