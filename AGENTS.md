# Repository Guidelines

## Project Structure & Module Organization
KometaGUIv3.sln links the WinForms client (`KometaGUIv3/`) and shared library (`KometaGUIv3.Shared/`). Keep UI event handlers in `Forms/`, long-running flows in `Services/`, reusable helpers in `Utils/`, and static assets under `Resources/OverlayPreviews`. Shared models and serializers belong in `KometaGUIv3.Shared/Models` and `Services`; extend them rather than duplicating logic inside the UI project. Build artefacts in `bin/` and `obj/` stay untracked.

## Build, Test, and Development Commands
After cloning, run `dotnet restore KometaGUIv3.sln` to pull ImageSharp, YamlDotNet, and friends. Use `dotnet build KometaGUIv3.sln -c Debug` for local validation and `dotnet run --project KometaGUIv3/KometaGUIv3.csproj` to launch the WinForms shell. Packaging mirrors releases with `dotnet publish KometaGUIv3/KometaGUIv3.csproj -c Release -r win-x64 --self-contained true`.

## Coding Style & Naming Conventions
Stick to 4-space indentation, PascalCase classes, camelCase locals, and expressive method names (`KometaRunner.RunKometaAsync`). Nullable references are enabled in the shared library; address warnings instead of suppressing them. Centralize UI text in `Resources`, avoid blocking calls inside async flows, and record new dependencies explicitly in the respective `.csproj`.

## Testing Guidelines
There is no automated test project yet; when adding logic to `KometaGUIv3.Shared`, create a sibling `KometaGUIv3.Tests` (xUnit or MSTest) and add it to the solution. Cover JSON serialization, scheduler math, and profile migrations with unit tests, then document any manual UI checks you run (e.g., profile creation, overlay preview load).

## Commit & Pull Request Guidelines
Recent commits use concise release-style summaries (`v0.18`, `v0.17`); keep titles short and imperative such as `Add overlay scheduler` when not tagging a version. Group related refactors together and mention config schema changes in the body. Pull requests should state the user-visible impact, list verification steps (`dotnet build`, manual sync), link related issues, and include before/after screenshots for UI tweaks.

## Security & Configuration Tips
Never commit real API keys or the contents of `%AppData%\UnofficialKometaGUI`. Place masked sample configs under `KometaGUIv3.Shared/Models/Samples` if needed, and mark them `CopyToOutputDirectory` only when the app requires them. Audit any Task Scheduler or process-launch edits for absolute paths and proper escaping to avoid running unintended scripts.
