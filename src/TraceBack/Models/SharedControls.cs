namespace TraceBack.Models;

public enum ButtonTone { Primary, Secondary, Quiet, Destructive }

public sealed record UiButton(string Label, ButtonTone Tone = ButtonTone.Primary,
    string? Href = null, string? Icon = null, bool IconOnly = false, bool Disabled = false,
    bool Submit = false, string? PendingText = null, bool FullWidth = false,
    string? Name = null, string? Value = null);

public enum NoticeTone { Info, Success, Error }

public sealed record UiNotice(string Message, NoticeTone Tone = NoticeTone.Info,
    UiButton? Action = null);

public enum RecoveryArt { Search, Reports, Claims, Access, Service }

public sealed record RecoveryContent(string Heading, string Help, RecoveryArt Art,
    UiButton? Primary = null, UiButton? Secondary = null, bool FullPage = false,
    string? Reference = null);
