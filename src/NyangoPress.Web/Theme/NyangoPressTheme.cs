using MudBlazor;

namespace NyangoPress.Web.Theme;

/// <summary>
/// Thème NyangoPress : palette du cahier des charges (chapitre 11),
/// Poppins pour les titres, Inter pour le texte, clair et sombre.
/// </summary>
public static class NyangoPressTheme
{
    // Couleurs de marque (utilisables ailleurs, ex. graphiques)
    public const string DeepBlue = "#0B3C5D";   // Primaire
    public const string Turquoise = "#0E7490";  // Secondaire
    public const string Aqua = "#19B5C6";       // Accent
    public const string Gold = "#F4B942";       // Fidélité, jetons
    public const string Slate = "#64748B";      // Texte secondaire

    private static readonly string[] TitleFont = { "Poppins", "Inter", "Segoe UI", "sans-serif" };
    private static readonly string[] BodyFont = { "Inter", "Segoe UI", "Roboto", "sans-serif" };

    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = DeepBlue,
            Secondary = Turquoise,
            Tertiary = Aqua,
            Info = Aqua,
            Success = "#16A34A",
            Warning = "#F59E0B",
            Error = "#DC2626",
            Background = "#F1F5F9",
            Surface = "#FFFFFF",
            AppbarBackground = DeepBlue,
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#0F172A",
            DrawerIcon = Turquoise,
            TextPrimary = "#0F172A",
            TextSecondary = Slate,
            ActionDefault = Slate,
            LinesDefault = "#E2E8F0",
            TableLines = "#E2E8F0",
            Divider = "#E2E8F0"
        },
        PaletteDark = new PaletteDark
        {
            // Primaire éclaircie en sombre pour garder un contraste AA
            Primary = Aqua,
            Secondary = Turquoise,
            Tertiary = Gold,
            Info = Aqua,
            Success = "#22C55E",
            Warning = "#FBBF24",
            Error = "#F87171",
            Background = "#0A1822",
            Surface = "#10283A",
            AppbarBackground = "#0B3C5D",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#0D2030",
            DrawerText = "#E2E8F0",
            DrawerIcon = Aqua,
            TextPrimary = "#E2E8F0",
            TextSecondary = "#94A3B8",
            ActionDefault = "#94A3B8",
            LinesDefault = "#1E3A52",
            TableLines = "#1E3A52",
            Divider = "#1E3A52"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft = "260px",
            AppbarHeight = "64px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = BodyFont, FontSize = "0.9375rem", LineHeight = "1.5" },
            H1 = new H1Typography { FontFamily = TitleFont, FontSize = "2.25rem", FontWeight = "700", LineHeight = "1.2" },
            H2 = new H2Typography { FontFamily = TitleFont, FontSize = "1.875rem", FontWeight = "700", LineHeight = "1.25" },
            H3 = new H3Typography { FontFamily = TitleFont, FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.3" },
            H4 = new H4Typography { FontFamily = TitleFont, FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35" },
            H5 = new H5Typography { FontFamily = TitleFont, FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.4" },
            H6 = new H6Typography { FontFamily = TitleFont, FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },
            Subtitle1 = new Subtitle1Typography { FontFamily = BodyFont, FontWeight = "500" },
            Subtitle2 = new Subtitle2Typography { FontFamily = BodyFont, FontWeight = "500" },
            Body1 = new Body1Typography { FontFamily = BodyFont },
            Body2 = new Body2Typography { FontFamily = BodyFont },
            Button = new ButtonTypography { FontFamily = BodyFont, FontWeight = "600", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = BodyFont },
            Overline = new OverlineTypography { FontFamily = BodyFont }
        }
    };
}
