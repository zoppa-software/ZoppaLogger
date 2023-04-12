Imports System
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
    End Class
End Namespace

