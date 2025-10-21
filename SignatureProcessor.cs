using System;
using System.Security.Cryptography;
using System.Text;
using Autodesk.Inventor;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace InventorElectronicSignature
{
    public class SignatureProcessResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string SignatureData { get; set; }
    }

    public static class SignatureProcessor
    {
        public static SignatureProcessResult ProcessSignature(DrawingDocument drawingDoc, User user)
        {
            try
            {
                // 1. 计算文档内容的哈希值
                string hashValue = CalculateDocumentHash(drawingDoc);

                // 2. 创建签名数据
                SignatureData signatureData = new SignatureData
                {
                    SignerName = user.FullName,
                    SignerUsername = user.Username,
                    SignatureTime = DateTime.Now,
                    DocumentHash = hashValue
                };

                // 3. 对签名数据进行签名
                string signedData = SignData(signatureData, user);

                // 4. 将签名数据保存到文档属性
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

        private static string CalculateDocumentHash(DrawingDocument drawingDoc)
        {
            // 收集需要计算哈希的关键数据
            StringBuilder dataToHash = new StringBuilder();

            // 添加标题栏信息
            foreach (Sheet sheet in drawingDoc.Sheets)
            {
                foreach (TitleBlock titleBlock in sheet.TitleBlocks)
                {
                    foreach (TitleBlockField field in titleBlock.Fields)
                    {
                        dataToHash.AppendLine($"{field.Name}:{field.Text}");
                    }
                }
            }

            // 添加文档基本属性
            PropertySet designProps = drawingDoc.PropertySets["Design Tracking Properties"];
            dataToHash.AppendLine($"Title:{drawingDoc.Title}");
            dataToHash.AppendLine($"Number:{designProps["Part Number"].Value}");
            dataToHash.AppendLine($"Revision:{designProps["Revision Number"].Value}");
            dataToHash.AppendLine($"Description:{drawingDoc.Description}");

            // 计算SHA256哈希
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(dataToHash.ToString());
                byte[] hashBytes = sha256.ComputeHash(bytes);
                
                // 转换为字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static string SignData(SignatureData data, User user)
        {
            // 序列化签名数据
            string dataString = $"{data.SignerName}|{data.SignerUsername}|{data.SignatureTime:o}|{data.DocumentHash}";
            
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            byte[] signatureBytes = null;

            // 使用证书签名（如果有）
            if (user.Certificate != null)
            {
                // 使用BouncyCastle库进行签名
                AsymmetricKeyParameter privateKey = PrivateKeyFactory.CreateKey(user.Certificate.PrivateKey.ExportPkcs8PrivateKey());
                ISigner signer = SignerUtilities.GetSigner("SHA256withRSA");
                signer.Init(true, privateKey);
                signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
                signatureBytes = signer.GenerateSignature();
            }
            else
            {
                // 简化的签名方式（不使用证书）
                using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(data.SignerUsername + "secret_key")))
                {
                    signatureBytes = hmac.ComputeHash(dataBytes);
                }
            }

            // 返回签名数据和签名结果
            return $"{dataString}|{Convert.ToBase64String(signatureBytes)}";
        }

        private static void SaveSignatureToDocument(DrawingDocument drawingDoc, string signedData, SignatureData signatureData)
        {
            // 保存签名数据到文档的自定义属性
            PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];

            // 添加或更新签名状态
            AddOrUpdateProperty(customProps, "SignatureStatus", "Signed");
            AddOrUpdateProperty(customProps, "SignerName", signatureData.SignerName);
            AddOrUpdateProperty(customProps, "SignatureTime", signatureData.SignatureTime.ToString("o"));
            
            // 保存完整的签名数据到命名实体中（更安全）
            try
            {
                NamedEntities namedEntities = drawingDoc.NamedEntities;
                NamedEntity signatureEntity = namedEntities["ElectronicSignatureData"];
                signatureEntity.Value = signedData;
            }
            catch
            {
                drawingDoc.NamedEntities.Add("ElectronicSignatureData", signedData);
            }

            drawingDoc.Save();
        }

        private static void AddOrUpdateProperty(PropertySet propSet, string name, string value)
        {
            try
            {
                Property prop = propSet[name];
                prop.Value = value;
            }
            catch
            {
                propSet.Add(value, name);
            }
        }
    }

    public class SignatureData
    {
        public string SignerName { get; set; }
        public string SignerUsername { get; set; }
        public DateTime SignatureTime { get; set; }
        public string DocumentHash { get; set; }
    }
}
