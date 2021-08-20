using System;
using System.Collections.Generic;
using System.Linq;

public partial class RTMPHandshake
{
    public enum enumSchemeType
    {
        Scheme0,
        Scheme1
    }

    private byte[] C1Key = new byte[] { 0x2, 0xC6, 0xDF, 0x30, 0x2A, 0xEA, 0x83, 0x56, 0x11, 0x5B, 0x6E, 0xD7, 0xAD, 0xDE, 0xD9, 0xBB, 0xF8, 0x6, 0xE6, 0xA7, 0x15, 0xB6, 0x75, 0xD0, 0x5B, 0x83, 0x5C, 0x2D, 0xD, 0x65, 0x86, 0x73, 0x25, 0x8B, 0x32, 0xD6, 0xA5, 0x72, 0x60, 0x3D, 0xD0, 0xA0, 0xD6, 0xC5, 0xB5, 0x73, 0x62, 0xA9, 0x0, 0xE5, 0xD8, 0x54, 0x38, 0xD7, 0x9C, 0x10, 0x65, 0xFC, 0x9F, 0x7F, 0x31, 0xD3, 0xBF, 0x23, 0xCD, 0x9D, 0xFC, 0xA9, 0xC6, 0x58, 0x99, 0x42, 0xA0, 0x78, 0x0, 0x4D, 0x82, 0x35, 0xFA, 0x8F, 0x1A, 0x19, 0x20, 0x68, 0x7F, 0xE5, 0x50, 0x6C, 0x10, 0xE4, 0x30, 0x2F, 0xE0, 0x24, 0x2C, 0x93, 0x8C, 0xA0, 0x0, 0xE5, 0x77, 0x3, 0xAE, 0x30, 0x4A, 0xAE, 0x17, 0xFC, 0x8A, 0x47, 0xEC, 0xA, 0x8B, 0xF1, 0xE9, 0xB8, 0x16, 0xE1, 0x93, 0x2, 0x99, 0x49, 0xE3, 0x84, 0x16, 0x57, 0x83, 0x8C };
    private byte[] S1Key = new byte[] { 0x72, 0xC5, 0x33, 0x6B, 0xA5, 0x92, 0x87, 0x92, 0x92, 0x15, 0x42, 0x28, 0xEF, 0x46, 0x62, 0x3B, 0x9F, 0xC4, 0x59, 0x1E, 0x4C, 0x46, 0xC6, 0xB, 0x5A, 0xA0, 0xF4, 0x73, 0x75, 0x96, 0xAE, 0xE8, 0x5B, 0xE2, 0x53, 0x0, 0x74, 0xDB, 0x92, 0x6, 0xF0, 0xD5, 0x2F, 0xDF, 0x1B, 0x91, 0x1A, 0xBA, 0x55, 0x73, 0xD8, 0xA1, 0xBA, 0x9E, 0xAC, 0x14, 0x3F, 0xA1, 0x87, 0xB4, 0x37, 0x36, 0x9C, 0x92, 0x18, 0xF0, 0x92, 0x8C, 0xCB, 0x24, 0x92, 0xBB, 0xF9, 0xC1, 0x9B, 0x14, 0x52, 0xB5, 0xCE, 0xA8, 0x29, 0xA7, 0x49, 0xE3, 0x45, 0xF6, 0xF7, 0x84, 0x97, 0x7F, 0x39, 0xCE, 0xB5, 0xD5, 0x60, 0xCD, 0xC5, 0xF2, 0x59, 0x90, 0x16, 0xEB, 0x4C, 0x10, 0xAD, 0xE7, 0x24, 0xFF, 0x9C, 0xF3, 0xA7, 0xC5, 0x9A, 0xF1, 0xA8, 0xDF, 0xE7, 0xA0, 0x64, 0x7E, 0x1F, 0x9D, 0x4C, 0xD4, 0x72, 0xAC, 0xA1, 0x38 };
    private byte[] GENUINE_FMS_KEY = new byte[] { 0x47, 0x65, 0x6E, 0x75, 0x69, 0x6E, 0x65, 0x20, 0x41, 0x64, 0x6F, 0x62, 0x65, 0x20, 0x46, 0x6C, 0x61, 0x73, 0x68, 0x20, 0x4D, 0x65, 0x64, 0x69, 0x61, 0x20, 0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x20, 0x30, 0x30, 0x31, 0xF0, 0xEE, 0xC2, 0x4A, 0x80, 0x68, 0xBE, 0xE8, 0x2E, 0x00, 0xD0, 0xD1, 0x02, 0x9E, 0x7E, 0x57, 0x6E, 0xEC, 0x5D, 0x2D, 0x29, 0x80, 0x6F, 0xAB, 0x93, 0xB8, 0xE6, 0x36, 0xCF, 0xEB, 0x31, 0xAE }; // // Genuine Adobe Flash Media Server 001
    private byte[] GENUINE_FP_KEY = new byte[] { 0x47, 0x65, 0x6E, 0x75, 0x69, 0x6E, 0x65, 0x20, 0x41, 0x64, 0x6F, 0x62, 0x65, 0x20, 0x46, 0x6C, 0x61, 0x73, 0x68, 0x20, 0x50, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x20, 0x30, 0x30, 0x31, 0xF0, 0xEE, 0xC2, 0x4A, 0x80, 0x68, 0xBE, 0xE8, 0x2E, 0x00, 0xD0, 0xD1, 0x02, 0x9E, 0x7E, 0x57, 0x6E, 0xEC, 0x5D, 0x2D, 0x29, 0x80, 0x6F, 0xAB, 0x93, 0xB8, 0xE6, 0x36, 0xCF, 0xEB, 0x31, 0xAE };
    private Random R = new Random();
    private byte[] _Version = null;

