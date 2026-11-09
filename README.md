# TentoUI

**AntDesign-patterned Blazor UI Component Library styled with Tailwind CSS v4 for .NET 10.**

- 🌐 **Documentation & Live Demos**: [https://dominiknita.github.io/TentoUI/](https://dominiknita.github.io/TentoUI/)
- 💻 **GitHub Repository**: [https://github.com/DominikNITA/TentoUI](https://github.com/DominikNITA/TentoUI)
- 📦 **NuGet Package**: [https://www.nuget.org/packages/TentoUI](https://www.nuget.org/packages/TentoUI)

- **75+ Accessible Components**: General, Layout, Navigation, Data Entry, Data Display, Feedback, Media (`AudioPlayer`, `Voiceform`), and AI/Copilot primitives (`ChatBubble`, `PromptInput`, `CodeBlock`, `DiffViewer`, `DataGridVirtual`, `KanbanBoard`, `FormWizard`, `StepperForm`, `FilterBuilder`, and more).
- **Zero-Hex Tailwind CSS v4 Engine**: Powered by semantic `--tento-*` design tokens and `TentoConfigProvider` for instant runtime theming and dark mode.
- **Built-in AI Agent Skills**: Ships with `tentoui-components` and `tentoui-tailwind-setup` skills plus `llms.txt` / `llms-full.txt` documentation manifests.
- **WCAG 2.1 AA Compliant**: Keyboard navigation, focus trapping, and ARIA semantics verified with 280+ bUnit tests.

---

## Installation & Setup

### 1. Install NuGet & Tailwind CLI

In your Blazor web project root directory:

```bash
dotnet add package TentoUI
npm init -y
npm install -D tailwindcss @tailwindcss/cli
npm pkg set scripts.css:build="tailwindcss -i Styles/app.tailwind.css -o wwwroot/css/tailwind.min.css --minify"
npm pkg set scripts.css:watch="tailwindcss -i Styles/app.tailwind.css -o wwwroot/css/tailwind.min.css --watch"
```

> **Note**: Running `dotnet build` automatically syncs TentoUI's `@theme` tokens (`tentoui.theme.css`) and precompiled class manifest (`tentoui.classes.txt`) into your project under `tailwind/tentoui/`.

---

### 2. Configure Tailwind CSS v4 (`Styles/app.tailwind.css`)

Create `Styles/app.tailwind.css`:

```css
@import "tailwindcss";
@import "../tailwind/tentoui/tentoui.theme.css";

/* TentoUI component classes (compiled library scanned via the shipped manifest) */
@source "../tailwind/tentoui/tentoui.classes.txt";

/* Your application markup */
@source "../Components/**/*.razor";
@source "../Pages/**/*.razor";
@source "../**/*.cs";
```

---

### 3. Wire Tailwind Compilation into MSBuild (`.csproj`)

Add the following MSBuild target to your web project's `.csproj` to automatically build Tailwind CSS during `dotnet build` and `dotnet publish` (including CI/CD pipelines):

```xml
<Target Name="TailwindBuild" BeforeTargets="BeforeBuild" DependsOnTargets="TentoUISyncTailwindAssets" Condition="'$(SkipTailwind)' != 'true'">
  <Exec Command="npm ci" Condition="!Exists('$(MSBuildProjectDirectory)/node_modules')"
        WorkingDirectory="$(MSBuildProjectDirectory)" />
  <Exec Command="npm run css:build" WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

#### Fast Inner Development Loop
When developing with `dotnet watch`, pass `-p:SkipTailwind=true` and run the Tailwind watcher in a parallel terminal:
```bash
# Terminal 1: Blazor hot reload
dotnet watch -p:SkipTailwind=true

# Terminal 2: Tailwind hot reload
npm run css:watch
```

---

### 4. Register Services (`Program.cs`)

```csharp
using TentoUI;

builder.Services.AddTentoUI();
```

---

### 5. Add Global Imports (`_Imports.razor`)

```razor
@using TentoUI
@using TentoUI.Configuration
```

---

### 6. Include Static Assets (`App.razor` or `index.html`)

```html
<head>
    <!-- TentoUI Design Tokens -->
    <link rel="stylesheet" href="_content/TentoUI/tentoui.min.css" />

    <!-- Compiled Tailwind CSS -->
    <link rel="stylesheet" href="css/tailwind.min.css" />
</head>
<body>
    ...
    <!-- TentoUI JavaScript Interop Layer (Modal, Audio, Tour, Focus Trap) -->
    <script src="_content/TentoUI/tentoui.js"></script>
</body>
```

---

### 7. Wrap Layout & Add Feedback Hosts (`MainLayout.razor`)

Place feedback hosts at the root of your layout to enable modals, notifications, and toasts anywhere via dependency injection:

```razor
<TentoConfigProvider>
    @Body

    <ModalHost />
    <NotificationHost />
    <MessageHost />
</TentoConfigProvider>
```

---

### 8. AI Agent Skills (Optional)

TentoUI ships with built-in agent skills (`tentoui-components` and `tentoui-tailwind-setup`). Extract them directly into your workspace:

```bash
dotnet msbuild -t:TentoUIInstallSkills
```

---

## License

MIT
