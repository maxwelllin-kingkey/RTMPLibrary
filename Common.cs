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

    public static AMF0InheritsBase ParsingAMF0(byte[] Value, int OffsetIndex, ref int ParsingLength)
    {
        enumAMF0ObjectType AMF0Type = default;
        AMF0InheritsBase RetValue = default;
        AMF0Type = (enumAMF0ObjectType)Value[OffsetIndex];
        switch (AMF0Type)
        {
            case enumAMF0ObjectType.String:
                {
                    int Length = Value[OffsetIndex + 1] * 256 + Value[OffsetIndex + 2];
                    RetValue = new AMF0String(Value, OffsetIndex);
                    ParsingLength = 3 + Length;
                    break;
                }

            case enumAMF0ObjectType.Number:
                {
                    RetValue = new AMF0Number(Value, OffsetIndex);
                    ParsingLength = 9;
                    break;
                }

            case enumAMF0ObjectType.Boolean:
                {
                    RetValue = new AMF0Boolean(Value, OffsetIndex);
                    ParsingLength = 2;
                    break;
                }

            case enumAMF0ObjectType.Object:
            case enumAMF0ObjectType.Null:
                {
                    var Length = default(int);
                    RetValue = new AMF0Object(Value, OffsetIndex, Length);
                    ParsingLength = Length;
                    break;
                }

            case enumAMF0ObjectType.EMCAArray:
            case enumAMF0ObjectType.StrictArray:
                {
                    // find 0 0 9
                    // byte: 1, 2, 3, 4: EMCA array length, ignore
                    int SubOffsetIndex = 5;
                    for (int _Loop = 1; _Loop <= 1000; _Loop++)
                    {
                        if (Value[OffsetIndex + SubOffsetIndex + 0] == 0 & Value[OffsetIndex + SubOffsetIndex + 1] == 0 & Value[OffsetIndex + SubOffsetIndex + 2] == 9)
                        {
                            ParsingLength = SubOffsetIndex + 3;
                            break;
                        }
                        else
                        {
                            AMF0Object.ObjectProperty OP = default;
                            var pLength = default(int);
                            OP = AMF0Object.ObjectProperty.ParseFromArray(Value, OffsetIndex + SubOffsetIndex, pLength);
                            SubOffsetIndex += pLength;
                        }

                        // Do
                        // Dim AMF0 As AMF0InheritsBase
                        // Dim pLength As Integer

                        // If Value(OffsetIndex + SubOffsetIndex + 0) = 0 And
                        // Value(OffsetIndex + SubOffsetIndex + 1) = 0 And
                        // Value(OffsetIndex + SubOffsetIndex + 2) = 9 Then
                        // ParsingLength = SubOffsetIndex + 3
                        // Else
                        // AMF0 = ParsingAMF0(Value, OffsetIndex + SubOffsetIndex, pLength)

                        // SubOffsetIndex += pLength
                        // End If
                        // Loop
                        // For I As Integer = OffsetIndex To Value.Length - 3
                        // If Value(I + 0) = 0 And
                        // Value(I + 1) = 0 And
                        // Value(I + 2) = 9 Then
                        // ParsingLength = (I + 3) - OffsetIndex
                        // Exit For
                        // End If
                        // Next
                    }

                    break;
                }

            default:
                {
                    // unknow
                    ParsingLength = 1;
                    break;
                }
        }

        return RetValue;
    }
}