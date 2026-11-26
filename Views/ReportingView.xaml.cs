using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WMS.ViewModels;

namespace WMS.Views;

public partial class ReportingView : UserControl
{
    public ReportingView()
    {
        InitializeComponent();
    }

    private void PrintReport_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReportingViewModel viewModel)
            return;

        var title = viewModel.SelectedReportIndex switch
        {
            0 => "Отчёт об остатках товаров",
            1 => "Отчёт о поступлениях и отгрузках",
            2 => "Отчёт о межскладских перемещениях",
            3 => "Отчёт по поставщикам и покупателям",
            4 => "Анализ результатов инвентаризации",
            _ => "Анализ загруженности складов"
        };

        var document = new FlowDocument
        {
            PagePadding = new Thickness(35),
            ColumnWidth = double.PositiveInfinity,
            FontFamily =
                new System.Windows.Media.FontFamily("Arial"),
            FontSize = 10
        };

        document.Blocks.Add(new Paragraph(new Run(title))
        {
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });

        var periodText =
            $"Период: " +
            $"{viewModel.FromDate?.ToString("dd.MM.yyyy") ?? "не указан"} — " +
            $"{viewModel.ToDate?.ToString("dd.MM.yyyy") ?? "не указан"}\n" +
            $"Склад: {viewModel.SelectedWarehouse?.Name ?? "Все склады"}";

        document.Blocks.Add(new Paragraph(new Run(periodText)));

        var table = new Table();
        AddColumns(table, viewModel.SelectedReportIndex);

        var headerGroup = new TableRowGroup();
        var header = new TableRow();

        foreach (var name in GetHeaders(viewModel.SelectedReportIndex))
        {
            header.Cells.Add(new TableCell(
                new Paragraph(new Run(name))
                {
                    FontWeight = FontWeights.Bold
                }));
        }

        headerGroup.Rows.Add(header);
        table.RowGroups.Add(headerGroup);

        var rowsGroup = new TableRowGroup();

        switch (viewModel.SelectedReportIndex)
        {
            case 0:
                foreach (var row in viewModel.StockRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.WarehouseName,
                        row.Article,
                        row.ProductName,
                        row.Quantity.ToString()));
                }
                break;

            case 1:
                foreach (var row in viewModel.ActivityRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.Operation,
                        row.WarehouseName,
                        row.PartnerName,
                        row.DocumentNumber,
                        row.Date.ToString("dd.MM.yyyy"),
                        row.Quantity.ToString()));
                }
                break;

            case 2:
                foreach (var row in viewModel.TransferRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.DocumentNumber,
                        row.Date.ToString("dd.MM.yyyy"),
                        row.SourceWarehouse,
                        row.DestinationWarehouse,
                        row.Quantity.ToString(),
                        row.Status));
                }
                break;

            case 3:
                foreach (var row in viewModel.PartnerRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.Type,
                        row.Name,
                        row.DocumentsCount.ToString(),
                        row.Quantity.ToString()));
                }
                break;

            case 4:
                foreach (var row in viewModel.StockTakingRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.DocumentNumber,
                        row.WarehouseName,
                        row.StartDate.ToString("dd.MM.yyyy"),
                        row.CompletedDate?.ToString("dd.MM.yyyy") ?? "—",
                        row.PositionsCount.ToString(),
                        row.DifferencesCount.ToString(),
                        row.ExcessQuantity.ToString(),
                        row.DeficitQuantity.ToString(),
                        row.Status));
                }
                break;

            case 5:
                foreach (var row in viewModel.CapacityRows)
                {
                    rowsGroup.Rows.Add(CreateRow(
                        row.WarehouseName,
                        row.Area.ToString("0.##"),
                        row.LocationsCount.ToString(),
                        row.Capacity.ToString(),
                        row.Stored.ToString(),
                        row.Free.ToString(),
                        $"{row.LoadPercent:0.#}%"));
                }
                break;
        }

        table.RowGroups.Add(rowsGroup);
        document.Blocks.Add(table);

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return;

        printDialog.PrintDocument(
            ((IDocumentPaginatorSource)document).DocumentPaginator,
            title);
    }

    private static string[] GetHeaders(int reportIndex) =>
        reportIndex switch
        {
            0 => new[]
            {
                "Склад", "Артикул", "Товар", "Количество"
            },
            1 => new[]
            {
                "Операция", "Склад", "Контрагент",
                "Документ", "Дата", "Количество"
            },
            2 => new[]
            {
                "Документ", "Дата", "Откуда",
                "Куда", "Количество", "Статус"
            },
            3 => new[]
            {
                "Тип", "Контрагент", "Документов", "Количество"
            },
            4 => new[]
            {
                "Документ", "Склад", "Начало", "Завершена",
                "Позиций", "Расхождений", "Излишки",
                "Недостача", "Статус"
            },
            _ => new[]
            {
                "Склад", "Площадь", "Ячеек", "Вместимость",
                "Хранится", "Свободно", "Загруженность"
            }
        };

    private static void AddColumns(Table table, int reportIndex)
    {
        foreach (var _ in GetHeaders(reportIndex))
        {
            table.Columns.Add(new TableColumn
            {
                Width = GridLength.Auto
            });
        }
    }

    private static TableRow CreateRow(params string[] values)
    {
        var row = new TableRow();

        foreach (var value in values)
        {
            row.Cells.Add(new TableCell(
                new Paragraph(new Run(value))));
        }

        return row;
    }
}