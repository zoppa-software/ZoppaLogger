# ZoppaLogger
�V���v���ȃ��O�o�͋@�\��񋟂��܂��B  

## ����
���O�o�̓��C�u�����͍��@�\�Ȃ��̂������ĕ֗��ł����g�����Ȃ��̂��������܂��B  
���̂��߁A�t�@�C���Ƀ��O���o�͂��邾���̃��O�o�͋@�\��p�ӂ��܂����B

``` vb.net
Dim logger = ZoppaLogger.Logger.Use(maxLogSize:=200 * 1024)

For i As Integer = 0 To 10000
    logger.LoggingInformation($"{i} abcdefghijklmnopqrstuvwxyz ABCDEFGHIJLKMNOPQRSTUVWXYZ 1234567890")
Next

logger.WaitFinish()
```
## �ˑ��֌W
���C�u������ .NET Standard 2.0 �ŋL�q���Ă��܂��B���̂��߁A.net framework 4.6.1�ȍ~�A.net core 2.0�ȍ~�Ŏg�p�ł��܂��B  
���̑��̃��C�u�����ւ̈ˑ��֌W�͂���܂���B

## �g����

## �쐬���
* ���c�@���izoppa software�j
* �~�E����3�V�X�e���J���p�j�[ 
* takashi.zouta@kkmiuta.jp

## ���C�Z���X
[apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html)
