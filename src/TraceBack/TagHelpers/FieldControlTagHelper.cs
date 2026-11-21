using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TraceBack.TagHelpers;

[HtmlTargetElement("input", Attributes = "asp-for,tb-field")]
[HtmlTargetElement("select", Attributes = "asp-for,tb-field")]
[HtmlTargetElement("textarea", Attributes = "asp-for,tb-field")]
public sealed class FieldControlTagHelper : TagHelper
{
    // Read the final name and classes emitted by the MVC input helpers.
    public override int Order => 100;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName("tb-hint")]
    public string? HintId { get; set; }

    [HtmlAttributeName("tb-error")]
    public string? ErrorId { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll("tb-field");
        output.Attributes.TryGetAttribute("aria-describedby", out var existing);
        var descriptions = string.Join(' ', new[] { existing?.Value?.ToString(), HintId, ErrorId }
            .OfType<string>().SelectMany(value => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal));
        if (descriptions.Length > 0)
        {
            output.Attributes.SetAttribute("aria-describedby", descriptions);
        }

        var name = output.Attributes["name"]?.Value?.ToString()
            ?? ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
        if (ViewContext.ViewData.ModelState.TryGetValue(name, out var entry) && entry.Errors.Count > 0)
        {
            output.Attributes.SetAttribute("aria-invalid", "true");
        }
    }
}
