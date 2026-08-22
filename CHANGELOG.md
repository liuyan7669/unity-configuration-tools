# Changelog

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
