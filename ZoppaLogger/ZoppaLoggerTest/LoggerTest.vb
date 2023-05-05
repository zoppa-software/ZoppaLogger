Imports Xunit

Namespace ZoppaLoggerTest

    Public Class LoggerTest

        <Fact>
        Sub TestSub()
            Dim logger = ZoppaLogger.Logger.Use(maxLogSize:=200 * 1024)

            For i As Integer = 0 To 10000
                logger.LoggingInformation($"{i} abcdefghijklmnopqrstuvwxyz ABCDEFGHIJLKMNOPQRSTUVWXYZ 1234567890")
            Next
            Debug.WriteLine("End {0}", Date.Now)

            logger.WaitFinish()
        End Sub

        <Fact>
        Sub Test2Sub()
            Dim logger = ZoppaLogger.Logger.UseCategorize(maxLogSize:=200 * 1024)

            logger.LoggingFatal("Fatal Test")
            logger.LoggingError("Error Test")
            logger.LoggingWarning("Warning Test")
            logger.LoggingInformation("Information Test")
            logger.LoggingDebug("Debug Test")

            logger.WaitFinish()
        End Sub

    End Class

End Namespace

