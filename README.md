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

## 作成情報
* 造田　崇（zoppa software）
* ミウラ第3システムカンパニー 
* takashi.zouta@kkmiuta.jp

## ライセンス
[apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html)
