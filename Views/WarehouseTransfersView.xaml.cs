using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WMS.ViewModels;

namespace WMS.Views;

public partial class WarehouseTransfersView : UserControl
{
    public WarehouseTransfersView()
    {
        InitializeComponent();
    }

    private void PrintTransfer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WarehouseTransfersViewModel viewModel ||
            viewModel.SelectedTransfer is not { } transfer)
        {
            MessageBox.Show(
                "Сначала выберите документ перемещения.",
                "Печать документа",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var document = new FlowDocument
        {
            PagePadding = new Thickness(45),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new System.Windows.Media.FontFamily("Arial"),
            FontSize = 12
        };

        document.Blocks.Add(new Paragraph(new Run("ВНУТРЕННЕЕ ПЕРЕМЕЩЕНИЕ"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });

        var headerText =
            $"Документ: {transfer.DocumentNumber}\n" +
            $"Дата оформления: {transfer.CreatedAtLocal:dd.MM.yyyy HH:mm}\n" +
            $"Планируемая дата доставки: {transfer.PlannedDeliveryDate:dd.MM.yyyy}\n" +
            $"Склад-отправитель: {transfer.SourceWarehouse?.Name}\n" +
            $"Склад-получатель: {transfer.DestinationWarehouse?.Name}\n" +
            $"Ответственный отправителя: {transfer.SourceResponsibleName}\n" +
            $"Ответственный получателя: {transfer.DestinationResponsibleName}\n" +
            $"Статус: {transfer.StatusDisplay}";

        document.Blocks.Add(new Paragraph(new Run(headerText)));

        var table = new Table();
        table.Columns.Add(new TableColumn
        {
            Width = new GridLength(4, GridUnitType.Star)
        });
        table.Columns.Add(new TableColumn
        {
            Width = new GridLength(2, GridUnitType.Star)
        });
        table.Columns.Add(new TableColumn
        {
            Width = new GridLength(2, GridUnitType.Star)
        });
        table.Columns.Add(new TableColumn
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        var headerGroup = new TableRowGroup();
        var header = new TableRow();
        header.Cells.Add(new TableCell(new Paragraph(new Run("Товар"))));
        header.Cells.Add(new TableCell(new Paragraph(new Run("Откуда"))));
        header.Cells.Add(new TableCell(new Paragraph(new Run("Куда"))));
        header.Cells.Add(new TableCell(new Paragraph(new Run("Кол-во"))));
        headerGroup.Rows.Add(header);
        table.RowGroups.Add(headerGroup);

        var rowsGroup = new TableRowGroup();

        foreach (var item in transfer.Items)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(
                $"{item.Product?.Name} ({item.Product?.Article})"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(
                item.SourceStorageLocation?.Code ?? "—"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(
                item.DestinationStorageLocation?.Code ?? "—"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(
                item.Quantity.ToString()))));
            rowsGroup.Rows.Add(row);
        }

        table.RowGroups.Add(rowsGroup);
        document.Blocks.Add(table);

        var confirmationText =
            $"Отправлено: {transfer.ShippedAtLocal?.ToString("dd.MM.yyyy HH:mm") ?? "—"}\n" +
            $"Получено: {transfer.ReceivedAtLocal?.ToString("dd.MM.yyyy HH:mm") ?? "—"}";

        document.Blocks.Add(new Paragraph(new Run(confirmationText)));

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                $"Перемещение {transfer.DocumentNumber}");
        }
    }
}