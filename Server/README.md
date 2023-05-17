> 一个基于.net 7的Socks5代理服务端，包括一个管理界面。

#### 1.如何配置？
##### 1.1 配置服务端
appsettings.json:
```
  "ServerConfig": {
    "Port": 8333,//服务端断开
    "NeedAuth": true,//是否启动认证
    "AuthVersion": 1
  }
```
#### 2.使用
##### 2.1 启动服务端
##### 2.2 使用代理软件如QQ或者Proxifier配置代理
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/manmain.jpg)
##### 2.3 看到如下效果
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/prodisplay.jpg)
##### 2.3 服务端管理界面
![image](https://github.com/BruceQiu1996/Socks5Server/blob/master/ScreentShots/manmain.jpg)
