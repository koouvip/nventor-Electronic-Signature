using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace InventorElectronicSignature
{
    // 用户信息类
    public class User
    {
        public string Username { get; set; } // 用户名
        public string FullName { get; set; } // 姓名
        public X509Certificate2 Certificate { get; set; } // 数字证书（可选）
    }

    // 用户认证窗口
    public partial class AuthenticationForm : Form
    {
        public User AuthenticatedUser { get; private set; } // 认证通过的用户

        public AuthenticationForm()
        {
            InitializeComponent();
            InitializeUI(); // 初始化界面
        }

        // 初始化窗口控件
        private void InitializeUI()
        {
            this.Text = "用户认证";
            this.Size = new System.Drawing.Size(350, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ControlBox = false;

            // 用户名标签和输入框
            Label usernameLabel = new Label { Text = "用户名:", Left = 30, Top = 30, Width = 80 };
            TextBox usernameTextBox = new TextBox { Left = 120, Top = 30, Width = 180, Name = "txtUsername" };

            // 密码标签和输入框
            Label passwordLabel = new Label { Text = "密码:", Left = 30, Top = 70, Width = 80 };
            TextBox passwordTextBox = new TextBox { Left = 120, Top = 70, Width = 180, PasswordChar = '*', Name = "txtPassword" };

            // 认证按钮
            Button passwordAuthButton = new Button { Text = "密码认证", Left = 30, Top = 120, Width = 120 };
            Button certAuthButton = new Button { Text = "证书认证", Left = 180, Top = 120, Width = 120 };
            Button cancelButton = new Button { Text = "取消", Left = 120, Top = 160, Width = 100 };

            // 密码认证点击事件
            passwordAuthButton.Click += (s, e) => 
            {
                string username = usernameTextBox.Text;
                string password = passwordTextBox.Text;
                
                if (AuthenticateWithPassword(username, password))
                {
                    AuthenticatedUser = new User { 
                        Username = username,
                        FullName = GetUserFullName(username)
                    };
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("用户名或密码不正确", "认证失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // 证书认证点击事件
            certAuthButton.Click += (s, e) => 
            {
                X509Certificate2 cert = SelectAndVerifyCertificate();
                if (cert != null)
                {
                    AuthenticatedUser = new User { 
                        Username = cert.GetNameInfo(X509NameType.SimpleName, false),
                        FullName = cert.GetNameInfo(X509NameType.Name, false),
                        Certificate = cert
                    };
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };

            // 取消按钮事件
            cancelButton.Click += (s, e) => 
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            // 添加控件到窗口
            this.Controls.AddRange(new Control[] {
                usernameLabel, usernameTextBox, passwordLabel, passwordTextBox,
                passwordAuthButton, certAuthButton, cancelButton
            });
        }

        // 密码认证（实际应用中应对接公司认证系统）
        private bool AuthenticateWithPassword(string username, string password)
        {
            // 示例：简单验证（非空即通过）
            return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }

        // 获取用户全名（示例）
        private string GetUserFullName(string username)
        {
            return username; // 实际应从用户系统获取
        }

        // 选择并验证数字证书
        private X509Certificate2 SelectAndVerifyCertificate()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            // 显示证书选择窗口
            X509Certificate2Collection collection = X509Certificate2UI.SelectFromCollection(
                store.Certificates, "选择数字证书", "请选择用于签名的数字证书", X509SelectionFlag.SingleSelection);

            store.Close();

            if (collection.Count == 0) return null;

            X509Certificate2 cert = collection[0];

            // 验证证书有效期
            if (cert.NotAfter < DateTime.Now || cert.NotBefore > DateTime.Now)
            {
                MessageBox.Show("所选证书已过期或尚未生效", "证书无效", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return cert;
        }

        // 自动生成的InitializeComponent（Windows.Forms要求）
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "AuthenticationForm";
            this.ResumeLayout(false);
        }
    }
}
