Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.CompilerServices
Imports System.Threading

''' <summary>カテゴリ別にログを出力するクラスです。</summary>
Public Class CategorizeLogger

    ''' <summary>ログ処理内で例外が発生したことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationException(sender As Object, e As Logger.NotificationExceptionEventArgs)

    ''' <summary>カレントのログファイルが圧縮されたことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationCompressedFile(sender As Object, e As Logger.NotificationCompressedFileEventArgs)

    ''' <summary>圧縮済みログファイルが最大世代数を超えたことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationOrganizeCompressedFile(sender As Object, e As Logger.NotificationOrganizeCompressedFileEventArgs)

    ' 対象ファイル
    Private mLoggers As New Dictionary(Of LogLevel, CateLogger)

    ' 出力エンコード
    Private mEncode As Text.Encoding

    ' 最大ログサイズ
    Private mMaxLogSize As Integer

    ' 最大ログ世代数
    Private mLogGen As Integer

    ' ログ出力レベル
    Private mLogLevel As LogLevel

    ' 日付が変わったら切り替えるかのフラグ
    Private mDateChange As Boolean

    ' キャッシュに保存するログ行数のリミット
    Private mCacheLimit As Integer

    ' ログ出力書式メソッド
    Private mFormatMethod As Func(Of Logger.LogData, String)

    ''' <summary>コンストラクタ。</summary>
    ''' <param name="fatalLogFilePath">出力ファイルパス（致命的なエラー）</param>
    ''' <param name="errorLogFilePath">出力ファイルパス（エラー）</param>
    ''' <param name="warningLogFilePath">出力ファイルパス（警告）</param>
    ''' <param name="informationLogFilePath">出力ファイルパス（案内）</param>
    ''' <param name="debugLogFilePath">出力ファイルパス（デバッグ）</param>
    ''' <param name="encode">出力エンコード。</param>
    ''' <param name="maxLogSize">最大ログファイルサイズ。</param>
    ''' <param name="logGeneration">ログ世代数。</param>
    ''' <param name="logLevel">ログレベル。</param>
    ''' <param name="dateChange">日付の変更でログを切り替えるかの設定。</param>
    ''' <param name="cacheLimit">ログを貯めて置くリミット（超えたらログ出力を優先）</param>
    ''' <param name="formatMethod">ログ出力書式メソッド。</param>
    Public Sub New(fatalLogFilePath As String,
                   errorLogFilePath As String,
                   warningLogFilePath As String,
                   informationLogFilePath As String,
                   debugLogFilePath As String,
                   encode As Text.Encoding,
                   maxLogSize As Integer,
                   logGeneration As Integer,
                   logLevel As LogLevel,
                   dateChange As Boolean,
                   cacheLimit As Integer,
                   formatMethod As Func(Of Logger.LogData, String))
        Dim dic = (New String() {
            fatalLogFilePath,
            errorLogFilePath,
            warningLogFilePath,
            informationLogFilePath,
            debugLogFilePath
        }).
        ToDictionary(
            Function(v) v,
            Function(v) New CateLogger(Me, v)
        )

        For Each pair In New(LogLevel, String)() {
                (LogLevel.Fatal, fatalLogFilePath),
                (LogLevel.Error, errorLogFilePath),
                (LogLevel.Warning, warningLogFilePath),
                (LogLevel.Infomation, informationLogFilePath),
                (LogLevel.Debug, debugLogFilePath)
            }
            Me.mLoggers.Add(pair.Item1, dic(pair.Item2))
        Next

        Me.mEncode = If(encode, Text.Encoding.Default)
        Me.mMaxLogSize = maxLogSize
        Me.mLogGen = logGeneration
        Me.mLogLevel = logLevel
        Me.mDateChange = dateChange
        Me.mCacheLimit = cacheLimit
        Me.mFormatMethod = If(formatMethod, New Func(Of Logger.LogData, String)(AddressOf Me.OnFormatting))
    End Sub

    ''' <summary>ログファイルを圧縮するフォルダへ移動する。</summary>
    ''' <param name="zipPath">移動先ファイルパス。</param>
    ''' <param name="logFile">ログファイル情報。</param>
    ''' <param name="retryCount">リトライ回数。</param>
    ''' <param name="retryInterval">リトライインターバル。</param>
    ''' <returns>移動に成功した場合はTrue、失敗した場合はFalse。</returns>
    Private Function RetryableMove(zipPath As FileInfo,
                                   logFile As IO.FileInfo,
                                   Optional retryCount As Integer = 5,
                                   Optional retryInterval As Integer = 100) As Boolean
        Dim exx As Exception = Nothing

        For i As Integer = 0 To retryCount - 1
            Try
                File.Move(logFile.FullName, zipPath.FullName)
                Return True
            Catch ex As Exception
                exx = ex
                Thread.Sleep(retryInterval)
            End Try
        Next

        Throw exx
    End Function

    ''' <summary>過去ファイルを整理する。</summary>
    ''' <param name="oldFiles">過去ファイルリスト。</param>
    Private Sub ArchiveOldFiles(oldFiles As List(Of String))
        Task.Run(
            Sub()
                ' 削除順にファイルをソート
                oldFiles.Sort()

                ' 削除するファイルを外部に通知
                Dim ev As New Logger.NotificationOrganizeCompressedFileEventArgs(oldFiles)
                Me.OnNotificationOrganizeCompressedFile(ev)

                ' キャンセルされていなければ削除
                If Not ev.Cancel Then
                    Try
                        Do While oldFiles.Count > Me.mLogGen
                            If File.Exists(oldFiles.First()) Then
                                File.Delete(oldFiles.First())
                                oldFiles.RemoveAt(0)
                            End If
                        Loop
                    Catch ex As Exception
                        Me.SendNotificationException(ex)
                    End Try
                End If
            End Sub
        )
    End Sub

    ''' <summary>ログをファイルに出力する文字列を作成する。</summary>
    ''' <param name="dat">ログデータ。</param>
    Protected Overridable Function OnFormatting(dat As Logger.LogData) As String
        Dim lv = "INFO "
        Select Case dat.LogLevel
            Case LogLevel.Debug
                lv = "DEBUG"
            Case LogLevel.Fatal
                lv = "FATAL"
            Case LogLevel.Error
                lv = "ERROR"
            Case LogLevel.Warning
                lv = "WARN "
        End Select

        Return $"[{dat.WriteTime:yyyy/MM/dd HH:mm:ss} {lv} {If(dat.CallerClass IsNot Nothing, $"{dat.CallerClass.Name}.{dat.CallerMethod}({dat.LineNo})", "")}] {dat.LogMessage}"
    End Function

    ''' <summary>例外を発生させたことを通知します。</summary>
    ''' <param name="sendEx">通知する例外、</param>
    ''' <param name="memberName">例外を発生させたメソッド。</param>
    ''' <param name="lineNo">メソッドの行番号。</param>
    Private Sub SendNotificationException(sendEx As Exception, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Debug.WriteLine($"exception {memberName}:{lineNo} {sendEx.Message}")
        Task.Run(
            Sub()
                Try
                    Me.OnNotificationException(New Logger.NotificationExceptionEventArgs(sendEx))
                Catch ex As Exception

                End Try
            End Sub
        )
    End Sub

    ''' <summary>エラーが発生したことを通知するイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationException(e As Logger.NotificationExceptionEventArgs)
        RaiseEvent NotificationException(Me, e)
    End Sub

    ''' <summary>ログファイルが圧縮されたことを通知するイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Private Sub SendNotificationCompressedFile(compressFile As String)
        Task.Run(
            Sub()
                Try
                    Dim fi As New FileInfo(compressFile)
                    Dim args As New Logger.NotificationCompressedFileEventArgs(fi)
                    Me.OnNotificationCompressedFile(args)
                Catch ex As Exception
                    Me.SendNotificationException(ex)
                End Try
            End Sub
        )
    End Sub

    ''' <summary>カレントのログファイルが圧縮されたイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationCompressedFile(e As Logger.NotificationCompressedFileEventArgs)
        RaiseEvent NotificationCompressedFile(Me, e)
    End Sub

    ''' <summary>圧縮済みログファイルが最大世代数を超えたイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationOrganizeCompressedFile(e As Logger.NotificationOrganizeCompressedFileEventArgs)
        RaiseEvent NotificationOrganizeCompressedFile(Me, e)
    End Sub

    ''' <summary>ログレベルを設定します。</summary>
    ''' <param name="lv">新しいログレベル。</param>
    Public Sub ChangeLogLevel(ByVal lv As LogLevel)
        SyncLock Me
            Me.mLogLevel = lv
        End SyncLock
    End Sub

    ''' <summary>エラーレベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingFatal(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Fatal).Write(Logging(LogLevel.Fatal, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>エラーレベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingError(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Error).Write(Logging(LogLevel.Error, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>エラーレベルログを出力します。</summary>
    ''' <param name="ex">例外オブジェクト。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingError(ex As Exception, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Error).Write(Logging(LogLevel.Error, $"{ex.Message}{vbCrLf}{ex.StackTrace}", New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>案内レベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingInformation(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Infomation).Write(Logging(LogLevel.Infomation, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>警告レベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingWarning(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Warning).Write(Logging(LogLevel.Warning, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>デバッグレベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingDebug(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.mLoggers(LogLevel.Debug).Write(Logging(LogLevel.Debug, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>ログ出力用のデータを作成します。</summary>
    ''' <param name="lv">ログレベル。</param>
    ''' <param name="message">メッセージ。</param>
    ''' <param name="caller">呼び出し元情報。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    ''' <returns>ログ出力情報。</returns>
    Private Shared Function Logging(lv As LogLevel, message As String, caller As System.Diagnostics.StackFrame, callMember As String, callLine As Integer) As Logger.LogData
        Dim callType = If(caller.GetMethod()?.ReflectedType, Nothing)
        Return New Logger.LogData(Date.Now, lv, message, callType, callMember, callLine)
    End Function

    ''' <summary>ログ出力終了を待機します。</summary>
    Public Sub WaitFinish()
        Dim loged = Me.mLoggers.Select(
            Function(lg)
                Return Task.Run(Sub() lg.Value.WaitFinish())
            End Function
        ).ToArray()
        Task.WaitAll(loged)
    End Sub

    ''' <summary>各カテゴリ別ログ出力機能です。</summary>
    Private NotInheritable Class CateLogger

        ' 親ログ機能
        Private ReadOnly mParent As CategorizeLogger

        ' 対象ファイル
        Private ReadOnly mLogFile As FileInfo

        ' 書込みバッファ
        Private ReadOnly mQueue As New Queue(Of Logger.LogData)()

        ' エラー書込みバッファ
        Private ReadOnly mErrQueue As New Queue(Of Logger.LogData)()

        ' 前回書込み完了日時
        Private mPrevWriteDate As Date

        ' 書込み中フラグ
        Private mWriting As Boolean

        ''' <summary>コンストラクタ。</summary>
        ''' <param name="parent">親ログ機能</param>
        ''' <param name="logFilePath">ログファイルパス。</param>
        Public Sub New(parent As CategorizeLogger, logFilePath As String)
            Me.mParent = parent

            Dim fi As New FileInfo(logFilePath)
            If Not fi.Directory.Exists Then
                fi.Directory.Create()
            End If
            Me.mLogFile = fi

            Me.mWriting = False
            Me.mPrevWriteDate = Date.MaxValue
        End Sub

        ''' <summary>ログをファイルに出力します。</summary>
        ''' <param name="message">出力するログ。</param>
        Public Sub Write(message As Logger.LogData)
            ' 書き出す情報をため込む
            Dim cnt As Integer
            SyncLock Me
                Me.mQueue.Enqueue(message)
                cnt = Me.mQueue.Count
            End SyncLock

            ' キューにログが溜まっていたら少々待機
            Me.WaitFlushed(cnt, Me.mParent.mCacheLimit)

            ' 別スレッドでファイルに出力
            Dim running As Boolean = False
            SyncLock Me
                If Not Me.mWriting Then
                    Me.mWriting = True
                    running = True
                End If
            End SyncLock
            If running Then
                Task.Run(Sub() Me.ThreadWrite())
            End If
        End Sub

        ''' <summary>キューに溜まっているログを出力します。</summary>
        ''' <param name="cnt">キューのログ数。</param>
        ''' <param name="limit">払い出しリミット。</param>
        ''' <param name="loopCount">待機ループ回数。</param>
        ''' <param name="interval">待機ループインターバル。</param>
        Private Sub WaitFlushed(cnt As Integer,
                                limit As Integer,
                                Optional loopCount As Integer = 10,
                                Optional interval As Integer = 100)
            If cnt > limit Then
                For i As Integer = 0 To loopCount - 1
                    Thread.Sleep(interval)

                    SyncLock Me
                        cnt = Me.mQueue.Count
                    End SyncLock
                    If cnt < limit Then Exit For
                Next
            End If
        End Sub

        ''' <summary>ログをファイルに出力する。</summary>
        Private Sub ThreadWrite()
            Me.mLogFile.Refresh()

            If Me.mLogFile.Exists AndAlso
                   (Me.mLogFile.Length > Me.mParent.mMaxLogSize OrElse Me.ChangeOfDate) Then
                Try
                    ' ファイル名の要素を分割
                    Dim ext = Path.GetExtension(Me.mLogFile.Name)
                    Dim nm = Me.mLogFile.Name.Substring(0, Me.mLogFile.Name.Length - ext.Length)
                    Dim tn = Date.Now.ToString("yyyyMMddHHmmssfff")

                    Dim zipPath = New IO.FileInfo($"{mLogFile.Directory.FullName}\{nm}_{tn}\{nm}{ext}")
                    Try
                        ' 圧縮するフォルダを作成
                        If Not zipPath.Exists Then
                            zipPath.Directory.Create()
                        End If

                        ' ログファイルを圧縮
                        '
                        ' 1. 圧縮フォルダにログファイル移動、移動出来たら圧縮
                        ' 2. 現在のログファイルを圧縮
                        ' 3. ログファイルを圧縮したことを外部に通知
                        If Me.mParent.RetryableMove(zipPath, Me.mLogFile) Then                                           ' 1
                            Dim compressFile = $"{zipPath.Directory.FullName}.zip"
                            ZipFile.CreateFromDirectory(zipPath.Directory.FullName, compressFile)   ' 2
                            Me.mParent.SendNotificationCompressedFile(compressFile)                         ' 3
                        End If

                    Catch ex As Exception
                        Throw
                    Finally
                        Directory.Delete($"{zipPath.Directory.FullName}", True)
                    End Try

                    ' 過去ファイルを整理
                    Dim oldfiles = Directory.GetFiles(Me.mLogFile.Directory.FullName, $"{nm}*.zip").ToList()
                    If oldfiles.Count > Me.mParent.mLogGen Then
                        Me.mParent.ArchiveOldFiles(oldfiles)
                    End If

                Catch ex As Exception
                    SyncLock Me
                        Me.mWriting = False
                    End SyncLock
                    Me.mParent.SendNotificationException(ex)
                    Return
                End Try
            End If

            Try
                Me.mLogFile.Refresh()
                Using sw As New StreamWriter(Me.mLogFile.FullName, True, Me.mParent.mEncode)
                    Dim writed As Boolean
                    Do
                        ' キュー内の文字列を取得
                        '
                        ' 2. キューにログ情報がある
                        '    対象ログレベル以上のログレベルを出力する場合、出力する
                        ' 3. キューにログ情報が空の場合はループを抜けてファイルストリームを閉じる
                        writed = False
                        Dim ln As Logger.LogData? = Nothing
                        Dim outd As Boolean = False
                        SyncLock Me
                            If Me.mErrQueue.Count > 0 Then                  ' 1
                                ln = Me.mErrQueue.Dequeue()
                                outd = True
                            ElseIf Me.mQueue.Count > 0 Then
                                ln = Me.mQueue.Dequeue()                    ' 2
                                outd = True
                            Else
                                Exit Do                                     ' 3
                            End If
                        End SyncLock

                        ' ファイルに書き出す
                        If ln IsNot Nothing Then
                            Try
                                If outd Then
                                    sw.WriteLine(Me.mParent.mFormatMethod(ln.Value))
                                End If
                            Catch ex As Exception
                                Me.mErrQueue.Enqueue(ln.Value)
                                Me.mParent.SendNotificationException(ex)
                            End Try
                            writed = True
                        End If

                        ' 出力した結果、ログファイルが最大サイズを超える場合、ループを抜けてストリームを閉じる
                        Me.mLogFile.Refresh()
                        If Me.mLogFile.Length > Me.mParent.mMaxLogSize OrElse Me.ChangeOfDate Then
                            Exit Do
                        End If
                    Loop While writed
                End Using

                ' 上のループを抜けたとき実行中フラグを落とす
                SyncLock Me
                    Me.mWriting = False
                End SyncLock

                Threading.Thread.Sleep(10)

            Catch ex As Exception
                SyncLock Me
                    Me.mWriting = False
                End SyncLock
                Me.mParent.SendNotificationException(ex)
            Finally
                Me.mPrevWriteDate = Date.Now
            End Try
        End Sub

        ''' <summary>ログ出力終了を待機します。</summary>
        Public Sub WaitFinish()
            For i As Integer = 0 To 5 * 60  ' 事情があって書き込めないとき無限ループするためループ回数制限する
                If Me.IsWriting Then
                    Me.FlushWrite()
                    Threading.Thread.Sleep(1000)
                Else
                    Exit For
                End If
            Next
        End Sub

        ''' <summary>出力スレッドが停止中ならば実行します。</summary>
        Private Sub FlushWrite()
            Try
                ' 出力スレッドが停止中ならばスレッド開始
                Dim running = False
                SyncLock Me
                    If Not Me.mWriting Then
                        Me.mWriting = True
                        running = True
                    End If
                End SyncLock
                If running Then
                    Task.Run(Sub() Me.ThreadWrite())
                End If

            Catch ex As Exception
                Debug.WriteLine($"FlushWrite {ex.Message}")
            End Try
        End Sub

        ''' <summary>書き込み中状態を取得します。</summary>
        ''' <returns>書き込み中状態。</returns>
        Public ReadOnly Property IsWriting() As Boolean
            Get
                SyncLock Me
                    Return (Me.mQueue.Count + Me.mErrQueue.Count > 0)
                End SyncLock
            End Get
        End Property

        ''' <summary>日付の変更でログを切り替えるならば真を返します。</summary>
        ''' <returns>切り替えるならば真。</returns>
        Private ReadOnly Property ChangeOfDate() As Boolean
            Get
                Return Me.mParent.mDateChange AndAlso
                        Me.mPrevWriteDate.Date < Date.Now.Date
            End Get
        End Property

    End Class

End Class
