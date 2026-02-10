namespace C3D.Extensions.VisualStudioDebug.WellKnown;

public class Engines
{
    /*
     *       Available default engine {029D0BD7-2382-44D7-BBE9-E45D5514E439} Microsoft Java Debug Engine 1
      Available default engine {1202F5B4-3522-4149-BAD8-58B2079D704F} T-SQL 1
      Available default engine {2E36F1D4-B23C-435D-AB41-18E608940038} Managed (.NET Core, .NET 5+) 1
      Available default engine {3B476D35-A401-11D2-AAD4-00C04F990171} Native 1
      Available default engine {3FBCC828-6272-46D4-B5FA-B7E643672113} JavaScript and TypeScript 1
      Available default engine {449EC4CC-30D2-4032-9256-EE18EB41B62B} Managed (.NET Framework) 1
      Available default engine {47843766-18EE-4226-B2EE-0BAA38E1E0D3} Managed (Native compilation) 1
      Available default engine {5FFF7536-0C87-462D-8FD2-7971D948E6DC} Managed (.NET Framework 3.x/2.0) 1
      Available default engine {7E17F634-FECC-4519-8369-AF099B022A53} Managed (.NET Core, .NET 5+) 1
      Available default engine {92EF0900-2251-11D2-B72E-0000F87572EF} Managed/Native 1
      Available default engine {F200A7E7-DEA5-11D0-B854-00A0244A1DE2} Script 1
      Available default engine {F4453496-1DB8-47F8-A7D5-31EBDDC2EC96} GPU - Software Emulator 1
      Available default engine {FB0D4648-F776-4980-95F8-BB7F36EBC1EE} Managed (.NET Framework 4.x) 1

     */
    // From Default Transport
    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (.NET Framework)")]
    public const string Net1 = "{449EC4CC-30D2-4032-9256-EE18EB41B62B}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (.NET Framework 3.x/2.0)")]
    public const string Net2 = "{5FFF7536-0C87-462D-8FD2-7971D948E6DC}";

    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (.NET Framework 3.x/2.0)")]
    public const string Net3 = Net2;
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (.NET Framework 4.x)")]
    public const string Net4 = "{FB0D4648-F776-4980-95F8-BB7F36EBC1EE}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "JavaScript and TypeScript")]
    public const string JavaScript = "{394120B6-2FF9-4D0D-8953-913EF5CD0BCD}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "JavaScript and TypeScript")]
    public const string TypeScript = JavaScript;
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Microsoft Java Debug Engine")]
    public const string Java = "{029D0BD7-2382-44D7-BBE9-E45D5514E439}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "T-SQL")]
    public const string TSQL = "{1202F5B4-3522-4149-BAD8-58B2079D704F}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (.NET Core, .NET 5+)")]
    public const string Net = "{2E36F1D4-B23C-435D-AB41-18E608940038}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Native")]
    public const string Native = "{3B476D35-A401-11D2-AAD4-00C04F990171}";

    [System.ComponentModel.DataAnnotations.Display(Name = "Managed (Native compilation)")]
    public const string Managed = "{47843766-18EE-4226-B2EE-0BAA38E1E0D3}";
    
    [System.ComponentModel.DataAnnotations.Display(Name = "Managed/Native")]
    public const string ManagedNative = "{92EF0900-2251-11D2-B72E-0000F87572EF}";

    [System.ComponentModel.DataAnnotations.Display(Name = "Script")]
    public const string Script = "{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}";

    [System.ComponentModel.DataAnnotations.Display(Name = "GPU - Software Emulator")]
    public const string GPU = "{F4453496-1DB8-47F8-A7D5-31EBDDC2EC96}";
}
