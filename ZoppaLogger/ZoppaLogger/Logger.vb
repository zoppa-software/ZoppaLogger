Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Threading

''' <summary>���O�o�͋@�\�B</summary>
Public NotInheritable Class Logger

    ' �P��C���X�^���X
    Private Shared mLogger As Logger

    ' �Ώۃt�@�C��
    Private ReadOnly mLogFile As FileInfo

    ' �o�̓G���R�[�h
    Private ReadOnly mEncode As Text.Encoding

    ' �ő働�O�T�C�Y
    Private ReadOnly mMaxLogSize As Integer

    ' �ő働�O���㐔
    Private ReadOnly mLogGen As Integer

    ' ���O�o�̓��x��
    Private mLogLevel As LogLevel

    ' ���t���ς������؂�ւ��邩�̃t���O
    Private ReadOnly mDateChange As Boolean

    ' �L���b�V���ɕۑ����郍�O�s���̃��~�b�g
    Private ReadOnly mCacheLimit As Integer

    ' ���O�o�͏������\�b�h
    Private ReadOnly mFormatMethod As Func(Of LogData, String)

    ' �����݃o�b�t�@
    Private ReadOnly mQueue As New Queue(Of LogData)()

    ' �O�񏑍��݊�������
    Private mPrevWriteDate As Date

    ' �����ݒ��t���O
    Private mWriting As Boolean

    ''' <summary>���O�ݒ���s���B</summary>
    ''' <param name="logFilePath">�o�̓t�@�C�����B</param>
    ''' <param name="encode">�o�̓G���R�[�h�B</param>
    ''' <param name="maxLogSize">�ő働�O�t�@�C���T�C�Y�B</param>
    ''' <param name="logGeneration">���O���㐔�B</param>
    ''' <param name="logLevel">���O�o�̓��x���B</param>
    ''' <param name="dateChange">���t�̕ύX�Ń��O��؂�ւ��邩�̐ݒ�B</param>
    ''' <param name="cacheLimit">���O�𒙂߂Ēu�����~�b�g�i�������烍�O�o�͂�D��j</param>
    ''' <param name="formatMethod">���O�o�͏������\�b�h�B</param>
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

    ''' <summary>�f�t�H���g���O���g�p���܂��B</summary>
    ''' <param name="logFilePath">�o�̓t�@�C���p�X�B</param>
    ''' <param name="encode">�o�̓G���R�[�h�B</param>
    ''' <param name="maxLogSize">�ő働�O�t�@�C���T�C�Y�B</param>
    ''' <param name="logGeneration">���O���㐔�B</param>
    ''' <param name="logLevel">���O���x���B</param>
    ''' <param name="dateChange">���t�̕ύX�Ń��O��؂�ւ��邩�̐ݒ�B</param>
    ''' <param name="cacheLimit">���O�𒙂߂Ēu�����~�b�g�i�������烍�O�o�͂�D��j</param>
    ''' <param name="formatMethod">���O�o�͏������\�b�h�B</param>
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

    ''' <summary>���O���t�@�C���ɏo�͂��܂��B</summary>
    ''' <param name="message">�o�͂��郍�O�B</param>
    Public Sub Write(message As LogData)
        ' �����o���������ߍ���
        Dim cnt As Integer
        SyncLock Me
            Me.mQueue.Enqueue(message)
            cnt = Me.mQueue.Count
        End SyncLock

        ' �L���[�Ƀ��O�����܂��Ă����班�X�ҋ@
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

        ' �ʃX���b�h�Ńt�@�C���ɏo��
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

    ''' <summary>���O���t�@�C���ɏo�͂���B</summary>
    Private Sub Write()
        Me.mLogFile.Refresh()

        If Me.mLogFile.Exists AndAlso
               (Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate) Then
            Try
                ' �ȑO�̃t�@�C�������l�[��
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

                ' �ߋ��t�@�C���𐮗�
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
                    ' �L���[���̕�������擾
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

                    ' �t�@�C���ɏ����o��
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

    ''' <summary>���O���x����ݒ肵�܂��B</summary>
    ''' <param name="lv">�V�������O���x���B</param>
    Public Sub ChangeLogLevel(ByVal lv As LogLevel)
        SyncLock Me
            Me.mLogLevel = lv
        End SyncLock
    End Sub

    ''' <summary>�G���[���x�����O���o�͂��܂��B</summary>
    ''' <param name="message">���O�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingFatal(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Fatal, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>�G���[���x�����O���o�͂��܂��B</summary>
    ''' <param name="message">���O�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingError(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Error, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>�G���[���x�����O���o�͂��܂��B</summary>
    ''' <param name="ex">��O�I�u�W�F�N�g�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingError(ex As Exception, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Error, $"{ex.Message}{vbCrLf}{ex.StackTrace}", New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>�ē����x�����O���o�͂��܂��B</summary>
    ''' <param name="message">���O�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingInformation(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Infomation, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>�x�����x�����O���o�͂��܂��B</summary>
    ''' <param name="message">���O�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingWarning(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Warning, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>�f�o�b�O���x�����O���o�͂��܂��B</summary>
    ''' <param name="message">���O�B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    Public Sub LoggingDebug(message As String, <CallerMemberName> Optional memberName As String = "", <CallerLineNumber> Optional lineNo As Integer = 0)
        Me.Write(Logging(LogLevel.Debug, message, New System.Diagnostics.StackFrame(1), memberName, lineNo))
    End Sub

    ''' <summary>���O�o�͗p�̃f�[�^���쐬���܂��B</summary>
    ''' <param name="lv">���O���x���B</param>
    ''' <param name="message">���b�Z�[�W�B</param>
    ''' <param name="caller">�Ăяo�������B</param>
    ''' <param name="memberName">���\�b�h���B</param>
    ''' <param name="lineNo">�s�ԍ��B</param>
    ''' <returns>���O�o�͏��B</returns>
    Private Shared Function Logging(lv As LogLevel, message As String, caller As System.Diagnostics.StackFrame, callMember As String, callLine As Integer) As LogData
        Dim callType = If(caller.GetMethod()?.ReflectedType, Nothing)
        Return New LogData(Date.Now, lv, message, callType, callMember, callLine)
    End Function

    ''' <summary>���O�o�͏I����ҋ@���܂��B</summary>
    Public Sub WaitFinish()
        For i As Integer = 0 To 5 * 60  ' ��������ď������߂Ȃ��Ƃ��������[�v���邽�߃��[�v�񐔐�������
            If Me.IsWriting Then
                Me.FlushWrite()
                Threading.Thread.Sleep(1000)
            Else
                Exit For
            End If
        Next
    End Sub

    ''' <summary>�o�̓X���b�h����~���Ȃ�Ύ��s���܂��B</summary>
    Private Sub FlushWrite()
        Try
            ' �o�̓X���b�h����~���Ȃ�΃X���b�h�J�n
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

    ''' <summary>�������ݒ���Ԃ��擾���܂��B</summary>
    ''' <returns>�������ݒ���ԁB</returns>
    Public ReadOnly Property IsWriting() As Boolean
        Get
            SyncLock Me
                Return (Me.mQueue.Count > 0)
            End SyncLock
        End Get
    End Property

    ''' <summary>���t�̕ύX�Ń��O��؂�ւ���Ȃ�ΐ^��Ԃ��܂��B</summary>
    ''' <returns>�؂�ւ���Ȃ�ΐ^�B</returns>
    Private ReadOnly Property ChangeOfDate() As Boolean
        Get
            Return Me.mDateChange AndAlso
                    Me.mPrevWriteDate.Date < Date.Now.Date
        End Get
    End Property

    ''' <summary>���O�f�[�^�B</summary>
    Public Structure LogData

        ''' <summary>�������ݓ����B</summary>
        Public ReadOnly WriteTime As Date

        ''' <summary>���O���x���B</summary>
        Public ReadOnly LogLevel As LogLevel

        ''' <summary>���O���b�Z�[�W�B</summary>
        Public ReadOnly LogMessage As String

        ''' <summary>���O�o�̓N���X�B</summary>
        Public ReadOnly CallerClass As Type

        ''' <summary>���O�o�̓��\�b�h�B</summary>
        Public ReadOnly CallerMethod As String

        ''' <summary>���O�o�͍s�ԍ��B</summary>
        Public ReadOnly LineNo As Integer

        ''' <summary>�R���X�g���N�^�B</summary>
        ''' <param name="wtm">�������ݓ����B</param>
        ''' <param name="lv">���O���x���B</param>
        ''' <param name="msg">���O���b�Z�[�W�B</param>
        ''' <param name="cls">���O�o�̓N���X�B</param>
        ''' <param name="mtd">���O�o�̓��\�b�h�B</param>
        ''' <param name="lineNo">���O�o�͍s�ԍ��B</param>
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
