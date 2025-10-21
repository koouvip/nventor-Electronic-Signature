using Autodesk.Inventor;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InventorElectronicSignature
{
    [Guid("B6F6D3A2-7A8E-4F3A-9B7D-8E6C5D1A2B3C"), ProgId("InventorElectronicSignature.AddIn")]
    public class ElectronicSignatureAddIn : ApplicationAddInServer
    {
        private Application _inventorApp;
        private RibbonPanel _signaturePanel;
        private ButtonDefinition _signButton;
        private ButtonDefinition _verifyButton;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            try
            {
                _inventorApp = addInSiteObject.Application;
                CreateRibbonUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"插件激活失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateRibbonUI()
        {
            // 创建Ribbon面板和按钮
            Ribbon drawingRibbon = _inventorApp.UserInterfaceManager.Ribbons["Drawing"];
            _signaturePanel = drawingRibbon.RibbonPanels.Add("电子签名", "ElectronicSignaturePanel", Guid.NewGuid().ToString());

            // 添加签名按钮
            _signButton = _inventorApp.CommandManager.ControlDefinitions.AddButtonDefinition(
                "添加电子签名", "AddElectronicSignature", CommandTypesEnum.kShapeEditCmdType,
                Guid.NewGuid().ToString(), "添加具有法律效力的电子签名", "为工程图添加电子签名并锁定",
                null, null);
            _signButton.OnExecute += SignButton_OnExecute;

            // 添加验证按钮
            _verifyButton = _inventorApp.CommandManager.ControlDefinitions.AddButtonDefinition(
                "验证签名", "VerifySignature", CommandTypesEnum.kShapeEditCmdType,
                Guid.NewGuid().ToString(), "验证电子签名的有效性", "检查工程图的签名是否有效",
                null, null);
            _verifyButton.OnExecute += VerifyButton_OnExecute;

            // 将按钮添加到面板
            _signaturePanel.CommandControls.AddButton(_signButton, false);
            _signaturePanel.CommandControls.AddButton(_verifyButton, false);
        }

        private void SignButton_OnExecute(NameValueMap context)
        {
            try
            {
                if (_inventorApp.ActiveDocument.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
                {
                    MessageBox.Show("请打开一个工程图文档再进行签名操作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DrawingDocument drawingDoc = (DrawingDocument)_inventorApp.ActiveDocument;

                // 检查文档是否已签名
                if (SignatureHelper.IsDocumentSigned(drawingDoc))
                {
                    if (MessageBox.Show("此文档已包含电子签名。是否要重新签名？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    {
                        return;
                    }
                }

                // 用户认证
                using (AuthenticationForm authForm = new AuthenticationForm())
                {
                    if (authForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    User authenticatedUser = authForm.AuthenticatedUser;

                    // 执行签名流程
                    SignatureProcessResult result = SignatureProcessor.ProcessSignature(drawingDoc, authenticatedUser);

                    if (result.Success)
                    {
                        // 锁定文档
                        DocumentLocker.LockDocument(drawingDoc);
                        MessageBox.Show("电子签名已成功添加并生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"签名过程失败: {result.ErrorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行签名时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VerifyButton_OnExecute(NameValueMap context)
        {
            try
            {
                if (_inventorApp.ActiveDocument.DocumentType != DocumentTypeEnum.kDrawingDocumentObject)
                {
                    MessageBox.Show("请打开一个工程图文档再进行验证操作。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DrawingDocument drawingDoc = (DrawingDocument)_inventorApp.ActiveDocument;

                // 验证签名
                VerificationResult result = SignatureVerifier.Verify(drawingDoc);

                if (result.IsVerified)
                {
                    MessageBox.Show($"签名有效。\n签名人: {result.SignerName}\n签名时间: {result.SignatureTime}", 
                                  "验证成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"签名验证失败: {result.Reason}", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证签名时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Deactivate()
        {
            // 清理资源
            _signButton = null;
            _verifyButton = null;
            _signaturePanel = null;
            _inventorApp = null;
        }

        public void ExecuteCommand(int commandID)
        {
            // 执行命令的实现
        }

        public object Automation
        {
            get { return null; }
        }
    }
}
