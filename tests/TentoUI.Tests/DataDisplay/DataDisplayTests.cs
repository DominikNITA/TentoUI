namespace TentoUI.Tests.DataDisplay;

public class DataDisplayTests : TestContext
{
    public record UserRecord(int Id, string Name, string Role);

    [Fact]
    public void Card_RendersHeaderExtraAndActions()
    {
        var cut = RenderComponent<Card>(parameters => parameters
            .Add(p => p.Title, "Card Title")
            .Add(p => p.Extra, (RenderFragment)(b => b.AddContent(0, "Action")))
            .Add(p => p.Hoverable, true)
            .AddChildContent("Card Body"));

        var card = cut.Find(".tento-card-header");
        card.TextContent.Should().Contain("Card Title");
        card.TextContent.Should().Contain("Action");

        var root = cut.Find("div");
        root.ClassList.Should().Contain("hover:shadow-lg");
        root.TextContent.Should().Contain("Card Body");
    }

    [Fact]
    public void Table_RendersContainerHeadersAndRows()
    {
        var data = new System.Collections.Generic.List<UserRecord>
        {
            new(1, "Alice", "Admin"),
            new(2, "Bob", "User")
        };

        var cut = RenderComponent<Table<UserRecord>>(parameters => parameters
            .Add(p => p.DataSource, data)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<UserRecord, int>>(0);
                builder.AddAttribute(1, "Property", (System.Linq.Expressions.Expression<Func<UserRecord, int>>)(u => u.Id));
                builder.AddAttribute(2, "Title", "User ID");
                builder.AddAttribute(3, "Sortable", true);
                builder.CloseComponent();

                builder.OpenComponent<PropertyColumn<UserRecord, string>>(4);
                builder.AddAttribute(5, "Property", (System.Linq.Expressions.Expression<Func<UserRecord, string>>)(u => u.Name));
                builder.AddAttribute(6, "Title", "Name");
                builder.CloseComponent();
            }));

        var container = cut.Find(".tento-table-container");
        container.Should().NotBeNull();

        var ths = cut.FindAll("th");
        ths.Count.Should().Be(2);
        ths[0].TextContent.Should().Contain("User ID");
        ths[1].TextContent.Should().Contain("Name");

        var trs = cut.FindAll("tbody tr");
        trs.Count.Should().Be(2);
    }

    [Fact]
    public void Tag_ColorAndClosable_RendersCorrectStylingAndInvokesClose()
    {
        var closed = false;
        var cut = RenderComponent<Tag>(parameters => parameters
            .Add(p => p.TagColor, TagColor.Success)
            .Add(p => p.Closable, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create<MouseEventArgs>(this, () => closed = true))
            .AddChildContent("Approved"));

        var tag = cut.Find(".tento-tag");
        tag.TextContent.Should().Contain("Approved");
        tag.ClassList.Should().Contain("bg-emerald-50");

        var closeBtn = cut.Find("button");
        closeBtn.Click();
        closed.Should().BeTrue();
    }

    [Fact]
    public void Badge_CountAndDot_RendersSupBadge()
    {
        var cut = RenderComponent<Badge>(parameters => parameters
            .Add(p => p.Count, 5)
            .AddChildContent("<span>Notification</span>"));

        var badge = cut.Find("sup");
        badge.TextContent.Should().Be("5");
    }

    [Fact]
    public void ListView_RendersItemsAndMeta()
    {
        var items = new List<string> { "Item 1", "Item 2" };

        var cut = RenderComponent<ListView<string>>(parameters => parameters
            .Add(p => p.DataSource, items)
            .Add(p => p.ItemTemplate, (RenderFragment<string>)(item => builder =>
            {
                builder.OpenComponent<ListItem>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<ListItemMeta>(2);
                    b.AddAttribute(3, "Title", item);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        var renderedItems = cut.FindAll(".tento-list-item");
        renderedItems.Count.Should().Be(2);
    }
}
