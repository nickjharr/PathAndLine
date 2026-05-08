# Changelog

## [1.2.0] - 2026-05-08

### Added
- **Markdown link format** option (Tools → Options → Copy Path and Line → General). When enabled, both commands produce a Markdown link — `[file.cs](path/to/file.cs#L42)` — compatible with GitHub, GitLab, VS Code, and AI coding tools such as OpenAI Codex.
- **Multi-line selection support.** When a selection spanning multiple lines is active, both commands capture the full range: `file.cs (Lines: 42-58)` in plain text mode, or `[file.cs](path/to/file.cs#L42-L58)` in Markdown mode.

### Changed
- Enabling Markdown link format automatically enables Unix-style paths (forward slashes), as backslashes are not valid in Markdown URLs.

---

## [1.1.0] - 2025-03-16

### Added
- **Unix-style paths** option (Tools → Options → Copy Path and Line → General). When enabled, path separators are forward slashes (`/`) instead of backslashes (`\`).

---

## [1.0.0]

Initial release. Adds **Copy full path and line number** and **Copy relative path and line number** to the code editor right-click menu.
