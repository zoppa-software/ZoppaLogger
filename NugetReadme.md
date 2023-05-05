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

以上、簡単な説明となります。**ライブラリの詳細は[Githubのページ](https://github.com/zoppa-software/ZoppaLogger)を参照してください。**

## 更新について
* 1.0.0 ログレベル別にファイル出力する `CategorizeLogger` を追加
* 0.9.0 ドキュメント作成途中、機能リファクタリング
* 0.0.9 ドキュメント作成途中
