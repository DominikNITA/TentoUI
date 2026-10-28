---
name: tentoui-tailwind-setup
description: Production-grade Tailwind CSS v4 setup for any Blazor app that consumes the TentoUI NuGet package. Use right after installing TentoUI, or whenever TentoUI components render unstyled, theme colors are missing, dark mode does not work, or the Tailwind build/CI/publish pipeline must be configured.
framework: net10.0
metadata:
  version: 1.0.0
  author: TentoTemplate
  tags: [tailwind, tailwindcss, blazor, tentoui, build, msbuild, ci, theming]
---

# TentoUI Tailwind Setup Skill

TentoUI components are styled with Tailwind utility classes (`bg-primary-600`, `focus-visible:ring-2`, `dark:bg-slate-900`, ...).
The NuGet package ships **compiled assemblies, not Razor sources**, and `tentoui.min.css` only contains design tokens (`--tento-*`).
**The consuming app must run Tailwind itself**, otherwise components render without most styles. This skill sets that up.

## Use this skill when

- TentoUI was just added to a project (`dotnet add package TentoUI`) and Tailwind is not configured yet.
- Components look unstyled or partially styled, `primary-*` colors are missing, or `dark:` variants do nothing.
- You must wire Tailwind into `dotnet build` / `dotnet publish` / CI / `dotnet watch`.
- You want to change the brand palette, fonts or radius, or add a project-specific wrapper library of components.

## Do not use this skill when

- Choosing or composing TentoUI components (use `tentoui-components`).
- The app uses a Tailwind v3 pipeline with `tailwind.config.js` that cannot be upgraded (see "Tailwind v3 fallback" below).
- Writing backend, CQRS, auth or payments code.

## Core Architecture & Rules

1. **Tailwind v4 only** (CSS-first config). Do not create `tailwind.config.js` for new setups.
2. **Never point Tailwind at the TentoUI DLL.** String literals in .NET assemblies are not scannable. Use the class manifest shipped in the package.
3. **What TentoUI ships** (synced into your project on every build by `build/TentoUI.targets`, into `tailwind/tentoui/`):
   - `tentoui.theme.css` - `@theme` bridge mapping `primary-50..950` to the runtime `--tento-primary-*` variables, plus the `dark` variant (`.dark` or `[data-theme="dark"]`).
   - `tentoui.classes.txt` - every utility class candidate used by the components, for `@source`.
   - Opt out of syncing with `<TentoUISyncTailwind>false</TentoUISyncTailwind>`; change the destination with `<TentoUITailwindTarget>`.
