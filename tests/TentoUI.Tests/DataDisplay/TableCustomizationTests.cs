using System.Linq.Expressions;

namespace TentoUI.Tests.DataDisplay;

public class TableCustomizationTests : TestContext
{
    public record Employee(int Id, string Name, string Department, decimal Salary, bool Active);

    private static System.Collections.Generic.List<Employee> GetSampleEmployees() => new()
    {
        new(1, "Alice Johnson", "Engineering", 125000m, true),
        new(2, "Bob Smith", "Design", 95000m, true),
        new(3, "Charlie Davis", "Engineering", 140000m, false),
        new(4, "Diana Evans", "Marketing", 88000m, true),
        new(5, "Evan Wright", "Engineering", 110000m, true),
        new(6, "Fiona Green", "Design", 102000m, false)
    };

    [Fact]
    public void TableColumn_ManualCustomDisplay_RendersCustomTemplateAndSupportsSortingAndFiltering()
    {
        var employees = GetSampleEmployees();

        var cut = RenderComponent<Table<Employee>>(parameters => parameters
            .Add(p => p.DataSource, employees)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<TableColumn<Employee>>(0);
                builder.AddAttribute(1, "Title", "Employee");
                builder.AddAttribute(2, "Property", (Func<Employee, object?>)(e => e.Name));
                builder.AddAttribute(3, "Sortable", true);
                builder.AddAttribute(4, "Filterable", true);
                builder.AddAttribute(5, "CellTemplate", (RenderFragment<Employee>)(emp => b =>
                {
                    b.OpenElement(0, "strong");
                    b.AddAttribute(1, "class", "custom-emp-badge");
                    b.AddContent(2, $"{emp.Name} (#{emp.Id})");
                    b.CloseElement();
                }));
                builder.CloseComponent();

                builder.OpenComponent<TableColumn<Employee>>(10);
                builder.AddAttribute(11, "Title", "Compensation");
                builder.AddAttribute(12, "Property", (Func<Employee, object?>)(e => e.Salary));
                builder.AddAttribute(13, "Sortable", true);
                builder.AddAttribute(14, "Format", "N0");
                builder.CloseComponent();
            }));

        // Verify custom display template rendered
        var customBadges = cut.FindAll(".custom-emp-badge");
        customBadges.Count.Should().Be(6);
        customBadges[0].TextContent.Should().Be("Alice Johnson (#1)");

        // Filter by "Charlie" using the column filter input
        var filterInput = cut.Find("input[aria-label='Filter Employee']");
        filterInput.Input("Charlie");

        var rowsAfterFilter = cut.FindAll("tbody tr");
        rowsAfterFilter.Count.Should().Be(1);
        rowsAfterFilter[0].TextContent.Should().Contain("Charlie Davis (#3)");

        // Clear filter
        var clearBtn = cut.Find("button[aria-label='Clear filter Employee']");
        clearBtn.Click();
        cut.FindAll("tbody tr").Count.Should().Be(6);

        // Sort by Compensation descending (click twice)
        cut.FindAll("th")[1].Click(); // Asc -> Diana (88,000) first
        cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Diana Evans");

        cut.FindAll("th")[1].Click(); // Desc -> Charlie (140,000) first
        cut.FindAll("tbody tr")[0].TextContent.Should().Contain("Charlie Davis");
    }

    [Fact]
    public void PropertyColumn_WithCustomChildContentAndFilterModes_FiltersAndRendersCorrectly()
    {
        var employees = GetSampleEmployees();

        var cut = RenderComponent<Table<Employee>>(parameters => parameters
            .Add(p => p.DataSource, employees)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<PropertyColumn<Employee, string>>(0);
                builder.AddAttribute(1, "Property", (Expression<Func<Employee, string>>)(e => e.Name));
                builder.AddAttribute(2, "Title", "Name");
                builder.AddAttribute(3, "Filterable", true);
                builder.AddAttribute(4, "FilterMode", ColumnFilterMode.StartsWith);
                builder.CloseComponent();

                builder.OpenComponent<PropertyColumn<Employee, string>>(10);
                builder.AddAttribute(11, "Property", (Expression<Func<Employee, string>>)(e => e.Department));
                builder.AddAttribute(12, "Title", "Department");
                builder.AddAttribute(13, "Filterable", true);
                builder.AddAttribute(14, "FilterMode", ColumnFilterMode.Select);
                builder.AddAttribute(15, "FilterOptions", new[]
                {
                    new ColumnFilterOption("Engineering", "Engineering"),
                    new ColumnFilterOption("Design", "Design"),
                    new ColumnFilterOption("Marketing", "Marketing")
                });
                builder.AddAttribute(16, "ChildContent", (RenderFragment<Employee>)(emp => b =>
                {
                    b.OpenElement(0, "span");
                    b.AddAttribute(1, "data-dept", emp.Department);
                    b.AddContent(2, $"Dept: {emp.Department}");
                    b.CloseElement();
                }));
                builder.CloseComponent();
            }));

        // Select "Engineering" from Department dropdown filter
        var deptSelect = cut.Find("select[aria-label='Filter Department']");
        deptSelect.Change("Engineering");

        var engineeringRows = cut.FindAll("tbody tr");
        engineeringRows.Count.Should().Be(3);

        // Combine with StartsWith filter on Name ("E" -> Evan Wright)
        var nameInput = cut.Find("input[aria-label='Filter Name']");
        nameInput.Input("E");

        var combinedRows = cut.FindAll("tbody tr");
        combinedRows.Count.Should().Be(1);
        combinedRows[0].TextContent.Should().Contain("Evan Wright");
        combinedRows[0].TextContent.Should().Contain("Dept: Engineering");
    }

    [Fact]
    public void TemplateColumn_WithCustomFilterPredicateAndCustomSorter_AppliesCustomLogic()
    {
        var employees = GetSampleEmployees();

        var cut = RenderComponent<Table<Employee>>(parameters => parameters
            .Add(p => p.DataSource, employees)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<TemplateColumn<Employee>>(0);
                builder.AddAttribute(1, "Title", "Salary Band");
                builder.AddAttribute(2, "Sortable", true);
                builder.AddAttribute(3, "CustomSorter", (Comparison<Employee>)((a, b) => a.Salary.CompareTo(b.Salary)));
                builder.AddAttribute(4, "Filterable", true);
                builder.AddAttribute(5, "FilterPredicate", (Func<Employee, string, bool>)((emp, query) =>
                    query.Equals("high", StringComparison.OrdinalIgnoreCase) ? emp.Salary >= 120000m : emp.Salary < 120000m));
                builder.AddAttribute(6, "ChildContent", (RenderFragment<Employee>)(emp => b =>
                    b.AddContent(0, emp.Salary >= 120000m ? "High" : "Standard")));
                builder.CloseComponent();
            }));

        var filterInput = cut.Find("input[aria-label='Filter Salary Band']");
        filterInput.Input("high");

        var highEarners = cut.FindAll("tbody tr");
        highEarners.Count.Should().Be(2); // Alice (125k) and Charlie (140k)
    }

    [Fact]
    public void Table_PaginationCustomization_SizeChangerAndQuickJumperAndQueryChangeWork()
    {
        var employees = Enumerable.Range(1, 25)
            .Select(i => new Employee(i, $"User {i}", i % 2 == 0 ? "Engineering" : "Sales", 50000m + i * 1000, true))
            .ToList();

        TableQueryState? lastQuery = null;

        var cut = RenderComponent<Table<Employee>>(parameters => parameters
            .Add(p => p.DataSource, employees)
            .Add(p => p.PageSize, 10)
            .Add(p => p.ShowSizeChanger, true)
            .Add(p => p.ShowQuickJumper, true)
            .Add(p => p.OnQueryChange, EventCallback.Factory.Create<TableQueryState>(this, q => lastQuery = q))
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<TableColumn<Employee>>(0);
                builder.AddAttribute(1, "Title", "Name");
                builder.AddAttribute(2, "Property", (Func<Employee, object?>)(e => e.Name));
                builder.AddAttribute(3, "Sortable", true);
                builder.AddAttribute(4, "Filterable", true);
                builder.CloseComponent();
            }));

        cut.FindAll("tbody tr").Count.Should().Be(10);

        // Quick jump to page 3
        var jumper = cut.Find("input[aria-label='Jump to page']");
        jumper.Change("3");

        cut.FindAll("tbody tr").Count.Should().Be(5);
        lastQuery.Should().NotBeNull();
        lastQuery!.PageIndex.Should().Be(3);

        // Change page size to 20 -> resets to page 1 with 20 rows
        var sizeChanger = cut.Find("select[aria-label='Items per page']");
        sizeChanger.Change("20");

        cut.FindAll("tbody tr").Count.Should().Be(20);
        lastQuery.PageIndex.Should().Be(1);
        lastQuery.PageSize.Should().Be(20);
    }

    [Fact]
    public void Table_ColumnVisibilityAndExpandableRows_WorkCorrectly()
    {
        var employees = GetSampleEmployees().Take(2).ToList();

        var cut = RenderComponent<Table<Employee>>(parameters => parameters
            .Add(p => p.DataSource, employees)
            .Add(p => p.ExpandedRowTemplate, emp => b => b.AddContent(0, $"Details for {emp.Name}"))
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<TableColumn<Employee>>(0);
                builder.AddAttribute(1, "Title", "Visible Col");
                builder.AddAttribute(2, "Property", (Func<Employee, object?>)(e => e.Name));
                builder.CloseComponent();

                builder.OpenComponent<TableColumn<Employee>>(10);
                builder.AddAttribute(11, "Title", "Hidden Col");
                builder.AddAttribute(12, "Property", (Func<Employee, object?>)(e => e.Department));
                builder.AddAttribute(13, "Visible", false);
                builder.CloseComponent();
            }));

        // Hidden column is not rendered
        cut.Markup.Should().Contain("Visible Col");
        cut.Markup.Should().NotContain("Hidden Col");

        // Expand row 1
        var expandBtn = cut.Find("button[aria-label='Expand row 1']");
        expandBtn.Click();

        cut.Find(".tento-table-expanded-row").TextContent.Should().Contain("Details for Alice Johnson");
    }
}
