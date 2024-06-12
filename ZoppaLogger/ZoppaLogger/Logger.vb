Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.CompilerServices
Imports System.Threading

''' <summary>ログ出力機能。</summary>
Public Class Logger

    ''' <summary>ログ処理内で例外が発生したことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationException(sender As Object, e As NotificationExceptionEventArgs)

    ''' <summary>カレントのログファイルが圧縮されたことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationCompressedFile(sender As Object, e As NotificationCompressedFileEventArgs)

    ''' <summary>圧縮済みログファイルが最大世代数を超えたことを通知します。</summary>
    ''' <param name="sender">イベント発行元。</param>
    ''' <param name="e">イベントオブジェクト。</param>
    Public Event NotificationOrganizeCompressedFile(sender As Object, e As NotificationOrganizeCompressedFileEventArgs)

    ' 対象ファイル
    Private mLogFile As FileInfo

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
    Private mFormatMethod As Func(Of LogData, String)

    ' 書込みバッファ
    Private ReadOnly mQueue As New Queue(Of LogData)()

    ' エラー書込みバッファ
    Private ReadOnly mErrQueue As New Queue(Of LogData)()

    ' 前回書込み完了日時
    Private mPrevWriteDate As Date

    ' 書込み中フラグ
    Private mWriting As Boolean

    ''' <summary>デフォルトログを使用します。</summary>
    ''' <param name="logFilePath">出力ファイルパス。</param>
    ''' <param name="encode">出力エンコード。</param>
    ''' <param name="maxLogSize">最大ログファイルサイズ。</param>
    ''' <param name="logGeneration">ログ世代数。</param>
    ''' <param name="logLevel">ログレベル。</param>
    ''' <param name="dateChange">日付の変更でログを切り替えるかの設定。</param>
    ''' <param name="cacheLimit">ログを貯めて置くリミット（超えたらログ出力を優先）</param>
    ''' <param name="formatMethod">ログ出力書式メソッド。</param>
    Public Shared Function Use(Optional logFilePath As String = "default.log",
                               Optional encode As Text.Encoding = Nothing,
                               Optional maxLogSize As Integer = 30 * 1024 * 1024,
                               Optional logGeneration As Integer = 10,
                               Optional logLevel As LogLevel = LogLevel.Debug,
                               Optional dateChange As Boolean = False,
                               Optional cacheLimit As Integer = 1000,
                               Optional formatMethod As Func(Of LogData, String) = Nothing) As Logger
        Return UseCustom(Of Logger)(logFilePath, encode, maxLogSize, logGeneration, logLevel, dateChange, cacheLimit, formatMethod)
    End Function

    ''' <summary>カテゴリ別のログを使用します。</summary>
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
    Public Shared Function UseCategorize(Optional fatalLogFilePath As String = "fatal.log",
                                         Optional errorLogFilePath As String = "error.log",
                                         Optional warningLogFilePath As String = "warning.log",
                                         Optional informationLogFilePath As String = "information.log",
                                         Optional debugLogFilePath As String = "debug.log",
                                         Optional encode As Text.Encoding = Nothing,
                                         Optional maxLogSize As Integer = 30 * 1024 * 1024,
                                         Optional logGeneration As Integer = 10,
                                         Optional logLevel As LogLevel = LogLevel.Debug,
                                         Optional dateChange As Boolean = False,
                                         Optional cacheLimit As Integer = 1000,
                                         Optional formatMethod As Func(Of LogData, String) = Nothing) As CategorizeLogger
        Return New CategorizeLogger(fatalLogFilePath,
                                    errorLogFilePath,
                                    warningLogFilePath,
                                    informationLogFilePath,
                                    debugLogFilePath,
                                    If(encode, Text.Encoding.Default),
                                    maxLogSize,
                                    logGeneration,
                                    logLevel,
                                    dateChange,
                                    cacheLimit,
                                    formatMethod)
    End Function

    ''' <summary>カスタムログを使用します。</summary>
    ''' <param name="logFilePath">出力ファイルパス。</param>
    ''' <param name="encode">出力エンコード。</param>
    ''' <param name="maxLogSize">最大ログファイルサイズ。</param>
    ''' <param name="logGeneration">ログ世代数。</param>
    ''' <param name="logLevel">ログレベル。</param>
    ''' <param name="dateChange">日付の変更でログを切り替えるかの設定。</param>
    ''' <param name="cacheLimit">ログを貯めて置くリミット（超えたらログ出力を優先）</param>
    ''' <param name="formatMethod">ログ出力書式メソッド。</param>
    Public Shared Function UseCustom(Of T As {Logger, New})(Optional logFilePath As String = "default.log",
                                                            Optional encode As Text.Encoding = Nothing,
                                                            Optional maxLogSize As Integer = 30 * 1024 * 1024,
                                                            Optional logGeneration As Integer = 10,
                                                            Optional logLevel As LogLevel = LogLevel.Debug,
                                                            Optional dateChange As Boolean = False,
                                                            Optional cacheLimit As Integer = 1000,
                                                            Optional formatMethod As Func(Of LogData, String) = Nothing) As T
        Dim fi As New FileInfo(logFilePath)
        If Not fi.Directory.Exists Then
            fi.Directory.Create()
        End If
        Dim res = New T()
        With res
            .mLogFile = New FileInfo(logFilePath)
            .mEncode = If(encode, Text.Encoding.Default)
            .mMaxLogSize = maxLogSize
            .mLogGen = logGeneration
            .mLogLevel = logLevel
            .mDateChange = dateChange
            .mCacheLimit = cacheLimit
            .mFormatMethod = If(formatMethod, New Func(Of LogData, String)(AddressOf res.OnFormatting))

            .mWriting = False
            .mPrevWriteDate = Date.MaxValue
        End With
        Return res
    End Function

    ''' <summary>ログをファイルに出力します。</summary>
    ''' <param name="message">出力するログ。</param>
    Public Sub Write(message As LogData)
        ' 書き出す情報をため込む
        Dim cnt As Integer
        SyncLock Me
            Me.mQueue.Enqueue(message)
            cnt = Me.mQueue.Count
        End SyncLock

        ' キューにログが溜まっていたら少々待機
        Me.WaitFlushed(cnt, Me.mCacheLimit)

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
RETRY_THREAD:
        Me.mLogFile.Refresh()

        If Me.mLogFile.Exists AndAlso
               (Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate) Then
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
                    If Me.RetryableMove(zipPath) Then                                           ' 1
                        Dim compressFile = $"{zipPath.Directory.FullName}.zip"
                        ZipFile.CreateFromDirectory(zipPath.Directory.FullName, compressFile)   ' 2
                        Me.SendNotificationCompressedFile(compressFile)                         ' 3
                    End If

                Catch ex As Exception
                    Throw
                Finally
                    Directory.Delete($"{zipPath.Directory.FullName}", True)
                End Try

                ' 過去ファイルを整理
                Dim oldfiles = Directory.GetFiles(Me.mLogFile.Directory.FullName, $"{nm}*.zip").ToList()
                If oldfiles.Count > Me.mLogGen Then
                    Me.ArchiveOldFiles(oldfiles)
                End If

            Catch ex As Exception
                SyncLock Me
                    Me.mWriting = False
                End SyncLock
                Me.SendNotificationException(ex)
                Return
            End Try
        End If

        Try
            Me.mLogFile.Refresh()
            Using sw As New StreamWriter(Me.mLogFile.FullName, True, Me.mEncode)
                Dim writed As Boolean
                Do
                    ' キュー内の文字列を取得
                    '
                    ' 2. キューにログ情報がある
                    '    対象ログレベル以上のログレベルを出力する場合、出力する
                    ' 3. キューにログ情報が空の場合はループを抜けてファイルストリームを閉じる
                    writed = False
                    Dim ln As LogData? = Nothing
                    Dim outd As Boolean = False
                    SyncLock Me
                        If Me.mErrQueue.Count > 0 Then                  ' 1
                            ln = Me.mErrQueue.Dequeue()
                            If Me.mLogLevel >= ln.Value.LogLevel Then
                                outd = True
                            End If
                        ElseIf Me.mQueue.Count > 0 Then
                            ln = Me.mQueue.Dequeue()                    ' 2
                            If Me.mLogLevel >= ln.Value.LogLevel Then
                                outd = True
                            End If
                        Else
                            Me.mWriting = False                         ' 3
                            Exit Do
                        End If
                    End SyncLock

                    ' ファイルに書き出す
                    If ln IsNot Nothing Then
                        Try
                            If outd Then
                                sw.WriteLine(Me.mFormatMethod(ln.Value))
                            End If
                        Catch ex As Exception
                            Me.mErrQueue.Enqueue(ln.Value)
                            Me.SendNotificationException(ex)
                        End Try
                        writed = True
                    End If

                    ' 出力した結果、ログファイルが最大サイズを超える場合、ループを抜けてストリームを閉じる
                    Me.mLogFile.Refresh()
                    If Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate Then
                        GoTo RETRY_THREAD
                    End If
                Loop While writed
            End Using

            Threading.Thread.Sleep(10)

        Catch ex As Exception
            SyncLock Me
                Me.mWriting = False
            End SyncLock
            Me.SendNotificationException(ex)
        Finally
            Me.mPrevWriteDate = Date.Now
        End Try
    End Sub

    ''' <summary>ログファイルを圧縮するフォルダへ移動する。</summary>
    ''' <param name="zipPath">移動先ファイルパス。</param>
    ''' <param name="retryCount">リトライ回数。</param>
    ''' <param name="retryInterval">リトライインターバル。</param>
    ''' <returns>移動に成功した場合はTrue、失敗した場合はFalse。</returns>
    Private Function RetryableMove(zipPath As FileInfo,
                                   Optional retryCount As Integer = 5,
                                   Optional retryInterval As Integer = 100) As Boolean
        Dim exx As Exception = Nothing

        For i As Integer = 0 To retryCount - 1
            Try
                File.Move(Me.mLogFile.FullName, zipPath.FullName)
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
                Dim ev As New NotificationOrganizeCompressedFileEventArgs(oldFiles)
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
    Protected Overridable Function OnFormatting(dat As LogData) As String
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
                    Me.OnNotificationException(New NotificationExceptionEventArgs(sendEx))
                Catch ex As Exception

                End Try
            End Sub
        )
    End Sub

    ''' <summary>エラーが発生したことを通知するイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationException(e As NotificationExceptionEventArgs)
        RaiseEvent NotificationException(Me, e)
    End Sub

    ''' <summary>ログファイルが圧縮されたことを通知するイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Private Sub SendNotificationCompressedFile(compressFile As String)
        Task.Run(
            Sub()
                Try
                    Dim fi As New FileInfo(compressFile)
                    Dim args As New NotificationCompressedFileEventArgs(fi)
                    Me.OnNotificationCompressedFile(args)
                Catch ex As Exception
                    Me.SendNotificationException(ex)
                End Try
            End Sub
        )
    End Sub

    ''' <summary>カレントのログファイルが圧縮されたイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationCompressedFile(e As NotificationCompressedFileEventArgs)
        RaiseEvent NotificationCompressedFile(Me, e)
    End Sub

    ''' <summary>圧縮済みログファイルが最大世代数を超えたイベントを発行します。</summary>
    ''' <param name="e">イベントオブジェクト。</param>
    Protected Overridable Sub OnNotificationOrganizeCompressedFile(e As NotificationOrganizeCompressedFileEventArgs)
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
        Me.Write(Logging(LogLevel.Fatal, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>エラーレベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingError(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Error, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>エラーレベルログを出力します。</summary>
    ''' <param name="ex">例外オブジェクト。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingError(ex As Exception, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Error, $"{ex.Message}{vbCrLf}{ex.StackTrace}", New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>案内レベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingInformation(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Infomation, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>警告レベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingWarning(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Warning, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>デバッグレベルログを出力します。</summary>
    ''' <param name="message">ログ。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    Public Sub LoggingDebug(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Debug, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>ログ出力用のデータを作成します。</summary>
    ''' <param name="lv">ログレベル。</param>
    ''' <param name="message">メッセージ。</param>
    ''' <param name="caller">呼び出し元情報。</param>
    ''' <param name="memberName">メソッド名。</param>
    ''' <param name="lineNo">行番号。</param>
    ''' <returns>ログ出力情報。</returns>
    Private Shared Function Logging(lv As LogLevel, message As String, caller As System.Diagnostics.StackFrame, callMember As String, callLine As Integer) As LogData
        Dim callType = If(caller.GetMethod()?.ReflectedType, Nothing)
        Return New LogData(Date.Now, lv, message, callType, callMember, callLine)
    End Function

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
            Return Me.mDateChange AndAlso
                    Me.mPrevWriteDate.Date < Date.Now.Date
        End Get
    End Property

    ''' <summary>ログデータ。</summary>
    Public Structure LogData

        ''' <summary>書き込み日時。</summary>
        Public ReadOnly WriteTime As Date

        ''' <summary>ログレベル。</summary>
        Public ReadOnly LogLevel As LogLevel

        ''' <summary>ログメッセージ。</summary>
        Public ReadOnly LogMessage As String

        ''' <summary>ログ出力クラス。</summary>
        Public ReadOnly CallerClass As Type

        ''' <summary>ログ出力メソッド。</summary>
        Public ReadOnly CallerMethod As String

        ''' <summary>ログ出力行番号。</summary>
        Public ReadOnly LineNo As Integer

        ''' <summary>コンストラクタ。</summary>
        ''' <param name="wtm">書き込み日時。</param>
        ''' <param name="lv">ログレベル。</param>
        ''' <param name="msg">ログメッセージ。</param>
        ''' <param name="cls">ログ出力クラス。</param>
        ''' <param name="mtd">ログ出力メソッド。</param>
        ''' <param name="lineNo">ログ出力行番号。</param>
        Public Sub New(wtm As Date, lv As LogLevel, msg As String, cls As Type, mtd As String, lineNo As Integer)
            Me.WriteTime = wtm
            Me.LogLevel = lv
            Me.LogMessage = msg
            Me.CallerClass = cls
            Me.CallerMethod = mtd
            Me.LineNo = lineNo
        End Sub

    End Structure

    ''' <summary>エラー通知イベント情報。</summary>
    Public NotInheritable Class NotificationExceptionEventArgs
        Inherits Exception

        ''' <summary>発生した例外を取得します。</summary>
        ''' <returns>例外情報。</returns>
        Public ReadOnly Property Target As Exception

        ''' <summary>コンストラクタ。</summary>
        ''' <param name="ex">発生した例外。</param>
        Public Sub New(ex As Exception)
            Me.Target = ex
        End Sub

    End Class

    ''' <summary>ログファイル圧縮通知イベント情報。</summary>
    Public NotInheritable Class NotificationCompressedFileEventArgs
        Inherits EventArgs

        ''' <summary>圧縮したログファイルを示す情報を取得します。</summary>
        Public ReadOnly Property TargetFile As IO.FileInfo

        ''' <summary>コンストラクタ。</summary>
        ''' <param name="targetFile">圧縮したログファイル。</param>
        Public Sub New(targetFile As IO.FileInfo)
            Me.TargetFile = targetFile
        End Sub

    End Class

    ''' <summary>圧縮ログファイルの整理通知イベント情報。</summary>
    Public NotInheritable Class NotificationOrganizeCompressedFileEventArgs
        Inherits EventArgs

        ''' <summary>圧縮したログファイルのリストを取得します。</summary>
        Public ReadOnly Property TargetFiles As New List(Of IO.FileInfo)

        ''' <summary>古いファイルを削除のキャンセルを設定、取得します。</summary>
        Public Property Cancel As Boolean = False

        ''' <summary>コンストラクタ。</summary>
        ''' <param name="targetPaths">圧縮したログファイルリスト。</param>
        Public Sub New(targetPaths As List(Of String))
            For Each ph In targetPaths
                Me.TargetFiles.Add(New IO.FileInfo(ph))
            Next
        End Sub

    End Class

End Class
