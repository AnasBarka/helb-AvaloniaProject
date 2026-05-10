using System.Collections.Generic;
using OceanStock.Models;

namespace OceanStock;

public static class Session
{
    public static User? CurrentUser { get; set; }
    
    public static List<string>? SelectedUserIds { get; set; }

}