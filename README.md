# TentoUI

**AntDesign-patterned Blazor UI Component Library styled with Tailwind CSS v4 for .NET 10.**

- 🌐 **Documentation & Live Demos**: [https://dominiknita.github.io/TentoUI/](https://dominiknita.github.io/TentoUI/)
- 💻 **GitHub Repository**: [https://github.com/DominikNITA/TentoUI](https://github.com/DominikNITA/TentoUI)
- 📦 **NuGet Package**: [https://www.nuget.org/packages/TentoUI](https://www.nuget.org/packages/TentoUI)

- **75+ Accessible Components**: General, Layout, Navigation, Data Entry, Data Display, Feedback, and AI/Copilot primitives (`ChatBubble`, `PromptInput`, `CodeBlock`, `DiffViewer`, `DataGridVirtual`, `KanbanBoard`, `CommandPalette`, `FilterBuilder`, and more).
- **Zero-Hex Tailwind CSS v4 Engine**: Powered by semantic `--tento-*` design tokens and `TentoConfigProvider` for instant runtime theming and dark mode.
- **Built-in AI Agent Skills**: Ships with `tentoui-components` and `tentoui-tailwind-setup` skills plus `llms.txt` / `llms-full.txt` documentation manifests.
- **WCAG 2.1 AA Compliant**: Keyboard navigation, focus trapping, and ARIA semantics verified with 250+ bUnit tests.

## Quick Start

```bash
dotnet add package TentoUI
npm install -D tailwindcss @tailwindcss/cli
```

Register services in `Program.cs`:

```csharp
using TentoUI;

builder.Services.AddTentoUI();
```

Add imports to `_Imports.razor`:

```razor
@using TentoUI
@using TentoUI.Configuration
```

Unpack AI Agent Skills into your solution (optional):

```bash
dotnet msbuild -t:TentoUIInstallSkills
```

## License

MIT
