# Inventor工程图电子签名插件

## 功能说明
本插件为Autodesk Inventor 2021提供电子签名功能，支持：
- 用户身份认证（密码或数字证书）
- 在工程图中添加具有防篡改功能的电子签名
- 签名后自动锁定文档，防止未经授权的修改
- 验证签名有效性和文档完整性

## 部署步骤
1. **编译插件**：
   - 使用Visual Studio 2019/2022打开代码
   - 引用Autodesk Inventor 2021 API（COM组件）
   - 安装BouncyCastle NuGet包（用于加密）
   - 生成Release版本的DLL文件

2. **配置插件**：
   - 编辑`InventorElectronicSignature.addin`文件，将`<Assembly>`路径修改为DLL实际存放路径（如`C:\InventorPlugins\InventorElectronicSignature.dll`）

3. **安装插件**：
   - 将修改后的`.addin`文件复制到Inventor插件目录：
     - 个人目录：`%APPDATA%\Autodesk\Inventor 2021\Addins\`
     - 公共目录：`C:\ProgramData\Autodesk\Inventor 2021\Addins\`（需管理员权限）

## 使用方法
1. 打开Inventor 2021，加载工程图（.idw格式）
2. 在Ribbon菜单中找到「电子签名」面板
3. 点击「添加电子签名」，完成用户认证后即可签名
4. 点击「验证签名」可检查文档是否被篡改或签名是否有效

## 注意事项
- 签名使用SHA256哈希算法，确保文档完整性
- 数字证书需提前安装在本地证书库中
- 锁定后的文档需通过插件解锁（需权限控制，当前版本简化处理）