    public byte[] CreateC1(byte[] C1Base = null)
    {
        // Always use Scheme1
        // 4B Time
        // 4B Version
        // 764B Digest
        // 764B Key
        // 
        byte[] C1S1 = null;
        PackageValidate PV = null;
        byte[] DigestHash = null;

        C1S1 = (byte[])Array.CreateInstance(typeof(byte), 1536);
        if (C1Base == null)
        {
            byte[] KeySource = (byte[])Array.CreateInstance(typeof(byte), 764);
            byte[] DigestSource = (byte[])Array.CreateInstance(typeof(byte), 764);
            R.NextBytes(KeySource);
            R.NextBytes(DigestSource);

            // 重建 Key Source Offset
            KeySource[760] = 1;
            KeySource[761] = 2;
            KeySource[762] = 3;
            KeySource[763] = 4;

            // 重建  Digest Source Offset
            DigestSource[0] = 1;
            DigestSource[1] = 2;
            DigestSource[2] = 3;
            DigestSource[3] = 4;

            // Time
            C1S1[0] = 0;
            C1S1[1] = 0;
            C1S1[2] = 0;
            C1S1[3] = 0;

            // Version
            Array.Copy(_Version, 0, C1S1, 4, 4);
            // C1S1(4) = 9
            // C1S1(5) = 0
            // C1S1(6) = &H7C
            // C1S1(7) = 2

            Array.Copy(DigestSource, 0, C1S1, 8, DigestSource.Length);
            Array.Copy(KeySource, 0, C1S1, 8 + DigestSource.Length, KeySource.Length);
        }
        else
        {
            Array.Copy(C1Base, 0, C1S1, 0, C1S1.Length);
        }

        PV = GetPackageValidate(C1S1, enumSchemeType.Scheme1);
        if (PV != null)
        {
            byte[] FPKey = (byte[])Array.CreateInstance(typeof(byte), 30);

            Array.Copy(C1Key, 0, C1S1, PV.KeyOffset, C1Key.Length);
            Array.Copy(GENUINE_FP_KEY, 0, FPKey, 0, FPKey.Length);

            DigestHash = GetPackageDigest(C1S1, PV, FPKey);
            if (DigestHash != null)
            {
                Array.Copy(DigestHash, 0, C1S1, PV.DigestOffset, DigestHash.Length);
            }
        }

        return C1S1;
    }

