using MudBlazor;

namespace NyangoPress.Web.Theme;

public static class NyangoPressTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0B3C5D",
            Secondary = "#0E7490",
            Tertiary = "#19B5C6",
            Info = "#19B5C6",
            Success = "#16A34A",
            Warning = "#F59E0B",
            Error = "#DC2626",
            Background = "#F4F7FA",
            Surface = "#FFFFFF",
            DrawerBackground = "#0B3C5D",
            DrawerText = "#E2E8F0",
            DrawerIcon = "#7CF0FA",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0B3C5D",
            TextPrimary = "#0F172A",
            TextSecondary = "#64748B",
            LinesDefault = "#E2E8F0",
            TableLines = "#E2E8F0"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#19B5C6",
            Secondary = "#0E7490",
            Tertiary = "#F4B942",
            Info = "#38BDF8",
            Success = "#22C55E",
            Warning = "#FBBF24",
            Error = "#EF4444",
            Background = "#091522",
            Surface = "#0F2236",
            DrawerBackground = "#071828",
            DrawerText = "#E2E8F0",
            DrawerIcon = "#19B5C6",
            AppbarBackground = "#0B1E31",
            AppbarText = "#F8FAFC",
            TextPrimary = "#F8FAFC",
            TextSecondary = "#94A3B8",
            LinesDefault = "#1E3A52",
            TableLines = "#1E3A52"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft = "264px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Segoe UI", "system-ui", "sans-serif"]
            },
            H4 = new H4Typography
            {
                FontFamily = ["Poppins", "Inter", "sans-serif"],
                FontWeight = "700"
            },
            H5 = new H5Typography
            {
                FontFamily = ["Poppins", "Inter", "sans-serif"],
                FontWeight = "700"
            },
            H6 = new H6Typography
            {
                FontFamily = ["Poppins", "Inter", "sans-serif"],
                FontWeight = "600"
            }
        }
    };
}
