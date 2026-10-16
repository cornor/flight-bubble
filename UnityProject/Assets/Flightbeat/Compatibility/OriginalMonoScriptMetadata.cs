// Generated from the original f54915 WAT and immutable metadata fields.
// This compatibility entry retains the original assembly's metadata. Unity
// generates its own current-project registration; this is not its registrar.
using System;

namespace Flightbeat.Compatibility
{
    internal class OriginalMonoScriptMetadata
    {
        private struct MonoScriptData
        {
            public byte[] FilePathsData, TypesData;
            public int TotalTypes, TotalFiles;
            public bool IsEditorOnly;
        }

        // f1080 is exactly nop; this type has no instance fields.
        public OriginalMonoScriptMetadata() { }

        // f54915: allocate two fresh byte arrays and InitializeArray from the
        // original immutable fields. Packed store +8 is (117 types, 71 files).
        private static MonoScriptData Get()
        {
            return new MonoScriptData {
                FilePathsData = Convert.FromBase64String(FilePaths),
                TypesData = Convert.FromBase64String(Types),
                TotalTypes = 117, TotalFiles = 71, IsEditorOnly = false
            };
        }

        // metadata +0x36EF78; SHA-256 97428dd36ae1e883755ad1b273a0677e88c37566bd17871d2c4db2a711a92e2d
        private const string FilePaths =
            "AAAAAQAAACZcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXEFkU2VydmljZS5jcwAAAAYAAAAjXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxCYXR0bGUuY3MA" +
            "AAACAAAAKVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcQmF0dGxlRmluYWxlLmNzAAAAAQAAACpcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXEJhdHRsZUxv" +
            "YWRvdXQuY3MAAAACAAAALlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcQmF0dGxlU3VwZXJTdHJpa2UuY3MAAAACAAAAKlxBc3NldHNcUGl4ZWxXaW5nXFJ1" +
            "bnRpbWVcQmF0dGxlV2VhcG9ucy5jcwAAAAMAAAArXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxCcm9hZGNhc3RQaWxvdC5jcwAAAAEAAAAqXEFzc2V0c1xQ" +
            "aXhlbFdpbmdcUnVudGltZVxDaGluZXNlR2x5cGhzLmNzAAAAAQAAACZcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXENoaXBBdWRpby5jcwAAAAEAAAAoXEFz" +
            "c2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21iYXRDb21iby5jcwAAAAgAAAArXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21iYXRGZWVkYmFjay5jcwAA" +
            "AAIAAAAsXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21iYXRGbGVldExvb2suY3MAAAABAAAAKlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcQ29tYmF0" +
            "TGVzc29ucy5jcwAAAAEAAAAqXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21iYXRQYWxldHRlLmNzAAAABAAAADNcQXNzZXRzXFBpeGVsV2luZ1xSdW50" +
            "aW1lXENvbWJhdFByZXNzdXJlRmVlZGJhY2suY3MAAAABAAAANlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcQ29tYmF0U3VwZXJTdHJpa2VGZWVkYmFjay5j" +
            "cwAAAAIAAAArXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21iYXRXZWFwb25GeC5jcwAAAAEAAAAqXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxDb21t" +
            "YW5kQ2hyb21lLmNzAAAAAQAAACtcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXENvbW1hbmRlckNvbW1zLmNzAAAAAQAAAClcQXNzZXRzXFBpeGVsV2luZ1xS" +
            "dW50aW1lXERlY2tBc3NlbWJseS5jcwAAAAMAAAAmXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxEZWNrQ29kZXguY3MAAAABAAAAJlxBc3NldHNcUGl4ZWxX" +
            "aW5nXFJ1bnRpbWVcRGVja0RlYnVnLmNzAAAAAQAAACdcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXERlY2tFbmVyZ3kuY3MAAAABAAAAJVxBc3NldHNcUGl4" +
            "ZWxXaW5nXFJ1bnRpbWVcRGVja0hvbWUuY3MAAAABAAAAKFxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRGVja0xpYWlzb24uY3MAAAABAAAAKVxBc3NldHNc" +
            "UGl4ZWxXaW5nXFJ1bnRpbWVcRGVja092ZXJsYXlzLmNzAAAAAQAAAChcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXERlY2tSZWN5Y2xlLmNzAAAAAQAAACVc" +
            "QXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXERlY2tTaG9wLmNzAAAAAQAAACdcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXERlY2tTdXBwbHkuY3MAAAABAAAA" +
            "KFxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRGVja1VwZ3JhZGUuY3MAAAACAAAAKFxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRW5lbXlNb3Rpb24uY3MA" +
            "AAABAAAAJVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRmxlZXRBcnQuY3MAAAACAAAAKVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRmxlZXRDYXRhbG9n" +
            "LmNzAAAAAQAAACtcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXEZsZWV0SGFuZ2FyQXJ0LmNzAAAAAQAAAClcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXEZs" +
            "ZWV0T3BlbmluZy5jcwAAAAgAAAApXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxGbGVldFByb2ZpbGUuY3MAAAACAAAAK1xBc3NldHNcUGl4ZWxXaW5nXFJ1" +
            "bnRpbWVcRmxlZXRTYXZlU3RvcmUuY3MAAAABAAAAKlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRmxpZ2h0TGVzc29ucy5jcwAAAAEAAAAsXEFzc2V0c1xQ" +
            "aXhlbFdpbmdcUnVudGltZVxGbGlnaHROYXJyYXRpb24uY3MAAAABAAAAPVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRnJhbWV3b3JrXEdhbWVGcmFtZXdv" +
            "cmtFeGNlcHRpb24uY3MAAAABAAAALlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRnJhbWV3b3JrXElNZW1vcnkuY3MAAAABAAAAMVxBc3NldHNcUGl4ZWxX" +
            "aW5nXFJ1bnRpbWVcRnJhbWV3b3JrXE1lbW9yeVBvb2wuY3MAAAACAAAAQlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcRnJhbWV3b3JrXE1lbW9yeVBvb2wu" +
            "TWVtb3J5Q29sbGVjdGlvbi5jcwAAAAEAAAA1XEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxGcmFtZXdvcmtcTWVtb3J5UG9vbEluZm8uY3MAAAABAAAALlxB" +
            "c3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcTGVnZW5kYXJ5RmVlZGJhY2suY3MAAAABAAAAMFxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcTmF0aXZlQXVkaW9F" +
            "dmlkZW5jZS5jcwAAAAMAAAAuXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxPbmJvYXJkaW5nQ2FwdHVyZS5jcwAAAAEAAAAoXEFzc2V0c1xQaXhlbFdpbmdc" +
            "UnVudGltZVxQaXhlbEVmZmVjdC5jcwAAAAEAAAAsXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxQaXhlbEVmZmVjdFBvb2wuY3MAAAABAAAAKVxBc3NldHNc" +
            "UGl4ZWxXaW5nXFJ1bnRpbWVcUGl4ZWxTdXJmYWNlLmNzAAAAAgAAAC9cQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXFBpeGVsV2luZ0Jyb2FkY2FzdC5jcwAA" +
            "AAEAAAAtXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxQaXhlbFdpbmdDb2NrcGl0LmNzAAAAAwAAACxcQXNzZXRzXFBpeGVsV2luZ1xSdW50aW1lXFBpeGVs" +
            "V2luZ0ZpbmFsZS5jcwAAAAIAAAAqXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxQaXhlbFdpbmdHYW1lLmNzAAAAAQAAAClcQXNzZXRzXFBpeGVsV2luZ1xS" +
            "dW50aW1lXFBpeGVsV2luZ0hVRC5jcwAAAAEAAAAqXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxQaXhlbFdpbmdNZXRhLmNzAAAAAQAAACxcQXNzZXRzXFBp" +
            "eGVsV2luZ1xSdW50aW1lXFBpeGVsV2luZ1Jlc2N1ZS5jcwAAAAEAAAAsXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxQaXhlbFdpbmdSdW5uZXIuY3MAAAAB" +
            "AAAAK1xBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcUGl4ZWxXaW5nU3BhY2UuY3MAAAACAAAAMVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcUGl4ZWxXaW5n" +
            "U3VwZXJTdHJpa2UuY3MAAAACAAAALVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcUHJlc3N1cmVTZXR0aW5ncy5jcwAAAAEAAAApXEFzc2V0c1xQaXhlbFdp" +
            "bmdcUnVudGltZVxSYW5nZUxlc3NvbnMuY3MAAAABAAAAKlxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcUmV2aWV3Q2FwdHVyZS5jcwAAAAEAAAArXEFzc2V0" +
            "c1xQaXhlbFdpbmdcUnVudGltZVxSZXdhcmRlZEFkR2F0ZS5jcwAAAAEAAAApXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxTY3JlZW5MYXlvdXQuY3MAAAAC" +
            "AAAALVxBc3NldHNcUGl4ZWxXaW5nXFJ1bnRpbWVcU2VlZ0Rpc3RyaWJ1dGlvbi5jcwAAAAEAAAAoXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxUcmFuc2l0" +
            "aW9ucy5jcwAAAAEAAAAoXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxUdW5pbmdQYW5lbC5jcwAAAAMAAAAiXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxV" +
            "aUtpdC5jcwAAAAEAAAAlXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxXYXJab25lcy5jcwAAAAEAAAApXEFzc2V0c1xQaXhlbFdpbmdcUnVudGltZVxXZWNo" +
            "YXRCcmlkZ2UuY3M=";

