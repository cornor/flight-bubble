namespace Flightbeat.Model
{
    internal static partial class OriginalMath
    {
        // Original data address 3430608; SHA-256 dbda0719fe79db3b7da6fda5039f1fc7c5869fb1357374a7a58171f966db1cb4.
        private static readonly ulong[] AtanHi =
        {
            0x3FDDAC670561BB4FUL, 0x3FE921FB54442D18UL, 0x3FEF730BD281F69BUL, 0x3FF921FB54442D18UL,
        };
        // Original data address 3430640; SHA-256 29543b11e11e985c536893911f364e42dda82df41e3c0b62b583bed0955092a6.
        private static readonly ulong[] AtanLo =
        {
            0x3C7A2B7F222F65E2UL, 0x3C81A62633145C07UL, 0x3C7007887AF0CBBDUL, 0x3C91A62633145C07UL,
        };
        // Original data address 3430672; SHA-256 42833fc692f9a844be239295e25738ac1f57215cb82f00f0dc0cc7e4f6c5c359.
        private static readonly ulong[] Atan2BothInfinite =
        {
            0x3FE921FB54442D18UL, 0xBFE921FB54442D18UL, 0x4002D97C7F3321D2UL, 0xC002D97C7F3321D2UL,
        };
        // Original data address 3430704; SHA-256 5cc1ba76f0edef34ad4d4b6b8b232b0144b7cd4de61b030c1443570a4c4ee0fd.
        private static readonly ulong[] Atan2XInfinite =
        {
            0x0000000000000000UL, 0x8000000000000000UL, 0x400921FB54442D18UL, 0xC00921FB54442D18UL,
        };
        // Original data address 3441632; SHA-256 f2489ff53c4092ce0723e4d7be5cb9276098dfa45297d3db6c434efdd86c3f57.
        private static readonly ulong[] ExpTable =
        {
            0x3FF0000000000000UL, 0x3FEFD9B0D3158574UL, 0x3FEFB5586CF9890FUL, 0x3FEF9301D0125B51UL,
            0x3FEF72B83C7D517BUL, 0x3FEF54873168B9AAUL, 0x3FEF387A6E756238UL, 0x3FEF1E9DF51FDEE1UL,
            0x3FEF06FE0A31B715UL, 0x3FEEF1A7373AA9CBUL, 0x3FEEDEA64C123422UL, 0x3FEECE086061892DUL,
            0x3FEEBFDAD5362A27UL, 0x3FEEB42B569D4F82UL, 0x3FEEAB07DD485429UL, 0x3FEEA47EB03A5585UL,
            0x3FEEA09E667F3BCDUL, 0x3FEE9F75E8EC5F74UL, 0x3FEEA11473EB0187UL, 0x3FEEA589994CCE13UL,
            0x3FEEACE5422AA0DBUL, 0x3FEEB737B0CDC5E5UL, 0x3FEEC49182A3F090UL, 0x3FEED503B23E255DUL,
            0x3FEEE89F995AD3ADUL, 0x3FEEFF76F2FB5E47UL, 0x3FEF199BDD85529CUL, 0x3FEF3720DCEF9069UL,
            0x3FEF5818DCFBA487UL, 0x3FEF7C97337B9B5FUL, 0x3FEFA4AFA2A490DAUL, 0x3FEFD0765B6E4540UL,
        };
        // Original data address 3441920; SHA-256 028cff3f1098873e90db7bd3b34b9c65e4b262c0887d0164f389decbc06fce57.
        private static readonly ulong[] ExpShift =
        {
            0x4338000000000000UL,
        };
        // Original data address 3441928; SHA-256 593506263a63e18da9dad4c5d3c6ae41996e17ac872114b7174c8a4914af5069.
        private static readonly ulong[] ExpInvLn2N =
        {
            0x40471547652B82FEUL,
        };
        // Original data address 3441936; SHA-256 d95df8462492c25d180f28108c3d8068d6480e7d06b881244cb58d77b65d0034.
        private static readonly ulong[] ExpC2 =
        {
            0x3EBC6AF84B912394UL,
        };
        // Original data address 3441944; SHA-256 2f52586af10c7e4978c3d7e521d640b3f719db4a00a7bb872768497bad1383ff.
        private static readonly ulong[] ExpC1 =
        {
            0x3F2EBFCE50FAC4F3UL,
        };
        // Original data address 3441952; SHA-256 608f6a7a064f0f9d60f379fb769de3652af78918c820fa8e931f07ea83b289df.
        private static readonly ulong[] ExpC0 =
        {
            0x3F962E42FF0C52D6UL,
        };
    }
}