    public byte[] CreateS1(byte[] S1Base = null)
    {
        // Always use Scheme1
        // 4B Time
        // 4B Version
        // 764B Digest
        // 764B Key
        // 
        byte[] C1S1 = null;
        PackageValidate PV = null;
        byte[] DigestHash = null;

        C1S1 = (byte[])Array.CreateInstance(typeof(byte), 1536);
        if (S1Base == null)
        {
            byte[] KeySource = (byte[])Array.CreateInstance(typeof(byte), 764);
            byte[] DigestSource = (byte[])Array.CreateInstance(typeof(byte), 764);
            R.NextBytes(KeySource);
            R.NextBytes(DigestSource);

            // 重建 Key Source Offset
            KeySource[760] = 1;
            KeySource[761] = 2;
            KeySource[762] = 3;
            KeySource[763] = 4;

            // 重建  Digest Source Offset
            DigestSource[0] = 1;
            DigestSource[1] = 2;
            DigestSource[2] = 3;
            DigestSource[3] = 4;


            // Time
            C1S1[0] = 0;
            C1S1[1] = 0;
            C1S1[2] = 0;
            C1S1[3] = 0;

            // Version
            Array.Copy(_Version, 0, C1S1, 4, 4);
            // C1S1(4) = 9
            // C1S1(5) = 0
            // C1S1(6) = &H7C
            // C1S1(7) = 2

            Array.Copy(DigestSource, 0, C1S1, 8, DigestSource.Length);
            Array.Copy(KeySource, 0, C1S1, 8 + DigestSource.Length, KeySource.Length);
        }
        else
        {
            Array.Copy(S1Base, 0, C1S1, 0, C1S1.Length);
        }

        PV = GetPackageValidate(C1S1, enumSchemeType.Scheme1);
        if (PV != null)
        {
            byte[] FMSKey = (byte[])Array.CreateInstance(typeof(byte), 36);

            Array.Copy(S1Key, 0, C1S1, PV.KeyOffset, S1Key.Length);
            Array.Copy(GENUINE_FMS_KEY, 0, FMSKey, 0, FMSKey.Length);

            DigestHash = GetPackageDigest(C1S1, PV, FMSKey);
            if (DigestHash != null)
            {
                Array.Copy(DigestHash, 0, C1S1, PV.DigestOffset, DigestHash.Length);
            }
        }

        return C1S1;
    }

    public byte[] CreateC2(PackageValidate S1PV, byte[] C2Base = null)
    {
        byte[] C2 = null;
        byte[] C2HashKey = null;
        byte[] C2Digest = null;
        byte[] FPKey = (byte[])Array.CreateInstance(typeof(byte), 62);
        List<byte> RetValue = new List<byte>();

        Array.Copy(GENUINE_FP_KEY, 0, FPKey, 0, FPKey.Length);
        C2HashKey = HMACsha256(S1PV.Digest, FPKey);
        C2 = (byte[])Array.CreateInstance(typeof(byte), 1536 - 32);
        if (C2Base == null)
            R.NextBytes(C2);
        else
            Array.Copy(C2Base, 0, C2, 0, C2.Length);

        C2Digest = HMACsha256(C2, C2HashKey);
        RetValue.AddRange(C2);
        RetValue.AddRange(C2Digest);

        return RetValue.ToArray();
    }

    public byte[] CreateS2(PackageValidate C1PV, byte[] S2Base = null)
    {
        byte[] S2 = null;
        byte[] S2HashKey = null;
        byte[] S2Digest = null;
        byte[] FMSKey = (byte[])Array.CreateInstance(typeof(byte), 68);
        List<byte> RetValue = new List<byte>();

        if (C1PV != null)
        {
            Array.Copy(GENUINE_FMS_KEY, 0, FMSKey, 0, FMSKey.Length);
            S2HashKey = HMACsha256(C1PV.Digest, FMSKey);
            S2 = (byte[])Array.CreateInstance(typeof(byte), 1536 - 32);

            if (S2Base == null)
                R.NextBytes(S2);
            else
                Array.Copy(S2Base, 0, S2, 0, S2.Length);

            S2Digest = HMACsha256(S2, S2HashKey);
            RetValue.AddRange(S2);
            RetValue.AddRange(S2Digest);
        }
        else
        {
            S2 = (byte[])Array.CreateInstance(typeof(byte), 1536);
            R.NextBytes(S2);
            RetValue.AddRange(S2);
        }

        return RetValue.ToArray();
    }

    public byte[] CreateS1S2(PackageValidate C1PV)
    {
        List<byte> RetValue = new List<byte>();

        RetValue.AddRange(CreateS1());
        RetValue.AddRange(CreateS2(C1PV));

        return RetValue.ToArray();
    }

