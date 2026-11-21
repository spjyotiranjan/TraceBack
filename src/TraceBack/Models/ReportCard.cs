namespace TraceBack.Models;

public enum ReportCardVariant { Browse, Home, Dashboard }

public sealed record ReportCard(Item Item, ReportCardVariant Variant);