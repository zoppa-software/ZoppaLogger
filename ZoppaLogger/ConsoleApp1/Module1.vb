Imports System.Text
Imports ZoppaLogger

Module Module1

    Sub Main()
        Dim logger = ZoppaLogger.Logger.UseCustom(Of MyLogger)(maxLogSize:=200 * 1024)
        AddHandler logger.NotificationCompressedFile,
            Sub(sender, e)
                Console.WriteLine($"Compressed file: {e.TargetFile}")
            End Sub
        AddHandler logger.NotificationOrganizeCompressedFile,
            Sub(sender, e)
                For i As Integer = 0 To e.TargetFiles.Count - 1
                    Console.WriteLine($"Organize compressed file: {i}:{e.TargetFiles(i)}")
                Next
            End Sub

        Debug.WriteLine("Start {0}", Date.Now)
        For i As Integer = 0 To 10000
            logger.LoggingInformation($"{i} abcdefghijklmnopqrstuvwxyz ABCDEFGHIJLKMNOPQRSTUVWXYZ 1234567890")
            'Threading.Thread.Sleep(10)
        Next
        Debug.WriteLine("End {0}", Date.Now)

        logger.WaitFinish()
    End Sub

    Private Class MyLogger
        Inherits ZoppaLogger.Logger

    End Class

End Module