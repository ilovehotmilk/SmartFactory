# SmartFactory项目简介
* 融合了离线人脸识别、实时离线语音识别和离线语音合成的WPF项目
* 人脸识别方案基于[Face Recognition--Python](https://github.com/ageitgey/face_recognition)
* 语音识别和语音合成方案是基于科大讯飞的[离线命令词识别SDK](http://www.xfyun.cn/services/commandWord)和[离线语音合成SDK](http://www.xfyun.cn/services/offline_tts)（收费，试用版35天使用期限）
* 只支持Windows 64位操作系统

## 开发环境
* Windows 7
* Visual Studio 2017
* .Net 4.6.1
## 运行环境配置
### 硬件
* 摄像头、麦克风、音响
### 人脸识别
* 下载[Python3.5运行环境压缩包](https://pan.baidu.com/s/1MSSY8c8R1ipaKfDJJpsXpg)，解压至与可执行文件同一目录下
* 可执行文件路径不可以包含中文！！！
### 语音识别和语音合成
* 在科大讯飞官网注册并获取[离线命令词识别SDK](http://www.xfyun.cn/services/commandWord)和[离线语音合成SDK](http://www.xfyun.cn/services/offline_tts)
* 将SmartFactory项目下的msc_x64.dll替换为离线命令词识别SDK中bin文件夹中的msc_x64.dll
* 将SmartFactory项目下msc/asr文件夹中的**所有.jet文件**删除并替换为自己下载的离线命令词识别SDK中bin/msc/res/asr文件夹中的**所有.jet文件**
* 将SmartFactory项目下msc/tts文件夹中的**所有.jet文件**删除并替换为自己下载的离线语音合成SDK中bin/msc/res/tts文件夹中的**所有.jet文件**
* 用讯飞开放平台控制台中的APPID给SmartFactory项目下MainWindows.xaml.cs文件中的"loginParam"属性赋值（例如：loginParam = "APPID = 123456";）
* 将SmartFactory项目下的tengen.bnf中的command替换为自己需要识别的命令词，不同命令词之间用"|"分隔
* SmartFactory项目下的tengen.tbk中的每一行为一个完整的语音识别和合成的反馈组合,"|"的左边为语音识别的命令词，右边为语音合成需要反馈的语句

## 可能会遇到的问题
* 在Windows10环境下编译运行可能会遇到语音识别模块的报错问题。   
**解决方案：**    
1、在Windows7环境下编译运行    
2、向科大讯飞QQ群求助(群号:514352489)

## 联系作者
* QQ：718825515
