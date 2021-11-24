using System;

namespace RTMPLibrary.AMF0Objects
{
    public partial class AMF0EMCAArray : AMF0InheritsBase
    {
        public System.Collections.Generic.List<ObjectProperty> Properties = new System.Collections.Generic.List<ObjectProperty>();
        private string iArrayName;

        public void AddToProperties(string Name, AMF0InheritsBase AMF0)
        {
            ObjectProperty OP = new ObjectProperty();
            OP.Name = Name;
            OP.Value = AMF0;
            Properties.Add(OP);
        }

        public void SetValue(string Name, object Value)
        {
            AMF0InheritsBase AMF0;

            AMF0 = GetValue(Name);

            if (AMF0 != null)
            {
                switch (AMF0.ObjectType)
                {
                    case Common.enumAMF0ObjectType.Boolean:
                        {
                            AMF0Boolean ObjectValue = (AMF0Boolean)AMF0;

                            ObjectValue.Value = System.Convert.ToBoolean(Value);
                        }

                        break;
                    case Common.enumAMF0ObjectType.Number:
                        {
                            AMF0Number ObjectValue = (AMF0Number)AMF0;

                            ObjectValue.Value = System.Convert.ToDouble(Value);
                        }

                        break;
                    case Common.enumAMF0ObjectType.String:
                        {
                            AMF0String ObjectValue = (AMF0String)AMF0;

                            ObjectValue.Value = System.Convert.ToString(Value);
                        }

                        break;
                    default:
                        throw new System.Exception("No support value - " + AMF0.ObjectType.ToString());
                }
            }
            else if (Value != null)
            {
                if (Value.GetType().Name.Trim().ToUpper() == "Boolean".ToUpper())
                {
                    AMF0Boolean ObjectValue = new AMF0Boolean();

                    ObjectValue.Value = System.Convert.ToBoolean(Value);
                    this.AddToProperties(Name, ObjectValue);
                }
                else if (Value.GetType().Name.Trim().ToUpper() == "String".ToUpper())
                {
                    AMF0String ObjectValue = new AMF0String();

                    ObjectValue.Value = System.Convert.ToString(Value);
                    this.AddToProperties(Name, ObjectValue);
                }
                else
                {
                    double DoubleValue;

                    if (System.Double.TryParse(Value.ToString(), out DoubleValue))
                    {
                        AMF0Number ObjectValue = new AMF0Number();

                        ObjectValue.Value = DoubleValue;
                        this.AddToProperties(Name, ObjectValue);
                    }
                    else
                    {
                        throw new System.Exception("No support type - " + Value.GetType().Name);
                    }
                }
            }
        }

        public AMF0InheritsBase GetValue(string Name)
        {
            AMF0InheritsBase RetValue = null;

            foreach (ObjectProperty EachProp in Properties)
            {
                if (EachProp.Name.Trim().ToUpper() == Name.Trim().ToUpper())
                {
                    RetValue = EachProp.Value;
                    break;
                }
            }

            return RetValue;
        }

        public AMF0EMCAArray() : base(Common.enumAMF0ObjectType.EMCAArray)
        {
        }

        public AMF0EMCAArray(byte[] Value, int OffsetIndex, ref int ParsingLength) : base(Value, OffsetIndex)
        {
            if (base.ObjectType == Common.enumAMF0ObjectType.EMCAArray)
            {
                int TotalParsingLength = 0;
                int ArrayLength = 0;

                OffsetIndex += 1;
                ArrayLength = (int)Common.GetUInt(Value, OffsetIndex);

                OffsetIndex += 4;

                while (true)
                {
                    if (Value[OffsetIndex] == 0 & Value[OffsetIndex + 1] == 0 & Value[OffsetIndex + 2] == 9)
                    {
                        break;
                    }
                    else
                    {
                        ObjectProperty OP = null;
                        int OPLength = 0;

                        OP = ObjectProperty.ParseFromArray(Value, OffsetIndex, ref OPLength);

                        Properties.Add(OP);
                        OffsetIndex += OPLength;
                        TotalParsingLength += OPLength;
                    }
                }

                ParsingLength = TotalParsingLength + 8;
            }
            else if (base.ObjectType == Common.enumAMF0ObjectType.Null)
            {
                // object is null
                ParsingLength = 1;
            }
        }

        public override byte[] ToByteArray()
        {
            System.Collections.Generic.List<byte> RetValue = new System.Collections.Generic.List<byte>();

            RetValue.Add((byte)Common.enumAMF0ObjectType.EMCAArray);

            for (int I = 0; I <= 3; I++)
                RetValue.Add((byte)((long)Math.Round(Properties.Count % Math.Pow(256d, 3 - I + 1)) / (long)Math.Round(Math.Pow(256d, 3 - I))));

            if (Properties.Count > 0)
            {
                foreach (ObjectProperty EachOP in Properties)
                    RetValue.AddRange(EachOP.ToByteArray());
            }

            RetValue.AddRange(new byte[] { 0, 0, 9 });

            return RetValue.ToArray();
        }

        public partial class ObjectProperty
        {
            public string Name;
            public AMF0InheritsBase Value = default;

            public static ObjectProperty ParseFromArray(byte[] value, int offsetIndex, ref int ParsingLength)
            {
                int StringLength;
                int pLength = 0;
                ObjectProperty RetValue = new ObjectProperty();

                StringLength = value[offsetIndex] * 256 + value[offsetIndex + 1];

                RetValue.Name = System.Text.Encoding.Default.GetString(value, offsetIndex + 2, StringLength);
                RetValue.Value = Common.ParsingAMF0(value, offsetIndex + StringLength + 2, ref pLength);

                ParsingLength = pLength + StringLength + 2;

                return RetValue;
            }

            public byte[] ToByteArray()
            {
                System.Collections.Generic.List<byte> RetValue = new System.Collections.Generic.List<byte>();
                int NameLength = System.Text.Encoding.Default.GetByteCount(Name);

                RetValue.AddRange(new byte[] { (byte)(NameLength / 256), (byte)(NameLength % 256) });
                RetValue.AddRange(System.Text.Encoding.Default.GetBytes(Name));
                RetValue.AddRange(Value.ToByteArray());

                return RetValue.ToArray();
            }
        }
    }
}
