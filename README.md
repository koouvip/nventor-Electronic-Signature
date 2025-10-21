# nventor-Electronic-Signature
# Inventor工程图电子签名插件  
本插件支持在Autodesk Inventor 2021工程图中添加具有法律效力的电子签名，包含用户认证、签名添加、文档锁定和验证功能。  

## 使用步骤  
1. 下载代码 → 编译生成DLL文件。  
2. 修改`.addin`文件中的`<Assembly>`路径。  
3. 将`.addin`和DLL文件部署到Inventor插件目录。  
4. 在Inventor中使用「电子签名」面板操作。  

## 注意事项  
- 代码需使用Visual Studio 2019/2022编译，引用Inventor 2021 API和BouncyCastle库。  
- 部署时需确保路径正确，避免插件加载失败。  
