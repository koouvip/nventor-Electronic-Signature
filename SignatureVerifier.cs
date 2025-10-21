using System;
using System.Security.Cryptography;
using System.Text;
using Autodesk.Inventor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace InventorElectronicSignature
{
    // 验证结果
    public class VerificationResult
    {
        public bool IsVerified { get; set; } // 是否验证通过
        public string Reason { get; set; } // 验证失败原因
        public string SignerName { get; set; } // 签名人
        public DateTime SignatureTime { get; set; } // 签名时间
    }

    // 签名验证器
    public static class SignatureVerifier
    {
        // 验证文档签名
        public static VerificationResult Verify(DrawingDocument drawingDoc)
        {
            try
            {
                // 1. 检查文档是否已签名
                if (!SignatureHelper.IsDocumentSigned(drawingDoc))
                {
                    return new VerificationResult
                    {
                        IsVerified = false,
                        Reason = "文档未包含电子签名"
                    };
                }

                // 2. 获取存储的签名数据
                string signedData = GetSignatureFromDocument(drawingDoc);
                if (string.IsNullOrEmpty(signedData))
                {
                    return new VerificationResult
                    {
                        IsVerified = false,
                        Reason = "无法获取签名数据"
                    };
                }

                // 3. 解析签名数据（格式：签名人|用户名|时间|哈希|签名值）
                string[] dataParts = signedData.Split('|');
                if (dataParts.Length < 5)
                {
                    return new VerificationResult
                    {
                        IsVerified = false,
                        Reason = "签名数据格式无效"
                    };
                }

                // 4. 提取签名信息
                SignatureData signatureData = new SignatureData
                {
                    SignerName = dataParts[0],
                    SignerUsername = dataParts[1],
                    SignatureTime = DateTime.Parse(dataParts[2]),
                    DocumentHash = dataParts[3]
                };

                string originalData = $"{dataParts[0]}|{dataParts[1]}|{dataParts[2]}|{dataParts[3]}";
                byte[] signatureBytes = Convert.FromBase64String(dataParts[4]);

                // 5. 验证签名有效性
                bool signatureValid = VerifySignature(originalData, signatureBytes, signatureData.SignerUsername);
                if (!signatureValid)
                {
                    return new VerificationResult
                    {
                        IsVerified = false,
                        Reason = "签名无效或已被篡改",
                        SignerName = signatureData.SignerName,
                        SignatureTime = signatureData.SignatureTime
                    };
                }

                // 6. 验证文档内容未被修改（重新计算哈希并比对）
                string currentHash = SignatureProcessor.CalculateDocumentHash(drawingDoc);
                if (currentHash != signatureData.DocumentHash)
                {
                    return new VerificationResult
                    {
                        IsVerified = false,
                        Reason = "文档内容已被修改，哈希值不匹配",
                        SignerName = signatureData.SignerName,
                        SignatureTime = signatureData.SignatureTime
                    };
                }

                // 所有验证通过
                return new VerificationResult
                {
                    IsVerified = true,
                    Reason = "签名有效",
                    SignerName = signatureData.SignerName,
                    SignatureTime = signatureData.SignatureTime
                };
            }
            catch (Exception ex)
            {
                return new VerificationResult
                {
                    IsVerified = false,
                    Reason = $"验证过程中发生错误: {ex.Message}"
                };
            }
        }

        // 从文档中获取签名数据
        private static string GetSignatureFromDocument(DrawingDocument drawingDoc)
        {
            try
            {
                NamedEntity signatureEntity = drawingDoc.NamedEntities["ElectronicSignatureData"];
                return signatureEntity.Value.ToString();
            }
            catch
            {
                return null;
            }
        }

        // 验证签名有效性
        private static bool VerifySignature(string originalData, byte[] signatureBytes, string username)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(originalData);

            // 简化验证（与签名时的算法对应）
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(username + "密钥后缀")))
            {
                byte[] computedHash = hmac.ComputeHash(dataBytes);
                return CompareByteArrays(signatureBytes, computedHash); // 安全比较字节数组
            }
        }

        // 比较字节数组（防止计时攻击）
        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i]; // 异或运算，若所有位相同则结果为0
            }
            return result == 0;
        }
    }

    // 签名辅助工具
    public static class SignatureHelper
    {
        // 检查文档是否已签名
        public static bool IsDocumentSigned(DrawingDocument drawingDoc)
        {
            try
            {
                PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];
                Property statusProp = customProps["SignatureStatus"];
                return statusProp.Value.ToString().Equals("Signed", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
