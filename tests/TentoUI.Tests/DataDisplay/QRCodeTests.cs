using System.Collections;
using Bunit;
using FluentAssertions;
using QRCoder;
using TentoUI;
using TentoUI.Docs.Components;
using Xunit;

namespace TentoUI.Tests.DataDisplay;

public class QRCodeTests : TestContext
{
    [Fact]
    public void QRCoder_GeneratesValidMatrix()
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode("https://tentoui.dev", QRCodeGenerator.ECCLevel.M);

        data.ModuleMatrix.Should().NotBeNull();
        data.ModuleMatrix.Count.Should().BeGreaterThan(0);
        data.ModuleMatrix[0].Length.Should().Be(data.ModuleMatrix.Count);

        var count = data.ModuleMatrix.Count;
        count.Should().Be(33); // Version 2 (25x25) + 4 border on each side = 33x33

        // In QRCoder, ModuleMatrix includes a 4-module quiet zone border by default
        // So (4, 4) is the top-left of the first 7x7 finder pattern
        data.ModuleMatrix[4][4].Should().BeTrue();
        data.ModuleMatrix[4][5].Should().BeTrue();
        data.ModuleMatrix[4][6].Should().BeTrue();
        data.ModuleMatrix[4][7].Should().BeTrue();
        data.ModuleMatrix[4][8].Should().BeTrue();
        data.ModuleMatrix[4][9].Should().BeTrue();
        data.ModuleMatrix[4][10].Should().BeTrue();

        // Top-Right: (4, count - 11) => (4, 22)
        data.ModuleMatrix[4][count - 11].Should().BeTrue();
        // Bottom-Left: (count - 11, 4) => (22, 4)
        data.ModuleMatrix[count - 11][4].Should().BeTrue();

        // Outside the finder patterns in the quiet zone (rows 0..3, cols 0..3) must be False!
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < count; x++)
            {
                data.ModuleMatrix[y][x].Should().BeFalse();
            }
        }
    }

    [Fact]
    public void QRCode_RendersValidSvg_WithViewBoxAndCrispEdges()
    {
        // Act
        var cut = RenderComponent<QRCode>(parameters => parameters
            .Add(p => p.Value, "https://github.com/DominikNITA/TentoUI")
            .Add(p => p.Size, 180)
            .Add(p => p.Color, "#1d4ed8")
            .Add(p => p.BgColor, "#f8fafc"));

        // Assert
        var svg = cut.Find("svg");
        svg.Should().NotBeNull();
        svg.GetAttribute("width").Should().Be("180");
        svg.GetAttribute("height").Should().Be("180");
        svg.GetAttribute("shape-rendering").Should().Be("crispEdges");

        // Background rect
        var bgRect = cut.Find("rect");
        bgRect.GetAttribute("fill").Should().Be("#f8fafc");

        // Path containing dark modules
        var path = cut.Find("path");
        path.GetAttribute("fill").Should().Be("#1d4ed8");
        path.GetAttribute("d").Should().NotBeNullOrEmpty();
        path.GetAttribute("d").Should().StartWith("M");
    }

    [Fact]
    public void QRCode_RendersCenterIcon_WithBackgroundCutout()
    {
        // Act
        var cut = RenderComponent<QRCode>(parameters => parameters
            .Add(p => p.Value, "https://tentoui.dev")
            .Add(p => p.Size, 160)
            .Add(p => p.Icon, "logo.png")
            .Add(p => p.IconSize, 36)
            .Add(p => p.ErrorLevel, QRCodeErrorLevel.H));

        // Assert
        var image = cut.Find("image");
        image.Should().NotBeNull();
        image.GetAttribute("href").Should().Be("logo.png");

        // Should have a cutout rect behind the image
        var rects = cut.FindAll("rect");
        rects.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void QRCode_RendersExpiredOverlay_WithInteractiveRefresh()
    {
        var refreshed = false;

        // Act
        var cut = RenderComponent<QRCode>(parameters => parameters
            .Add(p => p.Value, "https://tentoui.dev/auth/session")
            .Add(p => p.Status, QRCodeStatus.Expired)
            .Add(p => p.OnRefresh, () => refreshed = true));

        // Assert
        cut.Markup.Should().Contain("QR code expired");
        var refreshBtn = cut.Find("button");
        refreshBtn.Should().NotBeNull();

        // Click refresh
        refreshBtn.Click();
        refreshed.Should().BeTrue();
    }

    [Fact]
    public void TentoMarkupRenderer_RendersQRCode_FromDynamicMarkup()
    {
        // Arrange
        const string markup = """<QRCode Value="https://tentoui.dev" Size="140" Color="#2563eb" />""";

        // Act
        var cut = RenderComponent<TentoMarkupRenderer>(parameters => parameters
            .Add(p => p.Markup, markup));

        // Assert
        cut.Find(".tento-qrcode").Should().NotBeNull();
        var svg = cut.Find("svg");
        svg.GetAttribute("width").Should().Be("140");
        cut.Find("svg path").GetAttribute("fill").Should().Be("#2563eb");
    }
}