4. **Load order in `<head>`**: `tentoui.min.css` (tokens) first, then the generated `tailwind.css`, then app CSS. Do not also load Bootstrap: Tailwind's preflight replaces it, and both together cause conflicts. Remove the template's `bootstrap.min.css` link and `bootstrap/` folder.
5. **Colors stay runtime-themable.** Never hardcode hex colors in components or wrappers. Brand changes go through `TentoTheme` (C#) or by overriding `--tento-primary-*` variables, not by editing `@theme` palette values.
6. **Generated CSS is a build artifact.** Add `wwwroot/tailwind.css` and `node_modules/` to `.gitignore`; always regenerate it in CI.
7. **Pin versions.** Commit `package-lock.json`, pin `tailwindcss` and `@tailwindcss/cli` to the same version, and use `npm ci` (never `npm install`) in CI.

## API & Architecture Reference

### Files in the consuming project

| File | Purpose |
|---|---|
| `Styles/app.tailwind.css` | Tailwind entry: imports Tailwind, TentoUI theme bridge, and `@source` globs |
| `tailwind/tentoui/*` | Synced from the NuGet package at build (do not edit; may be gitignored) |
| `wwwroot/tailwind.css` | Generated, minified output served to the browser |
| `package.json` | `css:build` and `css:watch` scripts, pinned Tailwind packages |

### MSBuild properties (from the TentoUI package)

| Property | Default | Meaning |
|---|---|---|
| `TentoUISyncTailwind` | `true` | Copy theme + manifest into the project before build |
| `TentoUITailwindTarget` | `$(MSBuildProjectDirectory)\tailwind\tentoui\` | Destination folder |

## Usage Patterns & Code Examples

### 0. Get this skill into your project

The skills ship inside the TentoUI NuGet package. After `dotnet add package TentoUI`, install them once:

```powershell
dotnet msbuild -t:TentoUIInstallSkills                                  # copies to ./.skills/
dotnet msbuild -t:TentoUIInstallSkills -p:TentoUISkillsTarget=.gemini/config/skills/   # other agent folder
```

Or keep them updated on every build with `<TentoUISyncSkills>true</TentoUISyncSkills>` in the `.csproj`.
Pick the folder your AI agent reads (`.skills/`, `.gemini/config/skills/`, `.claude/skills/`, ...). Projects scaffolded with `dotnet new tento-clean --enable-ai-skills true` already contain them.

### 1. Install Tailwind

```powershell
npm init -y
npm install -D tailwindcss @tailwindcss/cli
npm pkg set scripts.css:build="tailwindcss -i Styles/app.tailwind.css -o wwwroot/tailwind.css --minify"
npm pkg set scripts.css:watch="tailwindcss -i Styles/app.tailwind.css -o wwwroot/tailwind.css --watch"
```

Run `dotnet build` once first so TentoUI syncs `tailwind/tentoui/` into the project.

### 2. `Styles/app.tailwind.css`

```css
@import "tailwindcss";
@import "../tailwind/tentoui/tentoui.theme.css";

/* TentoUI component classes (compiled library - scanned via the shipped manifest) */
@source "../tailwind/tentoui/tentoui.classes.txt";

/* Your own markup. Include every folder that contains Razor/C# with class names. */
@source "../Components/**/*.razor";
@source "../Pages/**/*.razor";
@source "../**/*.cs";
```

Add one `@source` per additional project or wrapper library that uses utility classes (for example `../../MyCompany.Ui/**/*.razor`).

### 3. Wire into the build (build, publish, CI)

```xml
<!-- In the web project's .csproj -->
<Target Name="TailwindBuild" BeforeTargets="BeforeBuild" Condition="'$(SkipTailwind)' != 'true'">
  <Exec Command="npm ci" Condition="!Exists('$(MSBuildProjectDirectory)/node_modules')"
        WorkingDirectory="$(MSBuildProjectDirectory)" />
  <Exec Command="npm run css:build" WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

- Verified behavior: with `wwwroot/tailwind.css` deleted, `dotnet publish -c Release` regenerates it and includes it in the published `wwwroot`.
- Fast inner loop: pass `-p:SkipTailwind=true` and run `npm run css:watch` in a second terminal next to `dotnet watch`.
- CI: install Node (LTS), run `dotnet publish -c Release`; the target runs `npm ci` automatically.

### 4. Reference the CSS in `App.razor`

```razor
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="_content/TentoUI/tentoui.min.css" />
    <link rel="stylesheet" href="@Assets["tailwind.css"]" />
    <ImportMap />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src="_content/TentoUI/tentoui.js"></script>
    <script src="@Assets["_framework/blazor.web.js"]"></script>
</body>
```

`@Assets[...]` (with `app.MapStaticAssets()`) fingerprints the file, giving immutable caching and automatic cache busting in production. Use `href="tailwind.css"` only if the app does not call `MapStaticAssets()`.

### 5. Layout

```razor
<TentoConfigProvider DarkMode="@_dark">
    @Body
    <ModalHost />
    <MessageHost />
</TentoConfigProvider>
```

### 6. Customize the brand palette (runtime, no rebuild)

```csharp
builder.Services.AddTentoUI(theme =>
{
    theme.PrimaryColor = "#7c3aed";
    theme.PrimaryHoverColor = "#6d28d9";
});
```

Or override tokens in CSS loaded after `tentoui.min.css`:

```css
:root { --tento-primary-500: #7c3aed; --tento-primary-600: #6d28d9; }
[data-theme="dark"], .dark { --tento-primary-500: #a78bfa; --tento-primary-600: #c4b5fd; }
```

### 7. Dynamic class names (safelisting)

Tailwind only emits classes it can see as complete strings. This is wrong:

```razor
<div class="@($"bg-{_color}-500")">...</div>   @* never generated *@
```

Use complete class names in a lookup:

```csharp
private static readonly Dictionary<Status, string> StatusClass = new()
{
    [Status.Ok] = "bg-emerald-50 text-emerald-700",
    [Status.Bad] = "bg-red-50 text-red-700",
};
```

For classes that come from a database or CMS, safelist them: `@source inline("bg-emerald-50 bg-red-50");`.

### 8. Wrapper libraries

A project-specific component library that wraps TentoUI must:
1. Reference TentoUI normally.
2. Be added as an `@source` glob in every consuming app's `app.tailwind.css`.
3. Never ship its own Tailwind output; the app owns the single generated stylesheet.

### 9. Tailwind v3 fallback (only if v4 is impossible)

Create `tailwind.config.js` with `content` listing `./**/*.razor`, `./tailwind/tentoui/tentoui.classes.txt`, `darkMode: ['variant', ['&:where(.dark, .dark *, [data-theme="dark"], [data-theme="dark"] *)']]`, and `theme.extend.colors.primary` set to `var(--tento-primary-50)` ... `var(--tento-primary-950)`. Prefer migrating to v4.

## Accessibility (WCAG 2.1 AA) Guidelines

- Keep TentoUI's `focus-visible:ring-*` classes working: never add global `outline: none` or purge `focus-visible` variants.
- When changing `--tento-primary-*`, verify 4.5:1 contrast for body text and 3:1 for UI components against both light and dark surfaces.
- Do not override `--tento-border-focus` with a low-contrast value.
- Respect user motion settings in app CSS: `@media (prefers-reduced-motion: reduce) { * { animation-duration: .01ms !important; transition-duration: .01ms !important; } }`.

## Verification & Testing Method

After setup, confirm each of these:

```powershell
dotnet build                      # tailwind/tentoui/ exists, wwwroot/tailwind.css generated
dotnet publish -c Release -o out  # out/wwwroot contains the fingerprinted tailwind css
```

1. `wwwroot/tailwind.css` is non-trivial (tens of KB) and contains `focus-visible\:ring-2`, `text-primary-500`, and `var(--tento-primary-600)`.
2. Rendering `<Button Type="ButtonType.Primary">Hi</Button>` shows a filled primary button, with a visible focus ring on keyboard focus.
3. Toggling `DarkMode` on `<TentoConfigProvider>` switches surfaces and `dark:` variants.
4. A change to a class in your own `.razor` file appears after `npm run css:build`.
5. In the browser dev tools, no `404` for `tailwind.css`, `tentoui.min.css` or `tentoui.js`.
6. CI starts from a clean checkout and still produces the stylesheet (`node_modules` and `wwwroot/tailwind.css` are not committed).

If components are still unstyled, check in this order: `@source` paths (relative to the CSS file), the synced `tailwind/tentoui/` folder, the `<link>` order, and that no Bootstrap stylesheet is loaded.
