using System;
using System.Security.Cryptography;
using System.Text;
using Autodesk.Inventor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace InventorElectronicSignature
{
    // 签名处理结果
    public class SignatureProcessResult
    {
        public bool Success { get; set; } // 是否成功
        public string ErrorMessage { get; set; } // 错误信息
        public string SignatureData { get; set; } // 签名数据
    }

    // 签名处理器
    public static class SignatureProcessor
    {
        // 处理签名流程
        public static SignatureProcessResult ProcessSignature(DrawingDocument drawingDoc, User user)
        {
            try
            {
                // 1. 计算文档关键内容的哈希值
                string hashValue = CalculateDocumentHash(drawingDoc);

                // 2. 创建签名数据对象
                SignatureData signatureData = new SignatureData
                {
                    SignerName = user.FullName,
                    SignerUsername = user.Username,
                    SignatureTime = DateTime.Now,
                    DocumentHash = hashValue
                };

                // 3. 对签名数据进行加密签名
                string signedData = SignData(signatureData, user);

                // 4. 将签名数据保存到文档
                SaveSignatureToDocument(drawingDoc, signedData, signatureData);

                return new SignatureProcessResult
                {
                    Success = true,
                    SignatureData = signedData
                };
            }
            catch (Exception ex)
            {
                return new SignatureProcessResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // 计算文档哈希值（仅包含关键内容，避免元数据干扰）
        private static string CalculateDocumentHash(DrawingDocument drawingDoc)
        {
            StringBuilder dataToHash = new StringBuilder();

            // 1. 添加标题栏信息
            foreach (Sheet sheet in drawingDoc.Sheets)
            {
                foreach (TitleBlock titleBlock in sheet.TitleBlocks)
                {
                    foreach (TitleBlockField field in titleBlock.Fields)
                    {
                        dataToHash.AppendLine($"{field.Name}:{field.Text}"); // 字段名+内容
                    }
                }
            }

            // 2. 添加文档基本属性
            PropertySet designProps = drawingDoc.PropertySets["Design Tracking Properties"];
            dataToHash.AppendLine($"Title:{drawingDoc.Title}");
            dataToHash.AppendLine($"Number:{designProps["Part Number"].Value}");
            dataToHash.AppendLine($"Revision:{designProps["Revision Number"].Value}");
            dataToHash.AppendLine($"Description:{drawingDoc.Description}");

            // 3. 计算SHA256哈希
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(dataToHash.ToString());
                byte[] hashBytes = sha256.ComputeHash(bytes);
                
                // 转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // 对签名数据进行加密签名
        private static string SignData(SignatureData data, User user)
        {
            // 序列化签名数据
            string dataString = $"{data.SignerName}|{data.SignerUsername}|{data.SignatureTime:o}|{data.DocumentHash}";
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            byte[] signatureBytes = null;

            // 优先使用数字证书签名
            if (user.Certificate != null)
            {
                // 使用BouncyCastle库进行RSA签名
                AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(user.Certificate.PrivateKey.ExportPkcs8PrivateKey());
                ISigner signer = SignerUtilities.GetSigner("SHA256withRSA");
                signer.Init(true, privateKey);
                signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
                signatureBytes = signer.GenerateSignature();
            }
            else
            {
                // 简化签名：使用HMAC（需确保密钥安全）
                using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(data.SignerUsername + "密钥后缀")))
                {
                    signatureBytes = hmac.ComputeHash(dataBytes);
                }
            }

            // 返回完整签名数据（原始数据+签名结果）
            return $"{dataString}|{Convert.ToBase64String(signatureBytes)}";
        }

        // 将签名数据保存到文档
        private static void SaveSignatureToDocument(DrawingDocument drawingDoc, string signedData, SignatureData signatureData)
        {
            // 1. 保存到自定义属性（方便查看）
            PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];
            AddOrUpdateProperty(customProps, "SignatureStatus", "Signed"); // 标记已签名
            AddOrUpdateProperty(customProps, "SignerName", signatureData.SignerName);
            AddOrUpdateProperty(customProps, "SignatureTime", signatureData.SignatureTime.ToString("o"));
            
            // 2. 保存完整签名数据到命名实体（更安全，不易被修改）
            try
            {
                NamedEntities namedEntities = drawingDoc.NamedEntities;
                NamedEntity signatureEntity = namedEntities["ElectronicSignatureData"];
                signatureEntity.Value = signedData; // 更新已有数据
            }
            catch
            {
                drawingDoc.NamedEntities.Add("ElectronicSignatureData", signedData); // 创建新实体
            }

            drawingDoc.Save(); // 保存文档
        }

        // 添加或更新文档属性
        private static void AddOrUpdateProperty(PropertySet propSet, string name, string value)
        {
            try
            {
                Property prop = propSet[name];
                prop.Value = value; // 更新属性
            }
            catch
            {
                propSet.Add(value, name); // 创建新属性
            }
        }
    }

    // 签名数据结构
    public class SignatureData
    {
        public string SignerName { get; set; } // 签名人姓名
        public string SignerUsername { get; set; } // 签名人用户名
        public DateTime SignatureTime { get; set; } // 签名时间
        public string DocumentHash { get; set; } // 文档哈希值
    }
}
