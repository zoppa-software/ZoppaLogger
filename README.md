# ZoppaLogger
シンプルなログ出力機能を提供します。  

## 説明
ログ出力ライブラリは高機能なものが多くて便利ですが使いこなすのが難しくあります。  
そのため、ファイルにログを出力するだけのログ出力機能を用意しました。

``` vb.net
Dim logger = ZoppaLogger.Logger.Use(maxLogSize:=200 * 1024)

For i As Integer = 0 To 10000
    logger.LoggingInformation($"{i} abcdefghijklmnopqrstuvwxyz ABCDEFGHIJLKMNOPQRSTUVWXYZ 1234567890")
Next

logger.WaitFinish()
```
## 依存関係
ライブラリは .NET Standard 2.0 で記述しています。そのため、.net framework 4.6.1以降、.net core 2.0以降で使用できます。  
その他のライブラリへの依存関係はありません。

## 使い方
### ログの出力ファイルを設定
最初に以下のように静的メソッドを使用してログの出力ファイルを設定してインスタンスを取得します。  
``` vb.net
Dim logger = ZoppaLogger.Logger.Use(maxLogSize:=200 * 1024)
```
`Use`メソッドには以下のパラメータがあります。 

|パラメータ名|型|説明|
|:--|:--|:--|
|logFilePath|String|ログの出力ファイルのパスを指定します。指定しない場合は、アプリケーションの実行ファイルと同じディレクトリに`default.log`というファイルに出力します。|
|encode|System.Text.Encoding|ログの出力ファイルのエンコードを指定します。指定しない場合はシステムのデフォルトを設定します。|
|maxLogSize|Integer|ログの出力ファイルの最大サイズを指定します。指定しない場合は、30MBになります。|
|logGeneration|Integer|過去ログの保持件数を指定します。過去ログファイルは先に設定したログファイル名に年月日時分秒を加えてzip圧縮ファイルにして保持します。指定しない場合は10件保持します。|
|logLevel|ZoppaLogger.LogLevel|出力するログのレベルを指定します。指定しない場合は`Debug`レベルになります。|
|dateChange|Boolean|日付が変わったときログファイルを切り替えるか否かを真偽値で指定します。指定しない場合、日が変わってもログファイルを切り替えない`False`が設定されます|
|cacheLimit|Integer|内部で持つキャッシュの行数がこの指定値を超えた場合、ログの出力を優先して行います。|
|formatMethod|Func(Of LogData, String)|ログの出力フォーマットを指定したメソッドで置き換えます。引数の`LogData`クラスが出力するログの情報を保持しています。|

`Use`メソッドとは別に`UseCustom`メソッドを使用すると`Logger`クラスを継承したログ出力クラスを使用することができます（継承したクラスではイベントメソッドをオーバーライドすることができます）  

### ログの出力



### イベント


## 作成情報
* 造田　崇（zoppa software）
* ミウラ第3システムカンパニー 
* takashi.zouta@kkmiuta.jp

## ライセンス
[apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html)
