# 贡献指南

欢迎为 CaptureScreenService 项目做出贡献！我们非常感谢您的支持和参与。本指南将帮助您了解如何正确地为项目做出贡献。

## 项目简介

CaptureScreenService 是一个 Windows 后台服务，用于定期截取屏幕并支持本地存储或邮箱发送。项目采用 .NET 9.0 开发，具有守护进程架构和自动故障恢复能力。

## 如何贡献

### 1. 报告问题

如果您发现了 bug 或有新功能建议，请在 GitHub 上创建一个 issue：

1. 访问项目的 GitHub 仓库
2. 点击 "Issues" 选项卡
3. 点击 "New Issue" 按钮
4. 选择适当的模板（Bug 报告或功能请求）
5. 填写详细的描述，包括：
   - 问题的详细说明
   - 复现步骤（对于 bug）
   - 预期行为
   - 实际行为
   - 环境信息（Windows 版本、.NET 版本等）
   - 相关截图（如果适用）

### 2. 提交代码

#### 步骤 1: Fork 仓库

1. 访问项目的 GitHub 仓库
2. 点击 "Fork" 按钮，将仓库复制到您自己的 GitHub 账户

#### 步骤 2: 克隆仓库

```bash
git clone https://github.com/your-username/CaptureScreenService.git
cd CaptureScreenService
```

#### 步骤 3: 创建分支

```bash
git checkout -b feature/your-feature-name
# 或
git checkout -b fix/your-bug-fix
```

分支命名建议：
- 功能分支：`feature/功能名称`
- Bug 修复：`fix/bug 描述`
- 文档更新：`docs/文档名称`

#### 步骤 4: 编写代码

- 遵循项目的代码风格和规范
- 确保代码质量和可维护性
- 添加适当的注释
- 确保代码通过构建和测试

#### 步骤 5: 提交更改

```bash
git add .
git commit -m "简洁明了的提交信息"
```

提交信息建议：
- 使用现在时态（例如 "Add feature" 而不是 "Added feature"）
- 首行简洁明了（不超过 50 个字符）
- 如有必要，在首行下方添加详细描述

#### 步骤 6: 推送分支

```bash
git push origin feature/your-feature-name
```

#### 步骤 7: 创建 Pull Request

1. 访问您的 forked 仓库
2. 点击 "Pull requests" 选项卡
3. 点击 "New pull request" 按钮
4. 选择您的分支和目标分支
5. 填写 PR 描述，包括：
   - 变更的详细说明
   - 相关的 issue 编号（如果适用）
   - 测试结果
   - 任何其他相关信息
6. 点击 "Create pull request" 按钮

## 代码规范

### C# 代码规范

- 遵循 .NET 代码风格指南
- 使用 4 个空格进行缩进
- 类名使用 PascalCase
- 方法名使用 PascalCase
- 变量名使用 camelCase
- 常量使用 UPPER_CASE
- 接口名以 `I` 开头
- 使用 `var` 关键字进行局部变量声明
- 每行不超过 120 个字符
- 适当使用空行分隔代码块
- 添加必要的 XML 文档注释

### 命名规范

- 命名应清晰、描述性，避免使用缩写
- 变量名应反映其用途
- 方法名应反映其行为
- 类名应反映其职责

### 错误处理

- 使用 try-catch 块捕获异常
- 记录异常信息到日志
- 提供有意义的错误消息
- 避免捕获通用异常而不处理

## 测试要求

- 确保您的更改不会破坏现有功能
- 为新功能添加适当的测试
- 运行构建命令确保代码可以正常编译：
  ```bash
  dotnet build -c Release
  ```
- 测试安装程序构建：
  ```bash
  cd Installer
  ./build.ps1
  ```

## 代码审查

所有 PR 都将经过代码审查过程。审查者可能会提出一些修改建议，以确保代码质量和一致性。请耐心等待审查并及时回应审查者的评论。

## 其他贡献方式

除了代码贡献外，您还可以通过以下方式为项目做出贡献：

- **文档改进**：完善 README.md、DEPLOYMENT.md 等文档
- **翻译**：将文档翻译成其他语言
- **测试**：测试项目在不同环境下的表现
- **反馈**：提供使用体验和改进建议
- **宣传**：向朋友和同事推荐项目

## 行为准则

请参阅 [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) 文件，了解项目的行为准则。我们期望所有参与者能够遵循这些准则，创造一个友好、包容的社区环境。

## 许可证

通过为 CaptureScreenService 项目做出贡献，您同意您的贡献将在 MIT 许可证下发布。请参阅 [LICENSE.txt](LICENSE.txt) 文件了解详情。

## 联系我们

如果您有任何问题或建议，请通过以下方式联系我们：

- GitHub Issues：在项目仓库中创建 issue
- GitHub Discussions：在项目仓库中参与讨论

感谢您对 CaptureScreenService 项目的支持和贡献！
