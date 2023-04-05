Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Threading

''' <summary>ログ出力機能。</summary>
Public NotInheritable Class Logger

    ' 単一インスタンス
    Private Shared mLogger As Logger

    ' 対象ファイル
    Private ReadOnly mLogFile As FileInfo

    ' 出力エンコード
    Private ReadOnly mEncode As Text.Encoding

    ' 最大ログサイズ
    Private ReadOnly mMaxLogSize As Integer

    ' 最大ログ世代数
    Private ReadOnly mLogGen As Integer

    ' ログ出力レベル
    Private mLogLevel As LogLevel

    ' 日付が変わったら切り替えるかのフラグ
    Private ReadOnly mDateChange As Boolean

    ' キャッシュに保存するログ行数のリミット
    Private ReadOnly mCacheLimit As Integer

    ' ログ出力書式メソッド
    Private ReadOnly mFormatMethod As Func(Of LogData, String)

    ' 書込みバッファ
    Private ReadOnly mQueue As New Queue(Of LogData)()

    ' 前回書込み完了日時
    Private mPrevWriteDate As Date

    ' 書込み中フラグ
    Private mWriting As Boolean

    ''' <summary>ログ設定を行う。</summary>
    ''' <param name="logFilePath">出力ファイル名。</param>
    ''' <param name="encode">出力エンコード。</param>
    ''' <param name="maxLogSize">最大ログファイルサイズ。</param>
    ''' <param name="logGeneration">ログ世代数。</param>
    ''' <param name="logLevel">ログ出力レベル。</param>
    ''' <param name="dateChange">日付の変更でログを切り替えるかの設定。</param>
    ''' <param name="cacheLimit">ログを貯めて置くリミット（超えたらログ出力を優先）</param>
    ''' <param name="formatMethod">ログ出力書式メソッド。</param>
    Private Sub New(logFilePath As String,
                    encode As Text.Encoding,
                    maxLogSize As Integer,
                    logGeneration As Integer,
                    logLevel As LogLevel,
                    dateChange As Boolean,
                    cacheLimit As Integer,
                    formatMethod As Func(Of LogData, String))
        Me.mLogFile = New FileInfo(logFilePath)
        Me.mEncode = encode
        Me.mMaxLogSize = maxLogSize
        Me.mLogGen = logGeneration
        Me.mWriting = False
        Me.mLogLevel = logLevel
        Me.mDateChange = dateChange
        Me.mPrevWriteDate = Date.MaxValue
        Me.mCacheLimit = cacheLimit
        Me.mFormatMethod = formatMethod
    End Sub

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
        mLogger?.WaitFinish()

        Dim fi As New FileInfo(logFilePath)
        If Not fi.Directory.Exists Then
            fi.Directory.Create()
        End If
        mLogger = New Logger(
            logFilePath, If(encode, Text.Encoding.Default),
            maxLogSize, logGeneration, logLevel, dateChange, cacheLimit,
            If(formatMethod, New Func(Of LogData, String)(AddressOf LogFormat))
        )
        Return mLogger
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
        Dim cacheLmt As Integer = Me.mCacheLimit
        If cnt > cacheLmt Then
            For i As Integer = 0 To 99
                Thread.Sleep(100)
                SyncLock Me
                    cnt = Me.mQueue.Count
                End SyncLock
                If cnt < cacheLmt Then Exit For
            Next
        End If

        ' 別スレッドでファイルに出力
        Dim running As Boolean = False
        SyncLock Me
            If Not Me.mWriting Then
                Me.mWriting = True
                running = True
            End If
        End SyncLock
        If running Then
            Task.Run(Sub() Me.Write())
        End If
    End Sub

    ''' <summary>ログをファイルに出力する。</summary>
    Private Sub Write()
        Me.mLogFile.Refresh()

        If Me.mLogFile.Exists AndAlso
               (Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate) Then
            Try
                ' 以前のファイルをリネーム
                Dim ext = Path.GetExtension(Me.mLogFile.Name)
                Dim nm = Me.mLogFile.Name.Substring(0, Me.mLogFile.Name.Length - ext.Length)
                Dim tn = Date.Now.ToString("yyyyMMddHHmmssfff")

                Dim zipPath = New IO.FileInfo($"{mLogFile.Directory.FullName}\{nm}_{tn}\{nm}{ext}")
                If Not zipPath.Exists Then
                    zipPath.Directory.Create()
                End If
                Try
                    Dim moved = False
                    Dim exx As Exception = Nothing
                    For i As Integer = 0 To 4
                        Try
                            File.Move(Me.mLogFile.FullName, zipPath.FullName)
                            moved = True
                            Exit For
                        Catch ex As Exception
                            exx = ex
                            Thread.Sleep(100)
                        End Try
                    Next
                    If moved Then
                        ZipFile.CreateFromDirectory(
                                zipPath.Directory.FullName, $"{zipPath.Directory.FullName}.zip"
                            )
                    Else
                        Throw exx
                    End If
                Catch ex As Exception
                    Throw
                Finally
                    Directory.Delete($"{mLogFile.Directory.FullName}\{nm}_{tn}", True)
                End Try

                ' 過去ファイルを整理
                Dim oldfiles = Directory.GetFiles(Me.mLogFile.Directory.FullName, $"{nm}*.zip").ToList()
                oldfiles.Sort()
                Do While oldfiles.Count > Me.mLogGen
                    File.Delete(oldfiles.First())
                    oldfiles.RemoveAt(0)
                Loop

            Catch ex As Exception
                SyncLock Me
                    Me.mWriting = False
                End SyncLock
                Return
            End Try
        End If

        Try
            Using sw As New StreamWriter(Me.mLogFile.FullName, True, Me.mEncode)
                Dim writed As Boolean
                Do
                    ' キュー内の文字列を取得
                    writed = False
                    Dim ln As LogData? = Nothing
                    Dim outd As Boolean = False
                    SyncLock Me
                        If Me.mQueue.Count > 0 Then
                            ln = Me.mQueue.Dequeue()
                            If Me.mLogLevel >= ln.Value.LogLevel Then
                                outd = True
                            End If
                        Else
                            Me.mWriting = False
                        End If
                    End SyncLock

                    ' ファイルに書き出す
                    If ln IsNot Nothing Then
                        If outd Then sw.WriteLine(Me.mFormatMethod(ln.Value))
                        writed = True
                    End If

                    Me.mLogFile.Refresh()
                    If Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate Then
                        SyncLock Me
                            Me.mWriting = False
                        End SyncLock
                        Return
                    End If
                Loop While writed
            End Using

            Threading.Thread.Sleep(10)

        Catch ex As Exception
            SyncLock Me
                Me.mWriting = False
            End SyncLock
        Finally
            Me.mPrevWriteDate = Date.Now
        End Try
    End Sub

    Private Shared Function LogFormat(dat As LogData) As String
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
                Task.Run(Sub() Me.Write())
            End If

        Catch ex As Exception

        End Try
    End Sub

    ''' <summary>書き込み中状態を取得します。</summary>
    ''' <returns>書き込み中状態。</returns>
    Public ReadOnly Property IsWriting() As Boolean
        Get
            SyncLock Me
                Return (Me.mQueue.Count > 0)
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

End Class
