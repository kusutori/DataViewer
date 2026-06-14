# Repository Guidelines

## Project Structure & Module Organization

This repository contains a Windows desktop app built with .NET, WinUI 3, and Microsoft.UI.Reactor.

- `DataViewer.csproj` defines the target (`net10.0-windows10.0.22621.0`), WinUI settings, packages, and Debug-only Reactor devtools support.
- `DataViewer.slnx` is the solution entry point for CLI and IDE builds.
- `App.cs` contains the current Reactor component tree and application startup.
- `Properties/launchSettings.json` defines local launch profiles, including `DataViewer Devtools`.
- `skills/` stores local Reactor documentation and examples. Treat these as reference material, not runtime assets.
- `bin/` and `obj/` are generated build outputs and should not be edited.

## Build, Test, and Development Commands

Run commands from the repository root.

```powershell
dotnet restore DataViewer.slnx
```
Restores NuGet packages.

```powershell
dotnet build DataViewer.slnx
```
Builds the app with the project-defined runtime identifier.

```powershell
dotnet run --project DataViewer.csproj
```
Runs the app locally.

```powershell
dotnet run --project DataViewer.csproj -- --devtools
```
Runs the Debug build with Reactor devtools enabled.

```powershell
dotnet clean DataViewer.slnx
```
Removes generated build outputs.

## Coding Style & Naming Conventions

Use C# with nullable reference types enabled. Keep indentation at four spaces, match the existing file style, and prefer concise Reactor component composition. Use `PascalCase` for components, classes, and methods; `camelCase` for locals and state setters such as `setName`. Keep UI automation names stable and descriptive, for example `.AutomationName("NameInput")`.

## Testing Guidelines

There is no test project yet. When adding tests, create `DataViewer.Tests/`, add it to `DataViewer.slnx`, and use clear names like `ComponentName_Behavior_ExpectedResult`. Run tests with:

```powershell
dotnet test DataViewer.slnx
```

For UI changes, manually run the app and verify the default and devtools launch paths.

## Commit & Pull Request Guidelines

This repository has no commit history yet, so no project-specific convention exists. Use short, imperative commit messages such as `Add file import panel` or `Fix devtools launch profile`.

Pull requests should include a summary, reason for the change, manual verification steps, and screenshots or recordings for visible UI changes. Link related issues and call out changes to target frameworks, package versions, runtime identifiers, or launch profiles.

## Security & Configuration Tips

Do not commit secrets, local machine paths, or generated files from `bin/` and `obj/`. Keep Windows App SDK and Reactor package changes intentional, and document any packaging tradeoffs in `DataViewer.csproj` comments when they affect local run or publish behavior.
