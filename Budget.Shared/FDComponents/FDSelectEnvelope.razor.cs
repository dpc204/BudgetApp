using Microsoft.AspNetCore.Components;
using MudBlazor;
using Budget.Shared.Models;

namespace Budget.Shared.FDComponents;

public partial class FDSelectEnvelope
{
    [Parameter]
    public EnvelopeIdName? Value { get; set; }

    [Parameter]
    public EventCallback<EnvelopeIdName?> ValueChanged { get; set; }

    [Parameter]
    public IEnumerable<EnvelopeIdName>? Envelopes { get; set; }

    [Parameter]
    public string Label { get; set; } = "Envelope";

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public Variant Variant { get; set; } = Variant.Outlined;

    [Parameter]
    public Margin Margin { get; set; } = Margin.None;

    [Parameter]
    public bool Dense { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public string? HelperText { get; set; }

    [Parameter]
    public bool Clearable { get; set; }

    [Parameter]
    public string? AdornmentIcon { get; set; }

    [Parameter]
    public Color AdornmentColor { get; set; } = Color.Default;

    [Parameter]
    public string? Class { get; set; }

    private string ConvertEnvelopeToString(EnvelopeIdName? envelope) 
        => envelope?.Name ?? string.Empty;
}
