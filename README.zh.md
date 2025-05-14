# KedoExecutor - 工作流执行引擎 (.NET Core)  

## 介绍
Kedo是一个面向开发者的开源工作流管理平台，深度整合大语言模型（LLM）能力，提供可视化流程设计、智能任务调度和多模型协同的全流程解决方案。通过模块化架构实现业务逻辑与AI能力的无缝衔接，支持快速构建智能客服、数据处理、自动化运维等复杂场景。 本仓库执行器是后台是.netCore 语言开发。

## 1. 本地运行指南  

### 1.1 环境准备  
- **运行时要求**：  
  - .NET 7 SDK ([下载地址](https://dotnet.microsoft.com/download))  
  - 数据库（MySQL 5.7+ / PostgreSQL 12+ / SQL Server 2016+）  
  - 可选：Redis（用于分布式任务缓存）  

### 1.2 配置步骤  

#### 1. 克隆代码库  
```bash
git clone https://github.com/kedowen/KedoExecutorNet.git
cd kedo-executor/Kedo.Web.Entry
```

#### 2. 数据库配置  
修改 `appsettings.json` 中的连接字符串：  
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=kedo_executor;User=root;Password=123456;"
  }
}
```

#### 3. 初始化数据库  
执行项目中的SQL脚本：  
```bash
mysql -u root -p kedo_executor < ../database/schema.sql
```

### 1.3 启动服务  
```bash
dotnet restore
dotnet run
```
服务默认启动在 `http://localhost:5000`  

---

## 2. 生产环境部署  

### 2.1 独立发布  
```bash
dotnet publish -c Release -o ./publish --runtime linux-x64
```

### 2.2 使用Nginx反向代理  
示例配置：  
```nginx
server {
    listen 80;
    server_name executor.yourdomain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
    }
}
```

---

## 3. 服务验证  
访问健康检查端点：  
```bash
curl http://localhost:5000/healthcheck
```
预期返回：  
```json
{"status":"Healthy","executionTime":"15ms"}
```
---
> 如需容器化部署支持，建议基于本项目Dockerfile自行构建镜像：  
> ```dockerfile  
> FROM mcr.microsoft.com/dotnet/aspnet:7.0  
> COPY ./publish /app  
> WORKDIR /app  
> ENTRYPOINT ["dotnet", "Kedo.Executor.WebAPI.dll"]  
> ```

## 4. 开源协议

Kedo 遵循 **Apache 2.0 License** 协议，并附加了部分限制条款，请仔细阅读以下内容：

### 4.1 Apache 2.0 License 主要条款
- 您可以自由地使用、修改、分发本项目的代码。
- 如果您对代码进行了修改，必须明确标注修改的部分。
- 您需要在分发的副本中包含原始许可证和版权声明。

### 4.2 补充协议
除了 Apache 2.0 License 的条款外，我们还添加了以下补充协议：
1. **商业化授权**：如果您计划将 KedoFlow 用于商业用途（如出售、租赁或作为服务提供），必须事先获得我们的书面授权。请联系公司邮箱：**kedoai@kedowen.com**。
2. **Logo 使用限制**：未经许可，不得修改或移除 KedoFlow 的 Logo 或相关品牌标识。
3. **责任声明**：本项目按“原样”提供，不附带任何明示或暗示的担保。请确保在使用前充分测试代码。

更多详情请参考 [LICENSE 文件](LICENSE)。

---

## 5. 贡献指南

我们非常欢迎社区开发者为 Kedo提供贡献！以下是参与项目的建议步骤：

1. **Fork 项目**：点击 GitHub 页面右上角的 "Fork" 按钮。
2. **克隆代码**：将您的 Fork 仓库克隆到本地。
3. **创建分支**：基于主分支创建一个新的分支以进行开发。
4. **提交更改**：完成开发后，提交您的更改并推送到远程仓库。
5. **发起 Pull Request**：向主仓库提交 Pull Request 并描述您的更改。

请注意遵循项目的代码风格和规范。

---

## 6. 联系我们

如果您有任何问题或需要进一步的帮助，请通过以下方式联系我们：

- **公司网站**：[www.kedowen.com](http://www.kedoai.com)
- **公司邮箱**：kedoai@kedowen.com
- **GitHub Issues**：在本项目的 Issues 页面提交您的问题或建议。

---

## 7. 版权声明

Kedo及其相关 Logo、品牌标识均为 **KedoWen Inc.** 所有。未经授权，禁止任何形式的复制、传播或商业使用。

感谢您选择 Kedo！希望我们的平台能够为您的项目带来价值。
