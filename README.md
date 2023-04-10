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
### ���O�̏o�̓t�@�C����ݒ�
�ŏ��Ɉȉ��̂悤�ɐÓI���\�b�h���g�p���ă��O�̏o�̓t�@�C����ݒ肵�ăC���X�^���X���擾���܂��B  
``` vb.net
Dim logger = ZoppaLogger.Logger.Use(maxLogSize:=200 * 1024)
```
`Use`���\�b�h�ɂ͈ȉ��̃p�����[�^������܂��B 

|�p�����[�^��|�^|����|
|:--|:--|:--|
|logFilePath|String|���O�̏o�̓t�@�C���̃p�X���w�肵�܂��B�w�肵�Ȃ��ꍇ�́A�A�v���P�[�V�����̎��s�t�@�C���Ɠ����f�B���N�g����`default.log`�Ƃ����t�@�C���ɏo�͂��܂��B|
|encode|System.Text.Encoding|���O�̏o�̓t�@�C���̃G���R�[�h���w�肵�܂��B�w�肵�Ȃ��ꍇ�̓V�X�e���̃f�t�H���g��ݒ肵�܂��B|
|maxLogSize|Integer|���O�̏o�̓t�@�C���̍ő�T�C�Y���w�肵�܂��B�w�肵�Ȃ��ꍇ�́A30MB�ɂȂ�܂��B|
|logGeneration|Integer|�ߋ����O�̕ێ��������w�肵�܂��B�ߋ����O�t�@�C���͐�ɐݒ肵�����O�t�@�C�����ɔN���������b��������zip���k�t�@�C���ɂ��ĕێ����܂��B�w�肵�Ȃ��ꍇ��10���ێ����܂��B|
|logLevel|ZoppaLogger.LogLevel|�o�͂��郍�O�̃��x�����w�肵�܂��B�w�肵�Ȃ��ꍇ��`Debug`���x���ɂȂ�܂��B|
|dateChange|Boolean|���t���ς�����Ƃ����O�t�@�C����؂�ւ��邩�ۂ���^�U�l�Ŏw�肵�܂��B�w�肵�Ȃ��ꍇ�A�����ς���Ă����O�t�@�C����؂�ւ��Ȃ�`False`���ݒ肳��܂�|
|cacheLimit|Integer|�����Ŏ��L���b�V���̍s�������̎w��l�𒴂����ꍇ�A���O�̏o�͂�D�悵�čs���܂��B|
|formatMethod|Func(Of LogData, String)|���O�̏o�̓t�H�[�}�b�g���w�肵�����\�b�h�Œu�������܂��B������`LogData`�N���X���o�͂��郍�O�̏���ێ����Ă��܂��B|

`Use`���\�b�h�Ƃ͕ʂ�`UseCustom`���\�b�h���g�p�����`Logger`�N���X���p���������O�o�̓N���X���g�p���邱�Ƃ��ł��܂��i�p�������N���X�ł̓C�x���g���\�b�h���I�[�o�[���C�h���邱�Ƃ��ł��܂��j  

### ���O�̏o��



### �C�x���g


## �쐬���
* ���c�@���izoppa software�j
* �~�E����3�V�X�e���J���p�j�[ 
* takashi.zouta@kkmiuta.jp

## ���C�Z���X
[apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html)
