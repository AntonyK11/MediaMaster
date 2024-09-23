using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Border = DocumentFormat.OpenXml.Spreadsheet.Border;
using Name = DocumentFormat.OpenXml.Wordprocessing.Name;

namespace MediaMaster.Services;

public static class ExcelService
{
    public static void SaveSearchResultsToExcel(List<Media> mediaItems, string filePath)
    {
        using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Worksheet worksheet = new Worksheet();
            worksheetPart.Worksheet = worksheet;

            SheetData sheetData = new SheetData();
            worksheet.Append(sheetData);

            Sheets? sheets = spreadsheetDocument.WorkbookPart?.Workbook.AppendChild(new Sheets());

            Sheet sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart?.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Search Results"
            };
            sheets?.Append(sheet);

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell("Name", CellValues.String),
                CreateCell("Path", CellValues.String),
                CreateCell("Notes", CellValues.String),
                CreateCell("Tags", CellValues.String)
            );
            sheetData.Append(headerRow);

            Hyperlinks hyperlinks = new Hyperlinks();

            var hyperlinkId = 1;
            uint rowIndex = 2;
            var hyperlinkStyleIndex = GetHyperLinkStyleIndex(workbookPart);

            foreach (var mediaItem in mediaItems)
            {
                Row row = new() { RowIndex = rowIndex };

                Cell nameCell = CreateCell(mediaItem.Name, CellValues.String);

                Cell pathCell = CreateCell(mediaItem.Uri, CellValues.String);
                pathCell.StyleIndex = hyperlinkStyleIndex;
                var relationshipId = "rId" + hyperlinkId++;
                Hyperlink hyperlink = new Hyperlink()
                {
                    Reference = GetCellReference(2, rowIndex),
                    Id = relationshipId
                };
                hyperlinks.Append(hyperlink);

                Uri pathUri = new(mediaItem.Uri, UriKind.RelativeOrAbsolute);
                worksheetPart.AddHyperlinkRelationship(pathUri, true, relationshipId);

                Cell notesCell = CreateCell(mediaItem.Notes, CellValues.String);

                var tagsString = string.Join(", ", mediaItem.Tags.Select(t => t.Name));
                Cell tagsCell = CreateCell(tagsString, CellValues.String);

                row.Append(nameCell, pathCell, notesCell, tagsCell);
                sheetData.Append(row);

                rowIndex++;
            }

            if (hyperlinks.HasChildren)
            {
                worksheet.Append(hyperlinks);
            }

            Columns columns = AutoFitColumns(mediaItems);
            worksheetPart.Worksheet.InsertAt(columns, 0);

            workbookPart.Workbook.Save();
        }
    }

    private static Cell CreateCell(string text, CellValues dataType)
    {
        Cell cell = new Cell
        {
            DataType = new EnumValue<CellValues>(dataType),
            CellValue = new CellValue(text)
        };
        return cell;
    }

    private static string GetCellReference(uint columnNumber, uint rowNumber)
    {
        var columnName = GetExcelColumnName(columnNumber);
        return columnName + rowNumber;
    }

    private static string GetExcelColumnName(uint columnNumber)
    {
        var dividend = (int)columnNumber;
        var columnName = string.Empty;

        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static Columns AutoFitColumns(List<Media> mediaList)
    {
        Columns columns = new Columns();

        int[] maxColumnWidths = [0, 0, 0, 0];

        foreach (var media in mediaList)
        {
            maxColumnWidths[0] = Math.Max(maxColumnWidths[0], media.Name.Length);
            maxColumnWidths[1] = Math.Max(maxColumnWidths[1], media.Uri.Length);
            maxColumnWidths[2] = Math.Max(maxColumnWidths[2], media.Notes.Length);
            maxColumnWidths[3] = Math.Max(maxColumnWidths[3], string.Join(", ", media.Tags.Select(t => t.Name)).Length);
        }

        for (var i = 0; i < maxColumnWidths.Length; i++)
        {
            double width = Math.Max(maxColumnWidths[i] + 3, 10);
            columns.Append(new Column()
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = width,
                CustomWidth = true
            });
        }

        return columns;
    }

    private static uint GetHyperLinkStyleIndex(WorkbookPart workbookPart)
    {
        WorkbookStylesPart stylesPart;
        if (workbookPart.WorkbookStylesPart == null)
        {
            stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new Stylesheet();
        }
        else
        {
            stylesPart = workbookPart.WorkbookStylesPart;
        }

        Stylesheet stylesheet = stylesPart.Stylesheet;

        stylesheet.Fonts ??= new Fonts { Count = 2, KnownFonts = true };
        stylesheet.Fills ??= new Fills { Count = 1 };
        stylesheet.Borders ??= new Borders { Count = 1 };
        stylesheet.CellStyleFormats ??= new CellStyleFormats { Count = 2 };
        stylesheet.CellFormats ??= new CellFormats { Count = 2 };
        stylesheet.CellStyles ??= new CellStyles { Count = 2 };

        Font font = new Font(
            new Size { Val = 11 },
            new Color { Theme = 1 },
            new Name { Val = "Aptos Narrow" },
            new FontFamily { Val = 2},
            new FontScheme { Val = FontSchemeValues.Minor }
        );
        stylesheet.Fonts.Append(font);
        font = new Font(
            new Underline(),
            new Size { Val = 11 },
            new Color { Theme = 10 },
            new Name { Val = "Aptos Narrow" },
            new FontFamily { Val = 2 },
            new FontScheme { Val = FontSchemeValues.Minor }
        );
        stylesheet.Fonts.Append(font);
        stylesheet.Fonts.Count = (uint)stylesheet.Fonts.ChildElements.Count;

        Fill fill = new Fill(
            new PatternFill { PatternType = PatternValues.None}
        );
        stylesheet.Fills.Append(fill);
        stylesheet.Fills.Count = (uint)stylesheet.Fills.ChildElements.Count;

        Border border = new Border(
            new LeftBorder(),
            new RightBorder(),
            new TopBorder(),
            new BottomBorder(),
            new DiagonalBorder()

        );
        stylesheet.Borders.Append(border);
        stylesheet.Borders.Count = (uint)stylesheet.Borders.ChildElements.Count;

        CellFormat cellFormat = new CellFormat()
        {
            FontId = 0,
            BorderId = 0,
            FillId = 0,
            FormatId = 0,
        };
        stylesheet.CellFormats.Append(cellFormat);
        cellFormat = new CellFormat()
        {
            FontId = 1,
            BorderId = 0,
            FillId = 0,
            FormatId = 1
        };
        stylesheet.CellFormats.Append(cellFormat);
        stylesheet.CellFormats.Count = (uint)stylesheet.CellFormats.ChildElements.Count;

        cellFormat = new CellFormat
        {
            FontId = 0,
            BorderId = 0,
            FillId = 0,
            ApplyNumberFormat = false, 
            ApplyFill = false, 
            ApplyBorder = false,
            ApplyAlignment = false,
            ApplyProtection = false
        };
        stylesheet.CellStyleFormats.Append(cellFormat);
        cellFormat = new CellFormat
        {
            FontId = 1,
            BorderId = 0,
            FillId = 0,
            ApplyNumberFormat = false,
            ApplyFill = false,
            ApplyBorder = false,
            ApplyAlignment = false,
            ApplyProtection = false
        };
        stylesheet.CellStyleFormats.Append(cellFormat);

        CellStyle cellStyle = new CellStyle
        {
            Name = "Normal",
            BuiltinId = 0,
            FormatId = 0,
        };
        stylesheet.CellStyles.Append(cellStyle);
        cellStyle = new CellStyle
        {
            Name = "HyperLink",
            BuiltinId = 8,
            FormatId = 1,
        };
        stylesheet.CellStyles.Append(cellStyle);

        stylesPart.Stylesheet.Save();

        return 1;
    }
}

