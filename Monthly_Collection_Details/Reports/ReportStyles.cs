using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Monthly_Collection_Details.Reports
{
    public static class ReportStyles
    {
        // ===== TABLE HEADER =====
        public static IContainer HeaderStyle(IContainer container) => container
            .Border(1)
            .Background("#87CEFA") // blue
            .Padding(2)
            .AlignCenter()
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(9)
                 .Bold()
                 .FontColor("#000000"));

        // ===== NORMAL CELL =====
        public static IContainer NormalCell(IContainer container) => container
            .Border(1)
            .Padding(1)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .FontColor("#4E342E")); // Dark brown

        // ===== CASH / IMPORTANT =====
        public static IContainer YellowCell(IContainer container) => container
            .Border(1)
            .Background("#FFFACD") //yellow
            .Padding(2)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .FontColor("#4E342E"));

        // ===== NON-CASH =====
        public static IContainer CyanCell(IContainer container) => container
            .Border(1)
            .Background("#E0FFFF") // cyan
            .Padding(2)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .FontColor("#4E342E"));

        // ===== CREDIT / TOTALS =====
        public static IContainer GreenCyanCell(IContainer container) => container
            .Border(1)
            .Background("#F5E6B8") // Yellow-brown
            .Padding(2)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8).Bold()
                 .FontColor("#4E342E"));

        // ===== MIXED =====
        public static IContainer YellowCyanCell(IContainer container) => container
            .Border(1)
            .Background("#F8E1A1") // Warm yellow
            .Padding(2)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .FontColor("#4E342E"));

        public static IContainer TotalGreenCyanCell(IContainer container) => container
            .Border(1)
            .Background("#90EE90")
            .Padding(1)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .FontColor("#4E342E"));

        // ===== TOTAL ROW STYLE =====
        public static IContainer TotalBoldCell(IContainer container) => container
            .Border(1)
            .Background("#ffffff") 
            .Padding(1)
            .AlignMiddle()
            .DefaultTextStyle(x =>
                x.FontSize(8)
                 .Bold()                
                 .FontColor("#000000"));
    }   
}
