using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
namespace ERPLAB.DataAccess.Core
{
    /// <summary>
    /// 密碼雜湊處理工具類別。
    /// 提供相容於 ASP.NET Core Identity V3 規格之 PBKDF2 密碼雜湊與驗證功能，
    /// 並透過 Span 與 stackalloc 減少記憶體配置 (Zero-Allocation) 以最佳化效能。
    /// </summary>
    public static class CryptoHelper
    {
        // =====================================================================
        // 演算法與通訊協定常數
        // 說明：配合 Identity V3 二進位協定規格，標記與演算法代碼使用 byte/uint；
        // 其餘參數配合 .NET 底層 API 簽章，維持宣告為 int。
        // =====================================================================
        private const byte FormatMarker = 0x01;
        private const uint PrfSha256 = 1; // 0=SHA1, 1=SHA256, 2=SHA512

        private const int SaltSize = 16;  // 128-bit 鹽值長度
        private const int HashSize = 32;  // 256-bit 雜湊輸出長度 (對齊 SHA-256)

        // PBKDF2 迭代次數 (Work Factor)，依循 OWASP 規範設定
        private const int Iterations = 350000;

        /// <summary>
        /// 採用二進位封裝格式，生成相容於 ASP.NET Core Identity V3 的 Base64 密碼雜湊字串。
        /// </summary>
        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("密碼不能為空字串", nameof(plainPassword));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
                password: plainPassword,
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: HashSize);

            // 陣列結構：格式標記(1) + 演算法代碼(4) + 迭代次數(4) + 鹽值長度(4) + 鹽值(16) + 雜湊值(32)
            byte[] outputBytes = new byte[13 + salt.Length + subkey.Length];

            // 依據規格要求，參數需以大端序 (Big-Endian) 寫入。
            // 採用顯式 (uint) 強制轉型以明確型別意圖
            outputBytes[0] = FormatMarker;
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(1), PrfSha256);
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(5), (uint)Iterations);
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(9), (uint)salt.Length);

            // 複製變動長度的鹽值與雜湊值
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + salt.Length, subkey.Length);

            return Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// 解析二進位封裝字串並驗證密碼。
        /// 實作中加入防時序攻擊 (Timing Attack) 與邊界檢查，並以 Span 操作避免額外的記憶體配置。
        /// </summary>
        public static bool VerifyPassword(string plainPassword, string storedHashString)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHashString))
                return false;

            // 使用 stackalloc 於堆疊配置緩衝區，並搭配 TryFromBase64String 避免拋出例外的效能損耗
            Span<byte> decodedSpan = stackalloc byte[128];
            if (!Convert.TryFromBase64String(storedHashString, decodedSpan, out int decodedLength))
            {
                return false;
            }

            // 檢核標頭長度與格式標記
            if (decodedLength < 13) return false;
            if (decodedSpan[0] != FormatMarker) return false;

            // 從二進位陣列還原大端序參數
            uint prf = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(1, 4));
            uint iterCount = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(5, 4));
            uint saltLength = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(9, 4));

            if (prf != PrfSha256) return false;

            // 驗證 saltLength 範圍，避免整數溢位 (Integer Overflow) 與記憶體越界
            if (saltLength < 16 || saltLength > (uint)(decodedLength - 13))
                return false;

            // 確認範圍安全後轉型為 int
            int safeSaltLength = (int)saltLength;
            int subkeyLength = decodedLength - 13 - safeSaltLength;

            if (subkeyLength < 16) return false;

            // 限制迭代次數範圍，避免運算資源遭惡意消耗 (DoS)
            if (iterCount < 10000 || iterCount > 2000000)
                return false;

            // 使用 ReadOnlySpan 映射記憶體區塊，避免額外複製陣列
            ReadOnlySpan<byte> expectedSalt = decodedSpan.Slice(13, safeSaltLength);
            ReadOnlySpan<byte> expectedSubkey = decodedSpan.Slice(13 + safeSaltLength, subkeyLength);

            // 將明碼字串轉換為位元組陣列，操作限制於堆疊內，避免 Heap 記憶體配置
            int maxPasswordLength = Encoding.UTF8.GetMaxByteCount(plainPassword.Length);
            Span<byte> passwordSpan = stackalloc byte[maxPasswordLength];
            int actualPasswordLength = Encoding.UTF8.GetBytes(plainPassword, passwordSpan);
            ReadOnlySpan<byte> exactPasswordSpan = passwordSpan.Slice(0, actualPasswordLength);

            // 準備堆疊空間承接雜湊運算結果
            Span<byte> actualSubkey = stackalloc byte[subkeyLength];

            // 呼叫 Span 版本的 Pbkdf2 API 進行運算
            Rfc2898DeriveBytes.Pbkdf2(
                password: exactPasswordSpan,
                salt: expectedSalt,
                destination: actualSubkey,
                iterations: (int)iterCount,
                hashAlgorithm: HashAlgorithmName.SHA256);

            // 使用 FixedTimeEquals 進行常數時間比對，防範時序攻擊
            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
    }
}