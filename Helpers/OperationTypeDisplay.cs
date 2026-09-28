using WMS.Models;

namespace WMS.Helpers;

public static class OperationTypeDisplay
{
    public static string ToRussian(OperationType type) => type switch
    {
        OperationType.Receipt => "Приёмка",
        OperationType.Placement => "Размещение",
        OperationType.Movement => "Перемещение",
        OperationType.Picking => "Комплектация",
        OperationType.Shipment => "Отгрузка",
        _ => type.ToString()
    };
}