        // metadata +0x36DE48; SHA-256 0eb3942611e6c8d8ab750093d41187d48b407e8ad3778cf387f15c74375f1cf1
        private const string Types =
            "AAAAABNQaXhlbFdpbmd8QWRTZXJ2aWNlAAAAABRQaXhlbFdpbmd8UXVldWVFbnRyeQAAAAATUGl4ZWxXaW5nfExldmVsRGF0YQAAAAARUGl4ZWxXaW5nfEZp" +
            "Z2h0ZXIAAAAADlBpeGVsV2luZ3xTaG90AQAAABBQaXhlbFdpbmd8QmF0dGxlAAAAABxQaXhlbFdpbmcuQmF0dGxlfFBlbmRpbmdQdXNoAAAAABBQaXhlbFdp" +
            "bmd8RmluYWxlAQAAABBQaXhlbFdpbmd8QmF0dGxlAQAAABBQaXhlbFdpbmd8QmF0dGxlAAAAABVQaXhlbFdpbmd8U3VwZXJTdHJpa2UBAAAAEFBpeGVsV2lu" +
            "Z3xCYXR0bGUAAAAAF1BpeGVsV2luZ3xXZWFwb25Qcm9maWxlAQAAABBQaXhlbFdpbmd8QmF0dGxlAAAAABdQaXhlbFdpbmd8QnJvYWRjYXN0TW9kZQAAAAAb" +
            "UGl4ZWxXaW5nfEJyb2FkY2FzdERlY2lzaW9uAAAAABhQaXhlbFdpbmd8QnJvYWRjYXN0UGlsb3QAAAAAF1BpeGVsV2luZ3xDaGluZXNlR2x5cGhzAAAAABNQ" +
            "aXhlbFdpbmd8Q2hpcEF1ZGlvAQAAABhQaXhlbFdpbmd8Q29tYmF0RmVlZGJhY2sBAAAAGFBpeGVsV2luZ3xDb21iYXRGZWVkYmFjawAAAAAdUGl4ZWxXaW5n" +
            "LkNvbWJhdEZlZWRiYWNrfFBvc2UAAAAAH1BpeGVsV2luZy5Db21iYXRGZWVkYmFja3xGbGlnaHQAAAAAI1BpeGVsV2luZy5Db21iYXRGZWVkYmFja3xUcmFu" +
            "c2l0aW9uAAAAAB9QaXhlbFdpbmcuQ29tYmF0RmVlZGJhY2t8RnVzaW9uAAAAAB5QaXhlbFdpbmcuQ29tYmF0RmVlZGJhY2t8Qmxhc3QAAAAAHVBpeGVsV2lu" +
            "Zy5Db21iYXRGZWVkYmFja3xCZWFtAAAAACRQaXhlbFdpbmcuQ29tYmF0RmVlZGJhY2t8UGllcmNlU3BhcmsBAAAAGFBpeGVsV2luZ3xDb21iYXRGZWVkYmFj" +
            "awAAAAAdUGl4ZWxXaW5nLkNvbWJhdEZlZWRiYWNrfE1hcmsBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdHYW1lAAAAABdQaXhlbFdpbmd8Q29tYmF0UGFsZXR0" +
            "ZQEAAAAYUGl4ZWxXaW5nfENvbWJhdEZlZWRiYWNrAAAAAB1QaXhlbFdpbmcuQ29tYmF0RmVlZGJhY2t8V2F2ZQAAAAAhUGl4ZWxXaW5nLkNvbWJhdEZlZWRi" +
            "YWNrfEthbWlrYXplAAAAACBQaXhlbFdpbmcuQ29tYmF0RmVlZGJhY2t8UHVyZ2VGeAEAAAAYUGl4ZWxXaW5nfENvbWJhdEZlZWRiYWNrAQAAABhQaXhlbFdp" +
            "bmd8Q29tYmF0RmVlZGJhY2sAAAAAIVBpeGVsV2luZy5Db21iYXRGZWVkYmFja3xXZWFwb25GeAEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1Bp" +
            "eGVsV2luZ3xQaXhlbFdpbmdHYW1lAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQAAAAAUUGl4ZWxXaW5nfENvZGV4RW50cnkAAAAAFFBpeGVsV2luZ3xD" +
            "b2RleEluZGV4AQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdH" +
            "YW1lAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdHYW1lAQAA" +
            "ABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdHYW1lAQAAABdQaXhl" +
            "bFdpbmd8UGl4ZWxXaW5nR2FtZQAAAAAVUGl4ZWxXaW5nfEVuZW15TW90aW9uAAAAABpQaXhlbFdpbmcuRW5lbXlNb3Rpb258UG9zZQAAAAASUGl4ZWxXaW5n" +
            "fEZsZWV0QXJ0AAAAABRQaXhlbFdpbmd8TW9kdWxlU3BlYwAAAAAWUGl4ZWxXaW5nfEZsZWV0Q2F0YWxvZwEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUA" +
            "AAAAFlBpeGVsV2luZ3xGbGVldE9wZW5pbmcAAAAAE1BpeGVsV2luZ3xGbGVldENvcmUAAAAAFlBpeGVsV2luZ3xGbGVldFByb2ZpbGUAAAAAFlBpeGVsV2lu" +
            "Z3xGbGVldExvYWRvdXQAAAAAFFBpeGVsV2luZ3xGbGVldEJvbnVzAAAAABNQaXhlbFdpbmd8RmxlZXRMb29rAAAAABJQaXhlbFdpbmd8UGFydFB1bGwAAAAA" +
            "FVBpeGVsV2luZ3xGbGVldFJld2FyZAAAAAAWUGl4ZWxXaW5nfEZsZWV0RWNvbm9teQAAAAAYUGl4ZWxXaW5nfEZsZWV0U2F2ZVN0b3JlAAAAACFQaXhlbFdp" +
            "bmcuRmxlZXRTYXZlU3RvcmV8RW52ZWxvcGUAAAAAFlBpeGVsV2luZ3xGbGlnaHRMZXNzb24AAAAAGVBpeGVsV2luZ3xGbGlnaHROYXJyYXRpb24AAAAAHlRF" +
            "bmdpbmV8R2FtZUZyYW1ld29ya0V4Y2VwdGlvbgAAAAAPVEVuZ2luZXxJTWVtb3J5AQAAABJURW5naW5lfE1lbW9yeVBvb2wBAAAAElRFbmdpbmV8TWVtb3J5" +
            "UG9vbAAAAAAjVEVuZ2luZS5NZW1vcnlQb29sfE1lbW9yeUNvbGxlY3Rpb24AAAAAFlRFbmdpbmV8TWVtb3J5UG9vbEluZm8BAAAAF1BpeGVsV2luZ3xQaXhl" +
            "bFdpbmdHYW1lAAAAAB1QaXhlbFdpbmd8TmF0aXZlQXVkaW9FdmlkZW5jZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUAAAAAG1BpeGVsV2luZ3xPbmJv" +
            "YXJkaW5nQ2FwdHVyZQAAAAAgUGl4ZWxXaW5nLk9uYm9hcmRpbmdDYXB0dXJlfFBhY2UAAAAAFVBpeGVsV2luZ3xQaXhlbEVmZmVjdAAAAAAZUGl4ZWxXaW5n" +
            "fFBpeGVsRWZmZWN0UG9vbAAAAAAWUGl4ZWxXaW5nfFBpeGVsU3VyZmFjZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUAAAAAJ1BpeGVsV2luZy5QaXhl" +
            "bFdpbmdHYW1lfEJyb2FkY2FzdFN0YXR1cwEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdHYW1lAAAAACJQaXhl" +
            "bFdpbmcuUGl4ZWxXaW5nR2FtZXxGaW5hbGVTaGlwAAAAACNQaXhlbFdpbmcuUGl4ZWxXaW5nR2FtZXxGaW5hbGVCbGFzdAEAAAAXUGl4ZWxXaW5nfFBpeGVs" +
            "V2luZ0dhbWUAAAAAJ1BpeGVsV2luZy5QaXhlbFdpbmdHYW1lfFJ1bnRpbWVFdmlkZW5jZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVs" +
            "V2luZ3xQaXhlbFdpbmdHYW1lAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQ" +
            "aXhlbFdpbmdHYW1lAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQAAAAAcUGl4ZWxXaW5nfFN1cGVyU3RyaWtlVmlzdWFscwAAAAAaUGl4ZWxXaW5nfFBy" +
            "ZXNzdXJlU2V0dGluZ3MAAAAAFFBpeGVsV2luZ3xUdW5pbmdLbm9iAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQAAAAAXUGl4ZWxXaW5nfFJldmlld0Nh" +
            "cHR1cmUAAAAAGFBpeGVsV2luZ3xSZXdhcmRlZEFkR2F0ZQAAAAAWUGl4ZWxXaW5nfFNjcmVlbkxheW91dAAAAAAeUGl4ZWxXaW5nfFRvdXRpYW9SZWxlYXNl" +
            "Q29uZmlnAAAAABpQaXhlbFdpbmd8U2VlZ0Rpc3RyaWJ1dGlvbgEAAAAXUGl4ZWxXaW5nfFBpeGVsV2luZ0dhbWUBAAAAF1BpeGVsV2luZ3xQaXhlbFdpbmdH" +
            "YW1lAQAAABdQaXhlbFdpbmd8UGl4ZWxXaW5nR2FtZQAAAAAdUGl4ZWxXaW5nLlBpeGVsV2luZ0dhbWV8VWlIaXQAAAAAIVBpeGVsV2luZy5QaXhlbFdpbmdH" +
            "YW1lfEZsb2F0VGV4dAAAAAASUGl4ZWxXaW5nfFdhclpvbmVzAAAAABZQaXhlbFdpbmd8V2VjaGF0QnJpZGdl";
    }
}
