using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ERPLAB.DataAccess.Core
{
    public static class CryptoHelper
    {
        // =====================================================================
        // 💡 演算法與通訊協定常數
        // [型別決策] 配合 ASP.NET Core Identity V3 二進位協定規格，
        // 標記與演算法代碼使用 byte/uint；其餘參數為配合 .NET 底層 API，維持宣告為 int。
        // =====================================================================
        private const byte FormatMarker = 0x01;
        private const uint PrfSha256 = 1; // 0=SHA1, 1=SHA256, 2=SHA512

        private const int SaltSize = 16;  // 128-bit 鹽值長度
        private const int HashSize = 32;  // 256-bit 雜湊輸出長度 (對齊 SHA-256)

        // 迭代次數 (工作因子)：目前 OWASP 標準建議值，刻意消耗 CPU 算力以抵禦 GPU 暴力破解。
        private const int Iterations = 350000;

        /// <summary>
        /// 採用二進位封裝格式，生成 100% 相容於 ASP.NET Core Identity V3 (SHA-256) 的 Base64 密碼字串
        /// </summary>
        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("密碼不能為空字串", nameof(plainPassword));

            // 1. 生成 16 Bytes 隨機鹽值
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            // 2. PBKDF2 運算 (API 強制要求 int 作為參數型別)
            byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
                password: plainPassword,
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: HashSize);

            // 3. 配置二進位陣列空間 (1 + 4 + 4 + 4 + 16 + 32 = 61 Bytes)
            byte[] outputBytes = new byte[13 + salt.Length + subkey.Length];

            // =====================================================================
            // 🚨 [通訊協定邊界] 寫入二進位陣列 (Serialization)
            // 規格強制要求 4 Bytes 無號整數 (uint) 與大端序 (Big-Endian)。
            // 此處採用顯式 (uint) 強制轉型，宣示型別意圖，避免依賴編譯器的隱式轉換。
            // =====================================================================
            outputBytes[0] = FormatMarker;
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(1), PrfSha256);
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(5), (uint)Iterations);
            BinaryPrimitives.WriteUInt32BigEndian(outputBytes.AsSpan(9), (uint)salt.Length);

            // 4. 複製變動長度的鹽值與雜湊值
            Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
            Buffer.BlockCopy(subkey, 0, outputBytes, 13 + salt.Length, subkey.Length);

            // 5. 轉換為 Base64 字串寫入資料庫 (長度固定為 84 字元)
            return Convert.ToBase64String(outputBytes);
        }

        /// <summary>
        /// 解析二進位封裝字串，具備防時序攻擊、防記憶體越界、防 CPU 耗盡之多重防線。
        /// 全程零記憶體配置 (True Zero-Allocation)，徹底消除 GC 壓力。
        /// </summary>
        public static bool VerifyPassword(string plainPassword, string storedHashString)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHashString))
                return false;

            // =====================================================================
            // 💡 [Clean Code 優化] 捨棄 try-catch，改用 TryFromBase64String
            // 使用 stackalloc 於執行緒堆疊 (Stack) 配置 128 Bytes 緩衝區，
            // 徹底達成零配置，不產生任何記憶體堆積 (Heap) 垃圾回收壓力。
            // =====================================================================
            Span<byte> decodedSpan = stackalloc byte[128];
            if (!Convert.TryFromBase64String(storedHashString, decodedSpan, out int decodedLength))
            {
                return false; // 格式不符直接物理攔截，不拋出例外
            }

            // 標頭長度防呆檢查 (必須確保足夠容納 13 Bytes 的通訊協定標頭)
            if (decodedLength < 13) return false;
            if (decodedSpan[0] != FormatMarker) return false;

            // =====================================================================
            // 🚨 [通訊協定邊界] 讀取二進位陣列 (Deserialization)
            // 從大端序位元組精確還原 uint。此為不可信任的外部輸入，後續必須進行邊界審查。
            // =====================================================================
            uint prf = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(1, 4));
            uint iterCount = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(5, 4));
            uint saltLength = BinaryPrimitives.ReadUInt32BigEndian(decodedSpan.Slice(9, 4));

            // 防線：演算法代碼必須為 1 (SHA-256)
            if (prf != PrfSha256) return false;

            // =====================================================================
            // 🚨 [整數溢位防禦] 確保 saltLength 轉型為 int 時絕對不會變成負數
            // 攔截竄改長度引發的阻斷服務 (DoS) 與記憶體越界 (IndexOutOfRange) 攻擊。
            // 確保 saltLength 至少 16，且絕對不能大於「實際剩餘的可用位元組長度」。
            // =====================================================================
            if (saltLength < 16 || saltLength > (uint)(decodedLength - 13))
                return false;

            // 邊界確認後，方可安全轉型為 int，消滅所有溢位可能。
            int safeSaltLength = (int)saltLength;

            // 算出剩餘雜湊長度，因前方數學保證，此處絕對不會算錯或變負數
            int subkeyLength = decodedLength - 13 - safeSaltLength;
            if (subkeyLength < 16) return false;

            // =====================================================================
            // 🚨 [CPU 耗盡攻擊防禦] 設立迭代次數物理天花板
            // 防止惡意竄改 iterCount 為 21 億次，導致執行緒卡死數小時。
            // =====================================================================
            if (iterCount < 10000 || iterCount > 2000000)
                return false;

            // =====================================================================
            // 💡 [終極零配置記憶體切片 (True Zero-Allocation Slicing)]
            // 使用 ReadOnlySpan 直接在原記憶體上標示視窗，完全避免 .ToArray() 的記憶體浪費。
            // =====================================================================
            ReadOnlySpan<byte> expectedSalt = decodedSpan.Slice(13, safeSaltLength);
            ReadOnlySpan<byte> expectedSubkey = decodedSpan.Slice(13 + safeSaltLength, subkeyLength);

            // =====================================================================
            // 💡 [堆疊字串轉換] 繞過 string 預設轉 byte[] 的 Heap 配置
            // 將明碼字串就地轉為位元組，全程存活在堆疊 (Stack) 上。
            // =====================================================================
            int maxPasswordLength = Encoding.UTF8.GetMaxByteCount(plainPassword.Length);
            Span<byte> passwordSpan = stackalloc byte[maxPasswordLength];
            int actualPasswordLength = Encoding.UTF8.GetBytes(plainPassword, passwordSpan);
            ReadOnlySpan<byte> exactPasswordSpan = passwordSpan.Slice(0, actualPasswordLength);

            // 準備一塊堆疊空地，用來承接運算出來的雜湊結果
            Span<byte> actualSubkey = stackalloc byte[subkeyLength];

            // 呼叫全 Span 版本的 Pbkdf2 底層多載 (全程發生 0 個 new 陣列配置)
            Rfc2898DeriveBytes.Pbkdf2(
                password: exactPasswordSpan,
                salt: expectedSalt,
                destination: actualSubkey,
                iterations: (int)iterCount,
                hashAlgorithm: HashAlgorithmName.SHA256);

            // =====================================================================
            // 🛡️ [時序攻擊防禦 (Timing Attack)] 
            // 強制常數時間比對，阻斷駭客透過回應時間差推測密碼內容。
            // =====================================================================
            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
    }
}