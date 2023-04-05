Option Strict On
Option Explicit On

''' <summary>ログ出力レベル。</summary>
Public Enum LogLevel

    ''' <summary>致命的レベル。</summary>
    Fatal = 0

    ''' <summary>エラーレベル。</summary>
    [Error] = 1

    ''' <summary>案内レベル。</summary>
    Infomation = 2

    ''' <summary>警告レベル。</summary>
    Warning = 3

    ''' <summary>デバッグレベル。</summary>
    Debug = 4

End Enum