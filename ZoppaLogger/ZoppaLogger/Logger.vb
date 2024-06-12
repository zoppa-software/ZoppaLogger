Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.CompilerServices
Imports System.Threading

''' <summary>���O�o�͋@�\�B</summary>
Public Class Logger

    ''' <summary>���O�������ŗ�O�������������Ƃ�ʒm���܂��B</summary>
    ''' <param name="sender">�C�x���g���s���B</param>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Public Event NotificationException(sender As Object, e As NotificationExceptionEventArgs)

    ''' <summary>�J�����g�̃��O�t�@�C�������k���ꂽ���Ƃ�ʒm���܂��B</summary>
    ''' <param name="sender">�C�x���g���s���B</param>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Public Event NotificationCompressedFile(sender As Object, e As NotificationCompressedFileEventArgs)

    ''' <summary>���k�ς݃��O�t�@�C�����ő吢�㐔�𒴂������Ƃ�ʒm���܂��B</summary>
    ''' <param name="sender">�C�x���g���s���B</param>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Public Event NotificationOrganizeCompressedFile(sender As Object, e As NotificationOrganizeCompressedFileEventArgs)

    ' �Ώۃt�@�C��
    Private mLogFile As FileInfo

    ' �o�̓G���R�[�h
    Private mEncode As Text.Encoding

    ' �ő働�O�T�C�Y
    Private mMaxLogSize As Integer

    ' �ő働�O���㐔
    Private mLogGen As Integer

    ' ���O�o�̓��x��
    Private mLogLevel As LogLevel

    ' ���t���ς������؂�ւ��邩�̃t���O
    Private mDateChange As Boolean

    ' �L���b�V���ɕۑ����郍�O�s���̃��~�b�g
    Private mCacheLimit As Integer

    ' ���O�o�͏������\�b�h
    Private mFormatMethod As Func(Of LogData, String)

    ' �����݃o�b�t�@
    Private ReadOnly mQueue As New Queue(Of LogData)()

    ' �G���[�����݃o�b�t�@
    Private ReadOnly mErrQueue As New Queue(Of LogData)()

    ' �O�񏑍��݊�������
    Private mPrevWriteDate As Date

    ' �����ݒ��t���O
    Private mWriting As Boolean

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
        Return UseCustom(Of Logger)(logFilePath, encode, maxLogSize, logGeneration, logLevel, dateChange, cacheLimit, formatMethod)
    End Function

    ''' <summary>�J�e�S���ʂ̃��O���g�p���܂��B</summary>
    ''' <param name="fatalLogFilePath">�o�̓t�@�C���p�X�i�v���I�ȃG���[�j</param>
    ''' <param name="errorLogFilePath">�o�̓t�@�C���p�X�i�G���[�j</param>
    ''' <param name="warningLogFilePath">�o�̓t�@�C���p�X�i�x���j</param>
    ''' <param name="informationLogFilePath">�o�̓t�@�C���p�X�i�ē��j</param>
    ''' <param name="debugLogFilePath">�o�̓t�@�C���p�X�i�f�o�b�O�j</param>
    ''' <param name="encode">�o�̓G���R�[�h�B</param>
    ''' <param name="maxLogSize">�ő働�O�t�@�C���T�C�Y�B</param>
    ''' <param name="logGeneration">���O���㐔�B</param>
    ''' <param name="logLevel">���O���x���B</param>
    ''' <param name="dateChange">���t�̕ύX�Ń��O��؂�ւ��邩�̐ݒ�B</param>
    ''' <param name="cacheLimit">���O�𒙂߂Ēu�����~�b�g�i�������烍�O�o�͂�D��j</param>
    ''' <param name="formatMethod">���O�o�͏������\�b�h�B</param>
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

    ''' <summary>�J�X�^�����O���g�p���܂��B</summary>
    ''' <param name="logFilePath">�o�̓t�@�C���p�X�B</param>
    ''' <param name="encode">�o�̓G���R�[�h�B</param>
    ''' <param name="maxLogSize">�ő働�O�t�@�C���T�C�Y�B</param>
    ''' <param name="logGeneration">���O���㐔�B</param>
    ''' <param name="logLevel">���O���x���B</param>
    ''' <param name="dateChange">���t�̕ύX�Ń��O��؂�ւ��邩�̐ݒ�B</param>
    ''' <param name="cacheLimit">���O�𒙂߂Ēu�����~�b�g�i�������烍�O�o�͂�D��j</param>
    ''' <param name="formatMethod">���O�o�͏������\�b�h�B</param>
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
        Me.WaitFlushed(cnt, Me.mCacheLimit)

        ' �ʃX���b�h�Ńt�@�C���ɏo��
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

    ''' <summary>�L���[�ɗ��܂��Ă��郍�O���o�͂��܂��B</summary>
    ''' <param name="cnt">�L���[�̃��O���B</param>
    ''' <param name="limit">�����o�����~�b�g�B</param>
    ''' <param name="loopCount">�ҋ@���[�v�񐔁B</param>
    ''' <param name="interval">�ҋ@���[�v�C���^�[�o���B</param>
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

    ''' <summary>���O���t�@�C���ɏo�͂���B</summary>
    Private Sub ThreadWrite()
RETRY_THREAD:
        Me.mLogFile.Refresh()

        If Me.mLogFile.Exists AndAlso
               (Me.mLogFile.Length > Me.mMaxLogSize OrElse Me.ChangeOfDate) Then
            Try
                ' �t�@�C�����̗v�f�𕪊�
                Dim ext = Path.GetExtension(Me.mLogFile.Name)
                Dim nm = Me.mLogFile.Name.Substring(0, Me.mLogFile.Name.Length - ext.Length)
                Dim tn = Date.Now.ToString("yyyyMMddHHmmssfff")

                Dim zipPath = New IO.FileInfo($"{mLogFile.Directory.FullName}\{nm}_{tn}\{nm}{ext}")
                Try
                    ' ���k����t�H���_���쐬
                    If Not zipPath.Exists Then
                        zipPath.Directory.Create()
                    End If

                    ' ���O�t�@�C�������k
                    '
                    ' 1. ���k�t�H���_�Ƀ��O�t�@�C���ړ��A�ړ��o�����爳�k
                    ' 2. ���݂̃��O�t�@�C�������k
                    ' 3. ���O�t�@�C�������k�������Ƃ��O���ɒʒm
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

                ' �ߋ��t�@�C���𐮗�
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
                    ' �L���[���̕�������擾
                    '
                    ' 2. �L���[�Ƀ��O��񂪂���
                    '    �Ώۃ��O���x���ȏ�̃��O���x�����o�͂���ꍇ�A�o�͂���
                    ' 3. �L���[�Ƀ��O��񂪋�̏ꍇ�̓��[�v�𔲂��ăt�@�C���X�g���[�������
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

                    ' �t�@�C���ɏ����o��
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

                    ' �o�͂������ʁA���O�t�@�C�����ő�T�C�Y�𒴂���ꍇ�A���[�v�𔲂��ăX�g���[�������
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

    ''' <summary>���O�t�@�C�������k����t�H���_�ֈړ�����B</summary>
    ''' <param name="zipPath">�ړ���t�@�C���p�X�B</param>
    ''' <param name="retryCount">���g���C�񐔁B</param>
    ''' <param name="retryInterval">���g���C�C���^�[�o���B</param>
    ''' <returns>�ړ��ɐ��������ꍇ��True�A���s�����ꍇ��False�B</returns>
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

    ''' <summary>�ߋ��t�@�C���𐮗�����B</summary>
    ''' <param name="oldFiles">�ߋ��t�@�C�����X�g�B</param>
    Private Sub ArchiveOldFiles(oldFiles As List(Of String))
        Task.Run(
            Sub()
                ' �폜���Ƀt�@�C�����\�[�g
                oldFiles.Sort()

                ' �폜����t�@�C�����O���ɒʒm
                Dim ev As New NotificationOrganizeCompressedFileEventArgs(oldFiles)
                Me.OnNotificationOrganizeCompressedFile(ev)

                ' �L�����Z������Ă��Ȃ���΍폜
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

    ''' <summary>���O���t�@�C���ɏo�͂��镶������쐬����B</summary>
    ''' <param name="dat">���O�f�[�^�B</param>
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

    ''' <summary>��O�𔭐����������Ƃ�ʒm���܂��B</summary>
    ''' <param name="sendEx">�ʒm�����O�A</param>
    ''' <param name="memberName">��O�𔭐����������\�b�h�B</param>
    ''' <param name="lineNo">���\�b�h�̍s�ԍ��B</param>
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

    ''' <summary>�G���[�������������Ƃ�ʒm����C�x���g�𔭍s���܂��B</summary>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Protected Overridable Sub OnNotificationException(e As NotificationExceptionEventArgs)
        RaiseEvent NotificationException(Me, e)
    End Sub

    ''' <summary>���O�t�@�C�������k���ꂽ���Ƃ�ʒm����C�x���g�𔭍s���܂��B</summary>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
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

    ''' <summary>�J�����g�̃��O�t�@�C�������k���ꂽ�C�x���g�𔭍s���܂��B</summary>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Protected Overridable Sub OnNotificationCompressedFile(e As NotificationCompressedFileEventArgs)
        RaiseEvent NotificationCompressedFile(Me, e)
    End Sub

    ''' <summary>���k�ς݃��O�t�@�C�����ő吢�㐔�𒴂����C�x���g�𔭍s���܂��B</summary>
    ''' <param name="e">�C�x���g�I�u�W�F�N�g�B</param>
    Protected Overridable Sub OnNotificationOrganizeCompressedFile(e As NotificationOrganizeCompressedFileEventArgs)
        RaiseEvent NotificationOrganizeCompressedFile(Me, e)
    End Sub

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
                Task.Run(Sub() Me.ThreadWrite())
            End If

        Catch ex As Exception
            Debug.WriteLine($"FlushWrite {ex.Message}")
        End Try
    End Sub

    ''' <summary>�������ݒ���Ԃ��擾���܂��B</summary>
    ''' <returns>�������ݒ���ԁB</returns>
    Public ReadOnly Property IsWriting() As Boolean
        Get
            SyncLock Me
                Return (Me.mQueue.Count + Me.mErrQueue.Count > 0)
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

    ''' <summary>�G���[�ʒm�C�x���g���B</summary>
    Public NotInheritable Class NotificationExceptionEventArgs
        Inherits Exception

        ''' <summary>����������O���擾���܂��B</summary>
        ''' <returns>��O���B</returns>
        Public ReadOnly Property Target As Exception

        ''' <summary>�R���X�g���N�^�B</summary>
        ''' <param name="ex">����������O�B</param>
        Public Sub New(ex As Exception)
            Me.Target = ex
        End Sub

    End Class

    ''' <summary>���O�t�@�C�����k�ʒm�C�x���g���B</summary>
    Public NotInheritable Class NotificationCompressedFileEventArgs
        Inherits EventArgs

        ''' <summary>���k�������O�t�@�C�������������擾���܂��B</summary>
        Public ReadOnly Property TargetFile As IO.FileInfo

        ''' <summary>�R���X�g���N�^�B</summary>
        ''' <param name="targetFile">���k�������O�t�@�C���B</param>
        Public Sub New(targetFile As IO.FileInfo)
            Me.TargetFile = targetFile
        End Sub

    End Class

    ''' <summary>���k���O�t�@�C���̐����ʒm�C�x���g���B</summary>
    Public NotInheritable Class NotificationOrganizeCompressedFileEventArgs
        Inherits EventArgs

        ''' <summary>���k�������O�t�@�C���̃��X�g���擾���܂��B</summary>
        Public ReadOnly Property TargetFiles As New List(Of IO.FileInfo)

        ''' <summary>�Â��t�@�C�����폜�̃L�����Z����ݒ�A�擾���܂��B</summary>
        Public Property Cancel As Boolean = False

        ''' <summary>�R���X�g���N�^�B</summary>
        ''' <param name="targetPaths">���k�������O�t�@�C�����X�g�B</param>
        Public Sub New(targetPaths As List(Of String))
            For Each ph In targetPaths
                Me.TargetFiles.Add(New IO.FileInfo(ph))
            Next
        End Sub

    End Class

End Class