    public PackageValidate ValidC1(byte[] p)
    {
        PackageValidate PV0 = null;
        PackageValidate PV1 = null;
        PackageValidate RetValue = null;

        PV0 = ValidC1Scheme(p, enumSchemeType.Scheme0);
        if (PV0 != null)
        {
            if (PV0.IsValid)
            {
                RetValue = PV0;
            }
        }

        if (RetValue == null)
        {
            PV1 = ValidC1Scheme(p, enumSchemeType.Scheme1);
            if (PV1 != null)
            {
                if (PV1.IsValid)
                {
                    RetValue = PV1;
                }
            }
        }

        return RetValue;
    }

    public PackageValidate ValidC1Scheme(byte[] p, enumSchemeType Scheme)
    {
        PackageValidate PV = null;
        byte[] Tmp = null;

        // 生成C1的算法如下：
        // calc_c1_digest(c1, schema) {
        // get c1s1-joined from c1 by specified schema 
        // digest-data = HMACsha256(c1s1-joined, FPKey, 30) 
        // return digest-data; }
        // 
        // random fill 1536bytes c1 
        // // also fill the c1-128bytes-key time = time 
        // // c1[0-3] version = [0x80, 0x00, 0x07, 0x02] 
        // // c1[4-7] schema = choose schema0 or schema1
        // digest-data = calc_c1_digest(c1, schema) copy digest-data to c1

        PV = GetPackageValidate(p, Scheme);
        if (PV != null)
        {
            byte[] CalcedDigestData = null;
            byte[] FPKey = (byte[])Array.CreateInstance(typeof(byte), 30);

            Array.Copy(GENUINE_FP_KEY, 0, FPKey, 0, FPKey.Length);
            CalcedDigestData = GetPackageDigest(p, PV, FPKey);
            if (CalcedDigestData != null)
            {
                if (CalcedDigestData.SequenceEqual(PV.Digest))
                {
                    PV.IsValid = true;
                }
            }
        }

        return PV;
    }

    public PackageValidate ValidS1(byte[] p)
    {
        PackageValidate PV0 = null;
        PackageValidate PV1 = null;
        PackageValidate RetValue = null;

        PV0 = ValidS1Scheme(p, enumSchemeType.Scheme0);
        if (PV0 != null)
        {
            if (PV0.IsValid)
            {
                RetValue = PV0;
            }
        }

        if (RetValue == null)
        {
            PV1 = ValidS1Scheme(p, enumSchemeType.Scheme1);
            if (PV1 != null)
            {
                if (PV1.IsValid)
                {
                    RetValue = PV1;
                }
            }
        }

        return RetValue;
    }

    public PackageValidate ValidS1Scheme(byte[] p, enumSchemeType Scheme)
    {
        PackageValidate PV = null;
        byte[] Tmp = null;

        // 生成C1的算法如下：
        // calc_c1_digest(c1, schema) {
        // get c1s1-joined from c1 by specified schema 
        // digest-data = HMACsha256(c1s1-joined, FPKey, 30) 
        // return digest-data; }
        // 
        // random fill 1536bytes c1 
        // // also fill the c1-128bytes-key time = time 
        // // c1[0-3] version = [0x80, 0x00, 0x07, 0x02] 
        // // c1[4-7] schema = choose schema0 or schema1
        // digest-data = calc_c1_digest(c1, schema) copy digest-data to c1

        PV = GetPackageValidate(p, Scheme);
        if (PV != null)
        {
            byte[] CalcedDigestData = null;
            byte[] FMSKey = (byte[])Array.CreateInstance(typeof(byte), 36);

            Array.Copy(GENUINE_FMS_KEY, 0, FMSKey, 0, FMSKey.Length);
            CalcedDigestData = GetPackageDigest(p, PV, FMSKey);
            if (CalcedDigestData != null)
            {
                if (CalcedDigestData.SequenceEqual(PV.Digest))
                {
                    PV.IsValid = true;
                }
            }
        }

        return PV;
    }

    private byte[] GetPackageDigest(byte[] p, PackageValidate PV, byte[] ShaKey)
    {
        byte[] RetValue = null;

        if (PV != null)
        {
            byte[] Tmp = null;

            Tmp = (byte[])Array.CreateInstance(typeof(byte), 1536 - 32);

            Array.Copy(p, 0, Tmp, 0, PV.DigestOffset);
            Array.Copy(p, PV.DigestOffset + 32, Tmp, PV.DigestOffset, 1536 - (PV.DigestOffset + 32));

            RetValue = HMACsha256(Tmp, ShaKey);
        }

        return RetValue;
    }

