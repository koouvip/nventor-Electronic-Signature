using Autodesk.Inventor;
using System;
using System.Windows.Forms;

namespace InventorElectronicSignature
{
    // 文档锁定工具
    public static class DocumentLocker
    {
        // 锁定文档（防止修改）
        public static void LockDocument(DrawingDocument drawingDoc)
        {
            // 1. 设置文档为只读
            drawingDoc.ReadOnly = true;

            // 2. 在属性中标记锁定状态
            PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];
            SignatureProcessor.AddOrUpdateProperty(customProps, "DocumentLocked", "True");

            // 3. 添加事件监听，阻止编辑操作
            drawingDoc.OnSave += DrawingDoc_OnSave; // 保存事件
            drawingDoc.OnActivate += DrawingDoc_OnActivate; // 激活事件

            drawingDoc.Save(); // 保存锁定状态
        }

        // 文档激活时触发（提示已锁定）
        private static void DrawingDoc_OnActivate(object Document, EventTimingEnum BeforeOrAfter, NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            HandlingCode = HandlingCodeEnum.kEventHandled;
            if (BeforeOrAfter == EventTimingEnum.kBefore)
            {
                DrawingDocument doc = Document as DrawingDocument;
                if (IsDocumentLocked(doc))
                {
                    MessageBox.Show("此文档已被电子签名锁定，禁止修改。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // 文档保存时触发（阻止修改后保存）
        private static void DrawingDoc_OnSave(object Document, EventTimingEnum BeforeOrAfter, NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            HandlingCode = HandlingCodeEnum.kEventHandled;
            if (BeforeOrAfter == EventTimingEnum.kBefore)
            {
                DrawingDocument doc = Document as DrawingDocument;
                if (IsDocumentLocked(doc))
                {
                    MessageBox.Show("文档已被电子签名锁定，无法修改。", "操作受限", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    HandlingCode = HandlingCodeEnum.kCancelEvent; // 取消保存操作
                }
            }
        }

        // 解锁文档（需权限控制，此处简化）
        public static void UnlockDocument(DrawingDocument drawingDoc)
        {
            drawingDoc.ReadOnly = false; // 取消只读

            // 更新锁定状态
            PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];
            SignatureProcessor.AddOrUpdateProperty(customProps, "DocumentLocked", "False");

            // 移除事件监听
            drawingDoc.OnSave -= DrawingDoc_OnSave;
            drawingDoc.OnActivate -= DrawingDoc_OnActivate;

            drawingDoc.Save();
        }

        // 检查文档是否已锁定
        public static bool IsDocumentLocked(DrawingDocument drawingDoc)
        {
            try
            {
                PropertySet customProps = drawingDoc.PropertySets["User Defined Properties"];
                Property lockedProp = customProps["DocumentLocked"];
                return lockedProp.Value.ToString().Equals("True", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false; // 未找到锁定标记，视为未锁定
            }
        }
    }
}
