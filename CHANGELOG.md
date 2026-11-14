# Changelog

## [Unreleased]

## [3.1.0] - 2026-11-14

- 接入 OpenUPM 官方 tag 发布工作流，自动触发扫描并检查注册表发布结果。

- 统一 Cowart 包版本为 3.1.0。

- 声明 Inspector Groups 3.1.0 依赖，随包安装统一 Inspector 支持。

- 按功能整理 Cowart 菜单分类与名称，菜单声明保留在所属包内。

## [2.0.0] - 2026-11-12

### 破坏性变更

- `com.cowart.configuration-tools` 改为 `com.cowart.config-loader`；更新业务项目的安装地址、程序集和源码引用。
- 完成包标识、目录、程序集、命名空间、脚本及文档改名，保持资源 GUID 和包独立性。

## [1.0.7] - 2026-11-08

- 统一 Cowart 包图标：重新设计为按功能类别配色的圆角图标（带 { } 的配置文件），同步更新 `Icon.png` 与 `Icon@2x.png`。

## [1.0.6] - 2026-08-23

- 新增包专用图标资源。

## [1.0.5] - 2026-08-15

- 新增独立 `TextLoader`，支持从本地路径或 URL 加载任意文本文件。
- 所有 JSON 配置加载器改为组合复用 `TextLoader`，并保持原有公共 API 兼容。
- 新增原始文本与泛型 JSON 文件加载的 EditMode 自动化测试。

## [1.0.3] - 2026-08-07

- Added `Cowart > 配置模板 > 创建 game_config.json 示例` for `JsonConfigLoader<T>`.

## [1.0.2] - 2026-07-31

- Added `JsonConfigLoader<T>` for loading user-defined JSON configuration files.
- Removed the unused `RawTextConfig` API and sample command.
- Completed XML documentation for the public `QuestionConfig` API.

## [1.0.1] - 2026-07-31

- Moved configuration sample commands to `Cowart > 配置模板`.

## [1.0.0] - 2026-07-16

- Initial UPM package.
