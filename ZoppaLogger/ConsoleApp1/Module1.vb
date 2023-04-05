Module Module1

    Sub Main()
        Dim logger = ZoppaLogger.Logger.Use()
        logger.LoggingInformation("Hello World!")

        logger.WaitFinish()
    End Sub

End Module
