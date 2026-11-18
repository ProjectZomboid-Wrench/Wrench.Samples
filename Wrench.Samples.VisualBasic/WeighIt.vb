Imports Wrench
Imports Zomboid.Java.Characters
Imports Zomboid.Java.Inventory
Imports Zomboid.Lua.ContextMenus

Public NotInheritable Class WeighIt
    Inherits ModInstance

    Public Overrides Sub OnLoad()
        Log.Info("Scales calibrated.")
    End Sub
End Class

Friend NotInheritable Class WeighCommand
    Inherits InventoryCommand(Of InventoryItem)

    Public Overrides ReadOnly Property Text As String
        Get
            Return "Weigh"
        End Get
    End Property

    Public Overrides Sub Execute(item As InventoryItem, player As IsoPlayer)
        Dim weight = item.ActualWeight
        Dim verdict As String
        Select Case weight
            Case Is < 0.1F
                verdict = "featherlight"
            Case Is < 1.0F
                verdict = "light"
            Case Is < 5.0F
                verdict = "hefty"
            Case Else
                verdict = "back-breaking"
        End Select
        HaloTextHelper.AddText(player, $"{item.GetDisplayName()}: {weight:0.##} ({verdict})")
    End Sub
End Class