    private byte[] HMACsha256(byte[] msgBytes, byte[] key)
    {
        var macSHA = new System.Security.Cryptography.HMACSHA256(key);
        byte[] RetValue = null;

        RetValue = macSHA.ComputeHash(msgBytes);

        // macSHA.Dispose()
        macSHA = null;

        return RetValue;
    }

    private PackageValidate GetPackageValidate(byte[] p, enumSchemeType Scheme)
    {
        byte[] KeySource = null;
        byte[] DigestSource = null;
        int KeyOffset = 0;
        int DigestOffset = 0;
        PackageValidate RetValue = null;

        KeySource = (byte[])Array.CreateInstance(typeof(byte), 764);
        DigestSource = (byte[])Array.CreateInstance(typeof(byte), 764);
        switch (Scheme)
        {
            case enumSchemeType.Scheme0:
                // 4B Time
                // 4B Version
                // 764B Key
                // 764B Digest
                Array.Copy(p, 8, KeySource, 0, KeySource.Length);
                Array.Copy(p, 8 + KeySource.Length, DigestSource, 0, DigestSource.Length);

                KeyOffset = getKeyOffset(KeySource) + 8;
                DigestOffset = getDigestOffset(DigestSource) + 8 + 764;

                break;
            case enumSchemeType.Scheme1:
                // 4B Time
                // 4B Version
                // 764B Digest
                // 764B Key
                Array.Copy(p, 8, DigestSource, 0, DigestSource.Length);
                Array.Copy(p, 8 + DigestSource.Length, KeySource, 0, KeySource.Length);

                DigestOffset = getDigestOffset(DigestSource) + 8;
                KeyOffset = getKeyOffset(KeySource) + 8 + 764;

                break;
        }

        if ((DigestOffset != -1) && (KeyOffset != -1))
        {
            RetValue = new PackageValidate();

            RetValue.DigestOffset = DigestOffset;
            RetValue.KeyOffset = KeyOffset;
            RetValue.Digest = (byte[])Array.CreateInstance(typeof(byte), 32);

            Array.Copy(p, DigestOffset, RetValue.Digest, 0, RetValue.Digest.Length);
            RetValue.Key = (byte[])Array.CreateInstance(typeof(byte), 128);

            Array.Copy(p, KeyOffset, RetValue.Key, 0, RetValue.Key.Length);
            RetValue.Scheme = Scheme;
        }

        return RetValue;
    }

    private int getDigestOffset(byte[] DigestSource)
    {
        int offset = DigestSource[0] + DigestSource[1] + DigestSource[2] + DigestSource[3];

        // 764bytes digest结构 offset: 4bytes 
        // random-data: (offset)bytes 
        // digest-data: 32bytes 
        // random-data: (764-4-offset-32)bytes

        // offset = (offset Mod 728) + 8 + 4
        offset = offset % 728 + 4;
        if (offset + 32 > 764)
        {
            return -1;
        }
        else
        {
            return offset;
        }
    }

    private int getKeyOffset(byte[] KeySource)
    {
        int offset = KeySource[760] + KeySource[761] + KeySource[762] + KeySource[763];

        // 764bytes key结构 random-data: (offset)bytes 
        // key-data: 128bytes 
        // random-data: (764-offset-128-4)bytes 
        // offset: 4bytes 

        // offset = (offset Mod 632) + 8

        offset = offset % 632;
        if (offset + 128 + 4 > 764)
        {
            return -1;
        }
        else
        {
            return offset;
        }
    }

    public RTMPHandshake()
    {
        // Version: 9,0,124,2
        _Version = new byte[] { 9, 0, 0x7C, 2 };
    }

    public RTMPHandshake(string Version)
    {
        string[] VersionArray = null;

        VersionArray = Version.Split(",");
        _Version = (byte[])Array.CreateInstance(typeof(byte), VersionArray.Length);

        for (int I = 0; I < VersionArray.Length; I++)
            _Version[I] = System.Convert.ToByte(VersionArray[I]);
    }

    public partial class PackageValidate
    {
        public byte[] Key = null;
        public byte[] Digest = null;
        public int KeyOffset;
        public int DigestOffset;
        public enumSchemeType Scheme;
        public bool IsValid;
    }
}