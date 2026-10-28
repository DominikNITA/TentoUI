namespace TentoUI.Configuration;

public record TentoConfigContext(
    TentoTheme Theme,
    bool DarkMode,
    ComponentSize ComponentSize,
    Direction Direction
);
