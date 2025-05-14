# KedoExecutor - Workflow Execution Engine (.NET Core)  

## Introduction  
Kedo is an open-source workflow management platform for developers, deeply integrated with Large Language Model (LLM) capabilities. It provides a comprehensive solution for visual process design, intelligent task scheduling, and multi-model collaboration. Through its modular architecture, it enables seamless integration of business logic with AI capabilities, supporting rapid development of complex scenarios such as intelligent customer service, data processing, and automated operations. This repository contains the backend executor developed in .NET Core.  

## 1. Local Development Guide  

### 1.1 Environment Setup  
- **Runtime Requirements**:  
  - .NET 7 SDK ([Download](https://dotnet.microsoft.com/download))  
  - Database (MySQL 5.7+ / PostgreSQL 12+ / SQL Server 2016+)  
  - Optional: Redis (for distributed task caching)  

### 1.2 Configuration Steps  

#### 1. Clone the Repository  
```bash
git clone https://github.com/kedowen/KedoExecutorNet.git
cd kedo-executor/Kedo.Web.Entry
```

#### 2. Database Configuration  
Modify the connection string in `appsettings.json`:  
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=kedo_executor;User=root;Password=123456;"
  }
}
```

#### 3. Initialize the Database  
Execute the SQL script in the project:  
```bash
mysql -u root -p kedo_executor < ../database/schema.sql
```

### 1.3 Start the Service  
```bash
dotnet restore
dotnet run
```
The service will start by default at `http://localhost:5000`.  

---

## 2. Production Deployment  

### 2.1 Standalone Publish  
```bash
dotnet publish -c Release -o ./publish --runtime linux-x64
```

### 2.2 Nginx Reverse Proxy  
Example configuration:  
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

## 3. Service Verification  
Access the health check endpoint:  
```bash
curl http://localhost:5000/healthcheck
```
Expected response:  
```json
{"status":"Healthy","executionTime":"15ms"}
```
---
> For containerized deployment, we recommend building your own image based on the project's Dockerfile:  
> ```dockerfile  
> FROM mcr.microsoft.com/dotnet/aspnet:7.0  
> COPY ./publish /app  
> WORKDIR /app  
> ENTRYPOINT ["dotnet", "Kedo.Executor.WebAPI.dll"]  
> ```

## 4. Open Source License  

Kedo is licensed under the **Apache 2.0 License** with additional restrictions. Please carefully review the following terms:  

### 4.1 Apache 2.0 License Key Terms  
- You are free to use, modify, and distribute the code of this project.  
- If you modify the code, you must clearly indicate the changes made.  
- You must include the original license and copyright notice in any distributed copies.  

### 4.2 Supplemental Terms  
In addition to the Apache 2.0 License terms, we have included the following supplemental terms:  
1. **Commercial Authorization**: If you plan to use KedoFlow for commercial purposes (e.g., selling, leasing, or providing it as a service), you must obtain written authorization from us in advance. Please contact our company email: **kedoai@kedowen.com**.  
2. **Logo Usage Restrictions**: You may not modify or remove the KedoFlow logo or related branding without permission.  
3. **Disclaimer**: This project is provided "as-is" without any express or implied warranties. Ensure thorough testing before use.  

For more details, refer to the [LICENSE file](LICENSE).  

---

## 5. Contribution Guidelines  

We warmly welcome community developers to contribute to Kedo! Here are the recommended steps to participate:  

1. **Fork the Project**: Click the "Fork" button at the top right of the GitHub page.  
2. **Clone the Code**: Clone your forked repository locally.  
3. **Create a Branch**: Create a new branch based on the main branch for development.  
4. **Submit Changes**: After completing your development, commit your changes and push them to the remote repository.  
5. **Open a Pull Request**: Submit a Pull Request to the main repository and describe your changes.  

Please adhere to the project's code style and guidelines.  

---

## 6. Contact Us  

If you have any questions or need further assistance, please contact us through the following channels:  

- **Company Website**: [www.kedoai.com](https://www.kedoai.com)
- **Company Email**: kedoai@kedowen.com  
- **GitHub Issues**: Submit your questions or suggestions on the Issues page of this project.  

---

## 7. Copyright Notice  

Kedo and its related logos and branding are the property of **KedoWen Inc.** Unauthorized copying, distribution, or commercial use is prohibited.  

Thank you for choosing Kedo! We hope our platform adds value to your project.